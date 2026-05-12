using LLVMSharp.Interop;
using SolidLang.Parser;
using SolidLang.SemanticAnalyzer;

namespace SolidLang.IrGenerator;

public sealed partial class CodeGenerator
{
    private LLVMValueRef GenerateExpression(BoundExpression expr)
    {
        return expr switch
        {
            BoundLiteralExpr lit => GenerateLiteral(lit),
            BoundVarExpr varExpr => GenerateVarExpr(varExpr),
            BoundBinaryExpr bin => GenerateBinary(bin),
            BoundUnaryExpr un => GenerateUnary(un),
            BoundCallExpr call => GenerateCall(call),
            BoundConditionalExpr cond => GenerateConditional(cond),
            BoundMemberAccessExpr memberAccess => GenerateMemberAccess(memberAccess),
            _ => default,
        };
    }

    private LLVMValueRef GenerateLiteral(BoundLiteralExpr lit)
    {
        if (lit.Value is string str)
        {
            if (lit.Type is NamedType)
                return GenerateStringStructLiteral(str, lit.Type);
            return GenerateStringLiteral(str);
        }

        var llvmTy = GetLLVMType(lit.Type);

        if (lit.Value is int i)
            return LLVMValueRef.CreateConstInt(llvmTy, (ulong)(int)i, SignExtend: true);
        if (lit.Value is long l)
            return LLVMValueRef.CreateConstInt(llvmTy, (ulong)l, SignExtend: true);
        if (lit.Value is uint ui)
            return LLVMValueRef.CreateConstInt(llvmTy, ui, SignExtend: false);
        if (lit.Value is ulong ul)
            return LLVMValueRef.CreateConstInt(llvmTy, ul, SignExtend: false);
        if (lit.Value is float f)
            return LLVMValueRef.CreateConstReal(llvmTy, f);
        if (lit.Value is double d)
            return LLVMValueRef.CreateConstReal(llvmTy, d);
        if (lit.Value is bool b)
            return LLVMValueRef.CreateConstInt(llvmTy, b ? 1u : 0u, SignExtend: false);

        // Fallback: zero
        return LLVMValueRef.CreateConstInt(llvmTy, 0, false);
    }

    /// <summary>
    /// Generate a constant value from a bound expression (for global initializers).
    /// Only supports literals; complex expressions should use GenerateExpression in a function body.
    /// </summary>
    private LLVMValueRef GenerateConstant(BoundExpression expr)
    {
        return expr switch
        {
            BoundLiteralExpr lit => GenerateLiteral(lit),
            _ => LLVMValueRef.CreateConstInt(_context.Int32Type, 0, false),
        };
    }

    private int _stringId;
    private LLVMValueRef GenerateStringLiteral(string str)
    {
        var strConst = _context.GetConstString(str, DontNullTerminate: false);
        var name = $".str.{_stringId++}";
        var global = _module.AddGlobal(strConst.TypeOf, name);
        global.Initializer = strConst;
        global.Linkage = LLVMLinkage.LLVMPrivateLinkage;
        return global;
    }

    private LLVMValueRef GenerateStringStructLiteral(string str, SolidType stringType)
    {
        // Create the global string constant
        var strConst = _context.GetConstString(str, DontNullTerminate: false);
        var name = $".str.{_stringId++}";
        var global = _module.AddGlobal(strConst.TypeOf, name);
        global.Initializer = strConst;
        global.Linkage = LLVMLinkage.LLVMPrivateLinkage;

        // Cast [N x i8]* to i8* for the ptr field
        var ptrVal = LLVMValueRef.CreateConstBitCast(global,
            LLVMTypeRef.CreatePointer(_context.Int8Type, 0));

        // Length constant (usize = i64 on 64-bit)
        var lenVal = LLVMValueRef.CreateConstInt(_context.Int64Type, (ulong)str.Length, false);

        var structTy = GetLLVMType(stringType);
        return LLVMValueRef.CreateConstStruct(new[] { ptrVal, lenVal }, false);
    }

    private LLVMValueRef GenerateVarExpr(BoundVarExpr varExpr)
    {
        if (varExpr.Symbol is not VariableSymbol vs)
            return default; // Function reference, not loadable as value

        var ty = GetLLVMType(vs.DeclaredType);

        // Check globals first (external variables like stdout)
        if (_globals.TryGetValue(vs, out var global))
            return _builder.BuildLoad2(ty, global, vs.Name);

        // Local variable
        if (_variables.TryGetValue(vs, out var alloca))
            return _builder.BuildLoad2(ty, alloca, vs.Name);

        return default;
    }

    private LLVMValueRef GenerateBinary(BoundBinaryExpr bin)
    {
        var left = GenerateExpression(bin.Left);
        var right = GenerateExpression(bin.Right);

        return bin.Operator switch
        {
            SyntaxKind.PlusToken => _builder.BuildAdd(left, right, "add"),
            SyntaxKind.MinusToken => _builder.BuildSub(left, right, "sub"),
            SyntaxKind.StarToken => _builder.BuildMul(left, right, "mul"),
            SyntaxKind.SlashToken => _builder.BuildSDiv(left, right, "div"),
            SyntaxKind.PercentToken => _builder.BuildSRem(left, right, "rem"),
            SyntaxKind.EqualsEqualsToken => _builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, left, right, "eq"),
            SyntaxKind.BangEqualsToken => _builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, left, right, "ne"),
            SyntaxKind.LessToken => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, left, right, "lt"),
            SyntaxKind.GreaterToken => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, left, right, "gt"),
            SyntaxKind.LessEqualsToken => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, left, right, "le"),
            SyntaxKind.GreaterEqualsToken => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, left, right, "ge"),
            SyntaxKind.AmpersandToken => _builder.BuildAnd(left, right, "and"),
            SyntaxKind.PipeToken => _builder.BuildOr(left, right, "or"),
            SyntaxKind.CaretToken => _builder.BuildXor(left, right, "xor"),
            SyntaxKind.LessLessToken => _builder.BuildShl(left, right, "shl"),
            SyntaxKind.GreaterGreaterToken => _builder.BuildAShr(left, right, "ashr"),
            _ => default,
        };
    }

    private LLVMValueRef GenerateUnary(BoundUnaryExpr un)
    {
        if (un.Operator == SyntaxKind.AmpersandToken)
            return GenerateAddressOf(un.Operand);

        var operand = GenerateExpression(un.Operand);

        return un.Operator switch
        {
            SyntaxKind.MinusToken => _builder.BuildNeg(operand, "neg"),
            SyntaxKind.BangToken => _builder.BuildNot(operand, "not"),
            SyntaxKind.TildeToken => _builder.BuildNot(operand, "bnot"), // bitwise NOT
            _ => default,
        };
    }

    private LLVMValueRef GenerateAddressOf(BoundExpression operand)
    {
        if (operand is BoundVarExpr varExpr && varExpr.Symbol is VariableSymbol vs)
        {
            if (_globals.TryGetValue(vs, out var global))
                return global;
            if (_variables.TryGetValue(vs, out var alloca))
                return alloca;
        }
        return default;
    }

    private LLVMValueRef GenerateCall(BoundCallExpr call)
    {
        if (call.Function == null)
            return default;

        if (!_functions.TryGetValue(call.Function, out var callee))
            return default;

        if (!_functionTypes.TryGetValue(call.Function, out var funcTy))
            return default;

        var args = call.Arguments.Select(a => GenerateExpression(a)).ToArray();
        return _builder.BuildCall2(funcTy, callee, args, "call");
    }

    private LLVMValueRef GenerateConditional(BoundConditionalExpr cond)
    {
        var condVal = GenerateExpression(cond.Condition);
        var thenVal = GenerateExpression(cond.ThenExpr);
        var elseVal = GenerateExpression(cond.ElseExpr);

        return _builder.BuildSelect(condVal, thenVal, elseVal, "select");
    }

    private LLVMValueRef GenerateMemberAccess(BoundMemberAccessExpr ma)
    {
        if (ma.Member == null)
            return default;

        // Get the address of the receiver
        var receiverAddr = GetTargetAddress(ma.Receiver);
        if (receiverAddr.Handle == default)
            return default;

        if (ma.Receiver.Type is not NamedType nt)
            return default;

        var structTy = GetLLVMType(nt);
        var fieldIndex = GetFieldIndex(nt.TypeSymbol, ma.Member);
        var fieldPtr = _builder.BuildStructGEP2(structTy, receiverAddr, (uint)fieldIndex, ma.Member.Name);

        return _builder.BuildLoad2(GetLLVMType(ma.Member.MemberType), fieldPtr, ma.Member.Name);
    }

    private static int GetFieldIndex(TypeSymbol typeSymbol, MemberSymbol member)
    {
        var fields = typeSymbol.TypeScope!.Members.Values
            .OfType<MemberSymbol>()
            .OrderBy(m => m.Declaration!.Span.Start)
            .ToList();
        return fields.IndexOf(member);
    }
}
