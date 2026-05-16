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
            BoundIndexAccessExpr indexAccess => GenerateIndexAccess(indexAccess),
            BoundBuiltinCallExpr builtin => GenerateBuiltinCall(builtin),
            BoundStructLiteralExpr structLit => GenerateStructLiteral(structLit),
            BoundEnumLiteralExpr enumLit => GenerateEnumLiteral(enumLit),
            BoundVariantLiteralExpr variantLit => GenerateVariantLiteral(variantLit),
            BoundCtOperatorExpr ctOp => GenerateCtOperator(ctOp),
            _ => default,
        };
    }

    private LLVMValueRef GenerateIndexAccess(BoundIndexAccessExpr indexAccess)
    {
        // Get address of the receiver
        var receiverAddr = GetTargetAddress(indexAccess.Receiver);
        if (receiverAddr.Handle == default) return default;

        var receiverType = indexAccess.Receiver.Type;
        if (receiverType is NamedType nt && nt.TypeSymbol.Name == "Slice")
        {
            // Get the element type from the Slice's type argument
            var elemTy = nt.TypeArguments.Count > 0
                ? GetLLVMType(nt.TypeArguments[0]) : _context.Int32Type;

            var structTy = GetLLVMType(nt);
            // GEP to field 0 (ptr)
            var ptrFieldPtr = _builder.BuildStructGEP2(structTy, receiverAddr, 0u, "ptr.field");
            // Load the pointer
            var ptrTy = LLVMTypeRef.CreatePointer(elemTy, 0);
            var ptr = _builder.BuildLoad2(ptrTy, ptrFieldPtr, "ptr.load");
            // GEP to element at index
            var index = GenerateExpression(indexAccess.Index);
            var elemPtr = _builder.BuildGEP2(elemTy, ptr, new[] { index }, "elem.ptr");
            // Load element
            return _builder.BuildLoad2(elemTy, elemPtr, "elem");
        }

        return default;
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

        // null pointer literal
        if (lit.Type is PointerType)
            return LLVMValueRef.CreateConstNull(llvmTy);

        // Fallback: zero — use ConstNull for aggregate types, ConstInt for scalars
        if (lit.Type is ArrayType or NamedType)
            return LLVMValueRef.CreateConstNull(llvmTy);
        return LLVMValueRef.CreateConstInt(llvmTy, 0, false);
    }

    /// <summary>
    /// Generate a constant value from a bound expression (for global initializers).
    /// Only supports literals; complex expressions should use GenerateExpression in a function body.
    /// </summary>
    private LLVMValueRef GenerateConstant(BoundExpression expr)
    {
        switch (expr)
        {
            case BoundLiteralExpr lit:
                return GenerateLiteral(lit);
            case BoundUnaryExpr { Operator: SyntaxKind.MinusToken } unary
                when unary.Operand is BoundLiteralExpr operandLit:
            {
                // Constant-fold unary minus
                var operand = GenerateLiteral(operandLit);
                return LLVMValueRef.CreateConstNeg(operand);
            }
            case BoundStructLiteralExpr structLit:
                return GenerateConstantStructLiteral(structLit);
            default:
                return LLVMValueRef.CreateConstInt(_context.Int32Type, 0, false);
        }
    }

    private LLVMValueRef GenerateConstantStructLiteral(BoundStructLiteralExpr expr)
    {
        var structTy = GetLLVMType(new NamedType(expr.StructType));
        var members = expr.StructType.TypeScope!.Members.Values
            .OfType<MemberSymbol>()
            .OrderBy(m => m.Declaration!.Span.Start)
            .ToArray();

        var fieldValues = new LLVMValueRef[members.Length];
        for (int i = 0; i < members.Length; i++)
            fieldValues[i] = LLVMValueRef.CreateConstNull(GetLLVMType(members[i].MemberType));

        foreach (var (field, value) in expr.Fields)
        {
            var fieldIndex = GetFieldIndex(expr.StructType, field);
            fieldValues[fieldIndex] = GenerateConstant(value);
        }

        return LLVMValueRef.CreateConstStruct(fieldValues, false);
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

        // Pointer arithmetic: ptr + offset → GEP
        if (bin.Operator == SyntaxKind.PlusToken && bin.Left.Type is PointerType ptrTy)
        {
            var elemTy = GetLLVMType(ptrTy.PointeeType);
            return _builder.BuildGEP2(elemTy, left, new[] { right }, "ptr.add");
        }

        return bin.Operator switch
        {
            SyntaxKind.PlusToken => _builder.BuildAdd(left, right, "add"),
            SyntaxKind.MinusToken => _builder.BuildSub(left, right, "sub"),
            SyntaxKind.StarToken => _builder.BuildMul(left, right, "mul"),
            SyntaxKind.SlashToken => _builder.BuildSDiv(left, right, "div"),
            SyntaxKind.PercentToken => _builder.BuildSRem(left, right, "rem"),
            SyntaxKind.EqualsEqualsToken
                or SyntaxKind.BangEqualsToken
                or SyntaxKind.LessToken
                or SyntaxKind.GreaterToken
                or SyntaxKind.LessEqualsToken
                or SyntaxKind.GreaterEqualsToken
                => GenerateComparison(bin.Operator, left, right, bin.Left.Type),
            SyntaxKind.AmpersandToken => _builder.BuildAnd(left, right, "and"),
            SyntaxKind.AmpersandAmpersandToken => _builder.BuildAnd(left, right, "andl"),
            SyntaxKind.PipeToken => _builder.BuildOr(left, right, "or"),
            SyntaxKind.PipePipeToken => _builder.BuildOr(left, right, "orl"),
            SyntaxKind.CaretToken => _builder.BuildXor(left, right, "xor"),
            SyntaxKind.LessLessToken => _builder.BuildShl(left, right, "shl"),
            SyntaxKind.GreaterGreaterToken => _builder.BuildAShr(left, right, "ashr"),
            _ => default,
        };
    }

    private LLVMValueRef GenerateComparison(SyntaxKind op, LLVMValueRef left, LLVMValueRef right, SolidType? type)
    {
        var name = op switch
        {
            SyntaxKind.EqualsEqualsToken => "eq",
            SyntaxKind.BangEqualsToken => "ne",
            SyntaxKind.LessToken => "lt",
            SyntaxKind.GreaterToken => "gt",
            SyntaxKind.LessEqualsToken => "le",
            SyntaxKind.GreaterEqualsToken => "ge",
            _ => "cmp",
        };

        if (type is PrimitiveType pt && (pt.Name == "f32" || pt.Name == "f64"))
        {
            var pred = op switch
            {
                SyntaxKind.EqualsEqualsToken => LLVMRealPredicate.LLVMRealOEQ,
                SyntaxKind.BangEqualsToken => LLVMRealPredicate.LLVMRealONE,
                SyntaxKind.LessToken => LLVMRealPredicate.LLVMRealOLT,
                SyntaxKind.GreaterToken => LLVMRealPredicate.LLVMRealOGT,
                SyntaxKind.LessEqualsToken => LLVMRealPredicate.LLVMRealOLE,
                SyntaxKind.GreaterEqualsToken => LLVMRealPredicate.LLVMRealOGE,
                _ => LLVMRealPredicate.LLVMRealOEQ,
            };
            return _builder.BuildFCmp(pred, left, right, name);
        }

        var iPred = op switch
        {
            SyntaxKind.EqualsEqualsToken => LLVMIntPredicate.LLVMIntEQ,
            SyntaxKind.BangEqualsToken => LLVMIntPredicate.LLVMIntNE,
            SyntaxKind.LessToken => LLVMIntPredicate.LLVMIntSLT,
            SyntaxKind.GreaterToken => LLVMIntPredicate.LLVMIntSGT,
            SyntaxKind.LessEqualsToken => LLVMIntPredicate.LLVMIntSLE,
            SyntaxKind.GreaterEqualsToken => LLVMIntPredicate.LLVMIntSGE,
            _ => LLVMIntPredicate.LLVMIntEQ,
        };
        return _builder.BuildICmp(iPred, left, right, name);
    }

    private LLVMValueRef GenerateUnary(BoundUnaryExpr un)
    {
        if (un.Operator == SyntaxKind.AmpersandToken)
            return GenerateAddressOf(un.Operand);

        var operand = GenerateExpression(un.Operand);

        return un.Operator switch
        {
            SyntaxKind.PlusToken => operand,
            SyntaxKind.StarToken => _builder.BuildLoad2(
                GetLLVMType((un.Operand.Type as PointerType)!.PointeeType), operand, "deref"),
            SyntaxKind.MinusToken => _builder.BuildNeg(operand, "neg"),
            SyntaxKind.BangToken => _builder.BuildXor(operand,
                LLVMValueRef.CreateConstInt(_context.Int8Type, 1, false), "not"),
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
        // &(expr.field) — get address of a struct field
        if (operand is BoundMemberAccessExpr ma && ma.Member != null)
        {
            var structAddr = GetStructAddress(ma.Receiver);
            if (structAddr.Handle == default)
                return default;

            var typeSym = GetStructTypeSymbol(ma.Receiver.Type);
            if (typeSym == null)
                return default;

            var structTy = GetLLVMType(new NamedType(typeSym));
            var fieldIndex = GetFieldIndex(typeSym, ma.Member);
            if (typeSym.Kind == SymbolKind.Variant)
                fieldIndex++;
            return _builder.BuildStructGEP2(structTy, structAddr, (uint)fieldIndex, ma.Member.Name);
        }
        return default;
    }

    /// <summary>
    /// Get the address of a struct value, handling both by-value structs and
    /// pointer-to-struct receivers (for *. and &. desugarings).
    /// </summary>
    private LLVMValueRef GetStructAddress(BoundExpression expr)
    {
        // Direct variable reference: address is its alloca
        if (expr is BoundVarExpr varExpr && varExpr.Symbol is VariableSymbol vs)
        {
            if (_globals.TryGetValue(vs, out var global))
                return global;
            if (_variables.TryGetValue(vs, out var alloca))
                return alloca;
        }
        // *ptr: the loaded pointer value IS the struct address
        if (expr is BoundUnaryExpr un && un.Operator == SyntaxKind.StarToken)
            return GenerateExpression(un.Operand);
        // &original: the address is GenerateAddressOf(original)
        if (expr is BoundUnaryExpr addrOf && addrOf.Operator == SyntaxKind.AmpersandToken)
            return GenerateAddressOf(addrOf.Operand);
        // Nested member access: compute the field address
        if (expr is BoundMemberAccessExpr ma && ma.Member != null)
            return GenerateAddressOf(expr);
        return default;
    }

    /// <summary>
    /// Extract the TypeSymbol from a struct type, handling both NamedType and PointerType→NamedType.
    /// </summary>
    private static TypeSymbol? GetStructTypeSymbol(SolidType? type)
    {
        if (type is NamedType nt)
            return nt.TypeSymbol;
        if (type is PointerType pt && pt.PointeeType is NamedType ptNt)
            return ptNt.TypeSymbol;
        return null;
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

        // BuildSelect requires i1 condition; trunc if needed (bool is i8)
        if (condVal.TypeOf.IntWidth > 1)
            condVal = _builder.BuildTrunc(condVal, _context.Int1Type, "cond.i1");

        return _builder.BuildSelect(condVal, thenVal, elseVal, "select");
    }

    private LLVMValueRef GenerateMemberAccess(BoundMemberAccessExpr ma)
    {
        if (ma.Member == null)
            return default;

        var structAddr = GetStructAddress(ma.Receiver);
        if (structAddr.Handle == default)
            return default;

        var typeSym = GetStructTypeSymbol(ma.Receiver.Type);
        if (typeSym == null)
            return default;

        var structTy = GetLLVMType(new NamedType(typeSym));
        var fieldIndex = GetFieldIndex(typeSym, ma.Member);
        if (typeSym.Kind == SymbolKind.Variant)
            fieldIndex++;
        var fieldPtr = _builder.BuildStructGEP2(structTy, structAddr, (uint)fieldIndex, ma.Member.Name);

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

    private LLVMValueRef GenerateStructLiteral(BoundStructLiteralExpr expr)
    {
        var namedType = new NamedType(expr.StructType);
        var structTy = GetLLVMType(namedType);

        var alloca = _builder.BuildAlloca(structTy, "struct.tmp");
        // Zero-initialize all fields
        _builder.BuildStore(LLVMValueRef.CreateConstNull(structTy), alloca);

        foreach (var (field, value) in expr.Fields)
        {
            var fieldIndex = GetFieldIndex(expr.StructType, field);
            var fieldPtr = _builder.BuildStructGEP2(structTy, alloca, (uint)fieldIndex, field.Name);
            var fieldVal = GenerateExpression(value);
            _builder.BuildStore(fieldVal, fieldPtr);
        }

        return _builder.BuildLoad2(structTy, alloca, "struct.val");
    }

    private LLVMValueRef GenerateEnumLiteral(BoundEnumLiteralExpr expr)
    {
        // Enum literals are represented as the integer discriminant value.
        var enumTy = GetLLVMType(new NamedType(expr.EnumType));

        // Check for @flags annotation on the enum declaration
        var isFlags = expr.EnumType.Declaration is SolidLang.Parser.Nodes.Declarations.EnumDeclNode enumDecl
            && enumDecl.Annotations.Any(a => a.Name == "flags");

        // Extract discriminant from the enum field declaration
        if (expr.Member.Declaration is SolidLang.Parser.Nodes.Declarations.EnumFieldNode enumField
            && enumField.Value != null)
        {
            var eval = EvaluateConstantInt(enumField.Value);
            if (eval.HasValue)
                return LLVMValueRef.CreateConstInt(enumTy, (ulong)(long)eval.Value, true);
        }
        // Fallback: auto-assign value based on member index
        var members = expr.EnumType.TypeScope!.Members.Values
            .OfType<MemberSymbol>()
            .OrderBy(m => m.Declaration!.Span.Start)
            .ToList();
        var idx = members.IndexOf(expr.Member);
        var val = isFlags ? (1UL << idx) : (ulong)idx;
        return LLVMValueRef.CreateConstInt(enumTy, val, false);
    }

    /// <summary>
    /// Tries to evaluate an AST expression as a compile-time integer constant.
    /// </summary>
    private static long? EvaluateConstantInt(SolidLang.Parser.Nodes.Expressions.ExprNode expr)
    {
        return expr switch
        {
            SolidLang.Parser.Nodes.Expressions.PrimaryExprNode primary
                when primary.PrimaryKind == SolidLang.Parser.Nodes.Expressions.PrimaryExprKind.Literal
                && primary.Literal is SolidLang.Parser.Nodes.Literals.IntegerLiteralNode intLit
                => Convert.ToInt64(intLit.Value),
            SolidLang.Parser.Nodes.Expressions.BinaryExprNode bin
                => EvaluateBinaryOp(bin),
            SolidLang.Parser.Nodes.Expressions.UnaryExprNode un
                => EvaluateUnaryOp(un),
            _ => null,
        };
    }

    private static long? EvaluateBinaryOp(SolidLang.Parser.Nodes.Expressions.BinaryExprNode bin)
    {
        var left = EvaluateConstantInt(bin.Left);
        var right = EvaluateConstantInt(bin.Right);
        if (left == null || right == null) return null;

        return bin.Operator switch
        {
            SyntaxKind.PlusToken => left.Value + right.Value,
            SyntaxKind.MinusToken => left.Value - right.Value,
            SyntaxKind.StarToken => left.Value * right.Value,
            SyntaxKind.SlashToken => left.Value / right.Value,
            SyntaxKind.PercentToken => left.Value % right.Value,
            SyntaxKind.AmpersandToken => left.Value & right.Value,
            SyntaxKind.PipeToken => left.Value | right.Value,
            SyntaxKind.CaretToken => left.Value ^ right.Value,
            SyntaxKind.LessLessToken => left.Value << (int)right.Value,
            SyntaxKind.GreaterGreaterToken => left.Value >> (int)right.Value,
            _ => null,
        };
    }

    private static long? EvaluateUnaryOp(SolidLang.Parser.Nodes.Expressions.UnaryExprNode un)
    {
        var operand = EvaluateConstantInt(un.Operand);
        if (operand == null) return null;

        return un.Operator switch
        {
            SyntaxKind.MinusToken => -operand.Value,
            SyntaxKind.PlusToken => operand.Value,
            SyntaxKind.TildeToken => ~operand.Value,
            _ => null,
        };
    }

    private LLVMValueRef GenerateVariantLiteral(BoundVariantLiteralExpr expr)
    {
        // Variant/Tagged union: { i32 discriminant, member_0, member_1, ... }
        var namedType = new NamedType(expr.VariantType);
        var variantTy = GetLLVMType(namedType);

        // Get member index for discriminant (source order)
        var members = expr.VariantType.TypeScope!.Members.Values
            .OfType<MemberSymbol>()
            .OrderBy(m => m.Declaration!.Span.Start)
            .ToList();
        var memberIdx = members.IndexOf(expr.Member);
        var discVal = LLVMValueRef.CreateConstInt(_context.Int32Type,
            (ulong)(memberIdx >= 0 ? memberIdx : 0), false);

        var alloca = _builder.BuildAlloca(variantTy, "variant.tmp");
        _builder.BuildStore(LLVMValueRef.CreateConstNull(variantTy), alloca);

        // Store discriminant at field 0
        var discPtr = _builder.BuildStructGEP2(variantTy, alloca, 0u, "disc");
        _builder.BuildStore(discVal, discPtr);

        // Store payload at field memberIdx + 1 (discriminant is field 0)
        if (expr.Value != null)
        {
            var payloadIdx = (uint)(memberIdx + 1);
            var payloadPtr = _builder.BuildStructGEP2(variantTy, alloca, payloadIdx, "payload");
            var payloadVal = GenerateExpression(expr.Value);
            _builder.BuildStore(payloadVal, payloadPtr);
        }

        return _builder.BuildLoad2(variantTy, alloca, "variant.val");
    }

    private LLVMValueRef GenerateCtOperator(BoundCtOperatorExpr expr)
    {
        var resultTy = GetLLVMType(expr.Type);

        if (expr.OperatorKind == CtOperatorKind.Sizeof || expr.OperatorKind == CtOperatorKind.Alignof)
        {
            var llvmType = GetLLVMType(expr.TypeArgument);
            // Use the GEP trick: ptrtoint (getelementptr T, ptr null, i32 1) to i64
            var nullPtr = LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(llvmType, 0));
            var gep = LLVMValueRef.CreateConstGEP2(llvmType, nullPtr, new[] {
                LLVMValueRef.CreateConstInt(_context.Int32Type, 1, false) });
            var size = LLVMValueRef.CreateConstPtrToInt(gep, resultTy);
            return size;
        }

        if (expr.OperatorKind == CtOperatorKind.Offsetof)
        {
            // offsetof: compute byte offset of a field within a struct
            // GEP trick: ptrtoint (getelementptr T, ptr null, i32 0, i32 fieldIdx) to i64
            var structType = GetLLVMType(expr.TypeArgument);
            var nullPtr = LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(structType, 0));

            var typeSymbol = (expr.TypeArgument as NamedType)!.TypeSymbol;
            var member = typeSymbol.TypeScope!.Members.Values
                .OfType<MemberSymbol>()
                .FirstOrDefault(m => m.Name == expr.MemberName);
            if (member == null) return LLVMValueRef.CreateConstInt(resultTy, 0, false);

            var fieldIndex = GetFieldIndex(typeSymbol, member);
            if (typeSymbol.Kind == SymbolKind.Variant)
                fieldIndex++;

            var gep = LLVMValueRef.CreateConstGEP2(structType, nullPtr, new[] {
                LLVMValueRef.CreateConstInt(_context.Int32Type, 0, false),
                LLVMValueRef.CreateConstInt(_context.Int32Type, (ulong)fieldIndex, false) });
            var offset = LLVMValueRef.CreateConstPtrToInt(gep, resultTy);
            return offset;
        }

        return LLVMValueRef.CreateConstInt(resultTy, 0, false);
    }

    private LLVMValueRef GenerateBuiltinCall(BoundBuiltinCallExpr builtin)
    {
        return builtin.MethodKind switch
        {
            BuiltinMethodKind.ToPointer => GenerateArrayToPointer(builtin),
            BuiltinMethodKind.TypeCast => GenerateTypeCast(builtin),
            BuiltinMethodKind.ToSlice => GenerateArrayToSlice(builtin),
            _ => default,
        };
    }

    private LLVMValueRef GenerateArrayToPointer(BoundBuiltinCallExpr builtin)
    {
        // Get the address of the array variable
        var arrayAddr = GetTargetAddress(builtin.Receiver);
        if (arrayAddr.Handle == default) return default;

        // Build GEP to get address of first element: &arr[0]
        // arrayAddr points to [N x T], GEP indices [0, 0] to get T*
        var arrayLLVMType = GetLLVMType(builtin.Receiver.Type);
        var zero = LLVMValueRef.CreateConstInt(_context.Int32Type, 0, false);
        return _builder.BuildGEP2(arrayLLVMType, arrayAddr, new LLVMValueRef[] { zero, zero }, "array.ptr");
    }

    private LLVMValueRef GenerateTypeCast(BoundBuiltinCallExpr builtin)
    {
        var operand = GenerateExpression(builtin.Receiver);
        if (operand.Handle == default) return default;

        var srcType = builtin.Receiver.Type;
        var dstType = builtin.TypeArgument;

        var srcLLVM = GetLLVMType(srcType);
        var dstLLVM = GetLLVMType(dstType);

        var srcName = (srcType as PrimitiveType)?.Name ?? "";
        var dstName = (dstType as PrimitiveType)?.Name ?? "";
        var srcIsFloat = srcName == "f32" || srcName == "f64";
        var dstIsFloat = dstName == "f32" || dstName == "f64";
        var srcIsBool = srcName == "bool";
        var dstIsBool = dstName == "bool";
        var srcIsSigned = srcName.StartsWith("i");
        var dstIsSigned = dstName.StartsWith("i");
        var srcIsUnsigned = srcName.StartsWith("u");
        var dstIsUnsigned = dstName.StartsWith("u");

        // float → float: fpext / fptrunc
        if (srcIsFloat && dstIsFloat)
        {
            var srcBits = srcName == "f64" ? 64 : 32;
            var dstBits = dstName == "f64" ? 64 : 32;
            if (dstBits > srcBits)
                return _builder.BuildFPExt(operand, dstLLVM, "cast.fpext");
            else
                return _builder.BuildFPTrunc(operand, dstLLVM, "cast.fptrunc");
        }

        // int → float
        if (!srcIsFloat && !srcIsBool && dstIsFloat)
        {
            if (srcIsSigned)
                return _builder.BuildSIToFP(operand, dstLLVM, "cast.sitofp");
            else
                return _builder.BuildUIToFP(operand, dstLLVM, "cast.uitofp");
        }

        // float → int
        if (srcIsFloat && !dstIsFloat && !dstIsBool)
        {
            if (dstIsSigned)
                return _builder.BuildFPToSI(operand, dstLLVM, "cast.fptosi");
            else
                return _builder.BuildFPToUI(operand, dstLLVM, "cast.fptoui");
        }

        // bool → int: zext to target width
        if (srcIsBool && !dstIsBool)
        {
            return _builder.BuildZExt(operand, dstLLVM, "cast.bool2int");
        }

        // int/float → bool: compare != 0, then zext to i8
        if (!srcIsBool && dstIsBool)
        {
            if (srcIsFloat)
            {
                var zero = LLVMValueRef.CreateConstReal(srcLLVM, 0.0);
                var cmp = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, operand, zero, "tobool");
                return _builder.BuildZExt(cmp, dstLLVM, "cast.f2bool");
            }
            else
            {
                var zero = LLVMValueRef.CreateConstInt(srcLLVM, 0, false);
                var cmp = _builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, operand, zero, "tobool");
                return _builder.BuildZExt(cmp, dstLLVM, "cast.i2bool");
            }
        }

        // bool → bool: no-op
        if (srcIsBool && dstIsBool)
            return operand;

        // int → int: zext / trunc / bitcast
        var srcWidth = srcLLVM.IntWidth;
        var dstWidth = dstLLVM.IntWidth;

        if (dstWidth > srcWidth)
        {
            if (srcIsSigned)
                return _builder.BuildSExt(operand, dstLLVM, "cast.sext");
            else
                return _builder.BuildZExt(operand, dstLLVM, "cast.zext");
        }
        else if (dstWidth < srcWidth)
            return _builder.BuildTrunc(operand, dstLLVM, "cast.trunc");
        else
            return _builder.BuildBitCast(operand, dstLLVM, "cast.bitcast");
    }

    private LLVMValueRef GenerateArrayToSlice(BoundBuiltinCallExpr builtin)
    {
        // Get the address of the array variable
        var arrayAddr = GetTargetAddress(builtin.Receiver);
        if (arrayAddr.Handle == default) return default;

        var arrayType = builtin.Receiver.Type as ArrayType;
        var elemType = arrayType?.ElementType;

        // Get pointer to first element
        var arrayLLVMType = GetLLVMType(builtin.Receiver.Type);
        var zero = LLVMValueRef.CreateConstInt(_context.Int32Type, 0, false);
        var ptr = _builder.BuildGEP2(arrayLLVMType, arrayAddr, new LLVMValueRef[] { zero, zero }, "slice.ptr");

        // Array length (use compile-time size, or 0)
        var lenVal = LLVMValueRef.CreateConstInt(_context.Int64Type,
            (ulong)(arrayType?.Size ?? 0), false);

        // Build Slice struct { ptr, len } using alloca + field stores + load
        var elemLLVMType = GetLLVMType(elemType);
        var ptrFieldType = LLVMTypeRef.CreatePointer(elemLLVMType, 0);
        var sliceStructTy = LLVMTypeRef.CreateStruct(new[] { ptrFieldType, _context.Int64Type }, false);
        var sliceAlloca = _builder.BuildAlloca(sliceStructTy, "slice.tmp");
        var slicePtrField = _builder.BuildStructGEP2(sliceStructTy, sliceAlloca, 0u, "slice.ptr");
        _builder.BuildStore(ptr, slicePtrField);
        var sliceLenField = _builder.BuildStructGEP2(sliceStructTy, sliceAlloca, 1u, "slice.len");
        _builder.BuildStore(lenVal, sliceLenField);
        return _builder.BuildLoad2(sliceStructTy, sliceAlloca, "slice.val");
    }
}
