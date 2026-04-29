using Antlr4.Runtime.Tree;
using LLVMSharp;
using LLVMSharp.Interop;
using SolidLang.Compilers.Symbols;
using SolidLang.Compilers.Types;

namespace SolidLang.Compilers.CodeGen;

public sealed unsafe class LLVMCodeGenerator : IDisposable
{
    private readonly LLVMModuleRef _module;
    private readonly LLVMBuilderRef _builder;
    private readonly Dictionary<string, (LLVMValueRef Ptr, LLVMTypeRef Type)> _namedValues = new();
    private readonly Dictionary<string, FunctionSymbol> _functions = new();
    private readonly Dictionary<string, LLVMTypeRef> _functionTypes = new();

    public LLVMCodeGenerator(string moduleName)
    {
        LLVM.InitializeNativeTarget();
        LLVM.InitializeNativeAsmPrinter();

        _module = LLVMModuleRef.CreateWithName(moduleName);
        _module.Target = LLVMTargetRef.DefaultTriple;
        _builder = LLVMBuilderRef.Create(_module.Context);
    }

    public void SetFunctions(IReadOnlyList<FunctionSymbol> functions)
    {
        foreach (var func in functions)
        {
            _functions[func.Name] = func;
        }
    }

    public void GenerateModule(SolidLangParser.ProgramContext program)
    {
        var children = program.children;
        if (children == null) return;

        foreach (var child in children)
        {
            if (child is SolidLangParser.Func_decl_stmtContext funcDecl)
            {
                GenerateFunction(funcDecl);
            }
        }
    }

    private void GenerateFunction(SolidLangParser.Func_decl_stmtContext context)
    {
        _namedValues.Clear();

        var header = context.func_decl_header();
        var funcName = header.ID().GetText();
        var funcSymbol = _functions[funcName];

        var paramTypes = funcSymbol.Parameters
            .Select(p => GetLLVMType(p.Type))
            .ToArray();

        var returnType = GetLLVMType(funcSymbol.ReturnType);
        var funcType = LLVMTypeRef.CreateFunction(returnType, paramTypes);

        var func = _module.AddFunction(funcName, funcType);
        _functionTypes[funcName] = funcType;

        var entry = func.AppendBasicBlock("entry");
        _builder.PositionAtEnd(entry);

        for (uint i = 0; i < funcSymbol.Parameters.Count; i++)
        {
            var param = funcSymbol.Parameters[(int)i];
            var paramValue = func.GetParam(i);
            var paramType = GetLLVMType(param.Type);

            var alloca = _builder.BuildAlloca(paramType, param.Name);
            _builder.BuildStore(paramValue, alloca);
            _namedValues[param.Name] = (alloca, paramType);
        }

        var body = context.body_stmt();
        GenerateBody(body, func, funcSymbol.ReturnType);
    }

    private void GenerateBody(SolidLangParser.Body_stmtContext body, LLVMValueRef func, SolidType returnType)
    {
        foreach (var stmt in body.stmt())
        {
            GenerateStatement(stmt, func, returnType);
        }
    }

    private void GenerateStatement(SolidLangParser.StmtContext stmt, LLVMValueRef func, SolidType returnType)
    {
        if (stmt.return_stmt() is { } returnStmt)
        {
            GenerateReturn(returnStmt, returnType);
        }
        else if (stmt.var_decl_stmt() is { } varDecl)
        {
            GenerateVarDecl(varDecl);
        }
    }

    private void GenerateVarDecl(SolidLangParser.Var_decl_stmtContext varDecl)
    {
        var varName = varDecl.ID().GetText();
        var varType = GetLLVMType(varDecl.type().GetText());

        var alloca = _builder.BuildAlloca(varType, varName);

        if (varDecl.expr() is { } initExpr)
        {
            var initValue = GenerateExpression(initExpr);
            _builder.BuildStore(initValue, alloca);
        }

        _namedValues[varName] = (alloca, varType);
    }

    private void GenerateReturn(SolidLangParser.Return_stmtContext returnStmt, SolidType returnType)
    {
        if (returnStmt.expr() is { } expr)
        {
            var value = GenerateExpression(expr);
            _builder.BuildRet(value);
        }
        else
        {
            _builder.BuildRetVoid();
        }
    }

    private LLVMValueRef GenerateExpression(SolidLangParser.ExprContext expr)
    {
        return GenerateConditionalExpr(expr.conditional_expr());
    }

    private LLVMValueRef GenerateConditionalExpr(SolidLangParser.Conditional_exprContext condExpr)
    {
        var orExpr = condExpr.or_expr();

        if (condExpr.expr() is { } thenExpr && condExpr.conditional_expr() is { } elseExpr)
        {
            var cond = GenerateOrExpr(orExpr);
            var thenValue = GenerateExpression(thenExpr);
            var elseValue = GenerateConditionalExpr(elseExpr);
            return _builder.BuildSelect(cond, thenValue, elseValue, "condtmp");
        }

        return GenerateOrExpr(orExpr);
    }

    private LLVMValueRef GenerateOrExpr(SolidLangParser.Or_exprContext orExpr)
    {
        var andExprs = orExpr.and_expr();
        var result = GenerateAndExpr(andExprs[0]);

        for (int i = 1; i < andExprs.Length; i++)
        {
            var rhs = GenerateAndExpr(andExprs[i]);
            result = _builder.BuildOr(result, rhs, "ortmp");
        }

        return result;
    }

    private LLVMValueRef GenerateAndExpr(SolidLangParser.And_exprContext andExpr)
    {
        var bitOrExprs = andExpr.bit_or_expr();
        var result = GenerateBitOrExpr(bitOrExprs[0]);

        for (int i = 1; i < bitOrExprs.Length; i++)
        {
            var rhs = GenerateBitOrExpr(bitOrExprs[i]);
            result = _builder.BuildAnd(result, rhs, "andtmp");
        }

        return result;
    }

    private LLVMValueRef GenerateBitOrExpr(SolidLangParser.Bit_or_exprContext bitOrExpr)
    {
        var bitXorExprs = bitOrExpr.bit_xor_expr();
        var result = GenerateBitXorExpr(bitXorExprs[0]);

        for (int i = 1; i < bitXorExprs.Length; i++)
        {
            var rhs = GenerateBitXorExpr(bitXorExprs[i]);
            result = _builder.BuildOr(result, rhs, "bortmp");
        }

        return result;
    }

    private LLVMValueRef GenerateBitXorExpr(SolidLangParser.Bit_xor_exprContext bitXorExpr)
    {
        var bitAndExprs = bitXorExpr.bit_and_expr();
        var result = GenerateBitAndExpr(bitAndExprs[0]);

        for (int i = 1; i < bitAndExprs.Length; i++)
        {
            var rhs = GenerateBitAndExpr(bitAndExprs[i]);
            result = _builder.BuildXor(result, rhs, "xortmp");
        }

        return result;
    }

    private LLVMValueRef GenerateBitAndExpr(SolidLangParser.Bit_and_exprContext bitAndExpr)
    {
        var eqExprs = bitAndExpr.eq_expr();
        var result = GenerateEqExpr(eqExprs[0]);

        for (int i = 1; i < eqExprs.Length; i++)
        {
            var rhs = GenerateEqExpr(eqExprs[i]);
            result = _builder.BuildAnd(result, rhs, "bandtmp");
        }

        return result;
    }

    private LLVMValueRef GenerateEqExpr(SolidLangParser.Eq_exprContext eqExpr)
    {
        var cmpExprs = eqExpr.cmp_expr();
        var result = GenerateCmpExpr(cmpExprs[0]);

        var children = eqExpr.children;
        if (children == null) return result;

        var ops = children.OfType<ITerminalNode>().ToArray();

        for (int i = 1; i < cmpExprs.Length && i - 1 < ops.Length; i++)
        {
            var rhs = GenerateCmpExpr(cmpExprs[i]);
            var op = ops[i - 1].Symbol.Type;

            if (op == SolidLangLexer.EQEQ)
            {
                result = _builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, result, rhs, "eqtmp");
            }
            else if (op == SolidLangLexer.NOTEQ)
            {
                result = _builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, result, rhs, "netmp");
            }
        }

        return result;
    }

    private LLVMValueRef GenerateCmpExpr(SolidLangParser.Cmp_exprContext cmpExpr)
    {
        var shiftExprs = cmpExpr.shift_expr();
        var result = GenerateShiftExpr(shiftExprs[0]);

        var children = cmpExpr.children;
        if (children == null) return result;

        var ops = children.OfType<ITerminalNode>().ToArray();

        for (int i = 1; i < shiftExprs.Length && i - 1 < ops.Length; i++)
        {
            var rhs = GenerateShiftExpr(shiftExprs[i]);
            var op = ops[i - 1].Symbol.Type;

            result = op switch
            {
                SolidLangLexer.LT => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, result, rhs, "lttmp"),
                SolidLangLexer.GT => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, result, rhs, "gttmp"),
                SolidLangLexer.LE => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, result, rhs, "letmp"),
                SolidLangLexer.GE => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, result, rhs, "getmp"),
                _ => result
            };
        }

        return result;
    }

    private LLVMValueRef GenerateShiftExpr(SolidLangParser.Shift_exprContext shiftExpr)
    {
        var addExprs = shiftExpr.add_expr();
        var result = GenerateAddExpr(addExprs[0]);

        var children = shiftExpr.children;
        if (children == null) return result;

        var ops = children.OfType<ITerminalNode>().ToArray();

        for (int i = 1; i < addExprs.Length && i - 1 < ops.Length; i++)
        {
            var rhs = GenerateAddExpr(addExprs[i]);
            var op = ops[i - 1].Symbol.Type;

            result = op switch
            {
                SolidLangLexer.SHL => _builder.BuildShl(result, rhs, "shltmp"),
                SolidLangLexer.SHR => _builder.BuildAShr(result, rhs, "shrtmp"),
                _ => result
            };
        }

        return result;
    }

    private LLVMValueRef GenerateAddExpr(SolidLangParser.Add_exprContext addExpr)
    {
        var mulExprs = addExpr.mul_expr();
        var result = GenerateMulExpr(mulExprs[0]);

        var children = addExpr.children;
        if (children == null) return result;

        var ops = children.OfType<ITerminalNode>().ToArray();

        for (int i = 1; i < mulExprs.Length && i - 1 < ops.Length; i++)
        {
            var rhs = GenerateMulExpr(mulExprs[i]);
            var op = ops[i - 1].Symbol.Type;

            result = op switch
            {
                SolidLangLexer.PLUS => _builder.BuildAdd(result, rhs, "addtmp"),
                SolidLangLexer.MINUS => _builder.BuildSub(result, rhs, "subtmp"),
                _ => result
            };
        }

        return result;
    }

    private LLVMValueRef GenerateMulExpr(SolidLangParser.Mul_exprContext mulExpr)
    {
        var unaryExprs = mulExpr.unary_expr();
        var result = GenerateUnaryExpr(unaryExprs[0]);

        var children = mulExpr.children;
        if (children == null) return result;

        var ops = children.OfType<ITerminalNode>().ToArray();

        for (int i = 1; i < unaryExprs.Length && i - 1 < ops.Length; i++)
        {
            var rhs = GenerateUnaryExpr(unaryExprs[i]);
            var op = ops[i - 1].Symbol.Type;

            result = op switch
            {
                SolidLangLexer.STAR => _builder.BuildMul(result, rhs, "multmp"),
                SolidLangLexer.SLASH => _builder.BuildSDiv(result, rhs, "divtmp"),
                SolidLangLexer.MOD => _builder.BuildSRem(result, rhs, "modtmp"),
                _ => result
            };
        }

        return result;
    }

    private LLVMValueRef GenerateUnaryExpr(SolidLangParser.Unary_exprContext unaryExpr)
    {
        if (unaryExpr.postfix_expr() is { } postfixExpr)
        {
            return GeneratePostfixExpr(postfixExpr);
        }

        var children = unaryExpr.children;
        if (children == null)
            throw new InvalidOperationException("Invalid unary expression");

        var op = children.OfType<ITerminalNode>().FirstOrDefault();

        if (op == null)
            throw new InvalidOperationException("Invalid unary expression");

        var operand = GenerateUnaryExpr(unaryExpr.unary_expr());

        return op.Symbol.Type switch
        {
            SolidLangLexer.MINUS => _builder.BuildNeg(operand, "negtmp"),
            SolidLangLexer.NOT => _builder.BuildNot(operand, "nottmp"),
            SolidLangLexer.TILDE => _builder.BuildNot(operand, "invtmp"),
            _ => throw new NotSupportedException($"Unknown unary operator: {op.GetText()}")
        };
    }

    private LLVMValueRef GeneratePostfixExpr(SolidLangParser.Postfix_exprContext postfixExpr)
    {
        var result = GeneratePrimaryExpr(postfixExpr.primary_expr());

        foreach (var suffix in postfixExpr.postfix_suffix())
        {
            if (suffix.LPAREN() != null)
            {
                result = GenerateCall(result, suffix.call_args());
            }
        }

        return result;
    }

    private LLVMValueRef GenerateCall(LLVMValueRef callee, SolidLangParser.Call_argsContext? callArgs)
    {
        var args = new List<LLVMValueRef>();

        if (callArgs != null)
        {
            foreach (var expr in callArgs.expr())
            {
                args.Add(GenerateExpression(expr));
            }
        }

        // Get function name from callee
        var funcName = callee.Name;
        LLVMTypeRef funcType;

        // Look up stored function type
        if (!string.IsNullOrEmpty(funcName) && _functionTypes.TryGetValue(funcName, out var storedType))
        {
            funcType = storedType;
        }
        else
        {
            // Fallback: construct function type from arguments (assuming i32 return type)
            var returnType = LLVMTypeRef.Int32;
            var paramTypes = args.Select(a => a.TypeOf).ToArray();
            funcType = LLVMTypeRef.CreateFunction(returnType, paramTypes);
        }

        return _builder.BuildCall2(funcType, callee, args.ToArray(), "calltmp");
    }

    private LLVMValueRef GeneratePrimaryExpr(SolidLangParser.Primary_exprContext primaryExpr)
    {
        if (primaryExpr.literal() is { } literal)
        {
            return GenerateLiteral(literal);
        }

        if (primaryExpr.ID() is { } id)
        {
            var name = id.GetText();
            if (_namedValues.TryGetValue(name, out var valueInfo))
            {
                return _builder.BuildLoad2(valueInfo.Type, valueInfo.Ptr, name);
            }

            if (_functions.TryGetValue(name, out _))
            {
                var func = _module.GetNamedFunction(name);
                if (func.Handle != IntPtr.Zero)
                    return func;
            }

            throw new InvalidOperationException($"Unknown variable or function: {name}");
        }

        if (primaryExpr.expr() is { } expr)
        {
            return GenerateExpression(expr);
        }

        throw new NotSupportedException("Unsupported primary expression");
    }

    private LLVMValueRef GenerateLiteral(SolidLangParser.LiteralContext literal)
    {
        if (literal.INTEGER_LITERAL() is { } intLit)
        {
            var text = intLit.GetText();
            var suffixIndex = text.IndexOfAny(new[] { 'i', 'u' });
            if (suffixIndex > 0)
                text = text.Substring(0, suffixIndex);

            var value = long.Parse(text);
            return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)value);
        }

        if (literal.BOOL_LITERAL() is { } boolLit)
        {
            var value = boolLit.GetText() == "true" ? 1UL : 0UL;
            return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, value);
        }

        throw new NotSupportedException($"Unsupported literal: {literal.GetText()}");
    }

    private LLVMTypeRef GetLLVMType(SolidType type)
    {
        return type switch
        {
            I32Type => LLVMTypeRef.Int32,
            BoolType => LLVMTypeRef.Int1,
            VoidType => LLVMTypeRef.Void,
            _ => throw new NotSupportedException($"Unknown type: {type.Name}")
        };
    }

    private LLVMTypeRef GetLLVMType(string typeName)
    {
        return typeName switch
        {
            "i32" => LLVMTypeRef.Int32,
            "bool" => LLVMTypeRef.Int1,
            "void" => LLVMTypeRef.Void,
            _ => throw new NotSupportedException($"Unknown type: {typeName}")
        };
    }

    public string GetIR()
    {
        return _module.PrintToString();
    }

    public bool WriteBitcode(string filePath)
    {
        return _module.WriteBitcodeToFile(filePath) == 0;
    }

    public void EmitObjectFile(string objectFilePath)
    {
        var targetTriple = LLVMTargetRef.DefaultTriple;
        _module.Target = targetTriple;

        if (!LLVMTargetRef.TryGetTargetFromTriple(targetTriple, out var target, out var error))
        {
            throw new InvalidOperationException($"Failed to get target: {error}");
        }

        var targetMachine = target.CreateTargetMachine(
            targetTriple,
            "generic",
            "",
            LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
            LLVMRelocMode.LLVMRelocDefault,
            LLVMCodeModel.LLVMCodeModelDefault
        );

        targetMachine.EmitToFile(_module, objectFilePath, LLVMCodeGenFileType.LLVMObjectFile);
    }

    public void Dispose()
    {
        _builder.Dispose();
        _module.Dispose();
    }
}
