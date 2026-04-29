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
    private readonly Dictionary<string, string> _namedValueTypes = new(); // variable name -> type name
    private readonly Dictionary<string, FunctionSymbol> _functions = new();
    private readonly Dictionary<string, LLVMTypeRef> _functionTypes = new();
    private readonly Dictionary<string, LLVMTypeRef> _userTypes = new();
    private readonly Dictionary<string, Dictionary<string, uint>> _structFieldIndices = new();
    private readonly Dictionary<string, LLVMTypeRef> _structFieldTypes = new();

    // Loop context for break/continue
    private readonly Stack<(LLVMBasicBlockRef ContinueBB, LLVMBasicBlockRef BreakBB)> _loopContexts = new();

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

        // First pass: collect all struct/union/enum declarations
        foreach (var child in children)
        {
            if (child is SolidLangParser.Struct_decl_stmtContext structDecl)
            {
                DeclareStructType(structDecl);
            }
            else if (child is SolidLangParser.Union_decl_stmtContext unionDecl)
            {
                DeclareUnionType(unionDecl);
            }
            else if (child is SolidLangParser.Enum_decl_stmtContext enumDecl)
            {
                DeclareEnumType(enumDecl);
            }
        }

        // Second pass: generate functions
        foreach (var child in children)
        {
            if (child is SolidLangParser.Func_decl_stmtContext funcDecl)
            {
                GenerateFunction(funcDecl);
            }
        }
    }

    private void DeclareStructType(SolidLangParser.Struct_decl_stmtContext context)
    {
        var name = context.ID().GetText();
        var fieldTypes = new List<LLVMTypeRef>();
        var fieldIndices = new Dictionary<string, uint>();
        var fieldTypeMap = new Dictionary<string, LLVMTypeRef>();

        var structFields = context.struct_fields();
        if (structFields != null)
        {
            uint index = 0;
            foreach (var field in structFields.struct_field())
            {
                var fieldName = field.ID().GetText();
                var fieldType = GetLLVMTypeFromContext(field.type());
                fieldTypes.Add(fieldType);
                fieldIndices[fieldName] = index;
                fieldTypeMap[$"{name}.{fieldName}"] = fieldType;
                index++;
            }
        }

        var structType = LLVMTypeRef.CreateStruct(fieldTypes.ToArray(), false);
        _userTypes[name] = structType;
        _structFieldIndices[name] = fieldIndices;
        foreach (var kvp in fieldTypeMap)
        {
            _structFieldTypes[kvp.Key] = kvp.Value;
        }
    }

    private void DeclareUnionType(SolidLangParser.Union_decl_stmtContext context)
    {
        var name = context.ID().GetText();
        // Union represented as largest field size
        var unionType = LLVMTypeRef.Int64; // Simplified: use i64
        _userTypes[name] = unionType;
    }

    private void DeclareEnumType(SolidLangParser.Enum_decl_stmtContext context)
    {
        var name = context.ID().GetText();
        var enumType = context.type() != null
            ? GetLLVMTypeFromContext(context.type())
            : LLVMTypeRef.Int32;
        _userTypes[name] = enumType;
    }

    private void GenerateFunction(SolidLangParser.Func_decl_stmtContext context)
    {
        _namedValues.Clear();
        _namedValueTypes.Clear();

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
            // Store type name for struct types
            if (param.Type is Types.StructType st)
                _namedValueTypes[param.Name] = st.Name;
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
        else if (stmt.if_stmt() is { } ifStmt)
        {
            GenerateIfStmt(ifStmt, func, returnType);
        }
        else if (stmt.while_stmt() is { } whileStmt)
        {
            GenerateWhileStmt(whileStmt, func, returnType);
        }
        else if (stmt.for_stmt() is { } forStmt)
        {
            GenerateForStmt(forStmt, func, returnType);
        }
        else if (stmt.assign_stmt() is { } assignStmt)
        {
            GenerateAssignStmt(assignStmt);
        }
        else if (stmt.expr_stmt() is { } exprStmt)
        {
            GenerateExpression(exprStmt.expr());
        }
        else if (stmt.break_stmt() is { })
        {
            if (_loopContexts.Count > 0)
            {
                var (_, breakBB) = _loopContexts.Peek();
                _builder.BuildBr(breakBB);
            }
        }
        else if (stmt.continue_stmt() is { })
        {
            if (_loopContexts.Count > 0)
            {
                var (continueBB, _) = _loopContexts.Peek();
                _builder.BuildBr(continueBB);
            }
        }
        else if (stmt.switch_stmt() is { } switchStmt)
        {
            GenerateSwitchStmt(switchStmt, func, returnType);
        }
    }

    private void GenerateIfStmt(SolidLangParser.If_stmtContext ifStmt, LLVMValueRef func, SolidType returnType)
    {
        var condValue = GenerateExpression(ifStmt.expr());
        var condBool = _builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, condValue,
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0), "ifcond");

        var thenBB = func.AppendBasicBlock("then");
        var elseBB = func.AppendBasicBlock("else");
        var mergeBB = func.AppendBasicBlock("ifcont");

        _builder.BuildCondBr(condBool, thenBB, elseBB);

        _builder.PositionAtEnd(thenBB);
        var thenBody = ifStmt.body_stmt();
        if (thenBody != null && thenBody.Length > 0)
        {
            GenerateBody(thenBody[0], func, returnType);
        }
        if (thenBB.Terminator.Handle == IntPtr.Zero)
        {
            _builder.BuildBr(mergeBB);
        }

        _builder.PositionAtEnd(elseBB);
        if (ifStmt.ELSE() != null)
        {
            var elseBody = ifStmt.body_stmt();
            if (elseBody != null && elseBody.Length > 1)
            {
                GenerateBody(elseBody[1], func, returnType);
            }
            else
            {
                var nestedIfs = ifStmt.if_stmt();
                if (nestedIfs != null && nestedIfs.Length > 0)
                {
                    GenerateIfStmt(nestedIfs[0], func, returnType);
                }
            }
        }
        if (elseBB.Terminator.Handle == IntPtr.Zero)
        {
            _builder.BuildBr(mergeBB);
        }

        _builder.PositionAtEnd(mergeBB);
    }

    private void GenerateWhileStmt(SolidLangParser.While_stmtContext whileStmt, LLVMValueRef func, SolidType returnType)
    {
        var condBB = func.AppendBasicBlock("whilecond");
        var bodyBB = func.AppendBasicBlock("whilebody");
        var afterBB = func.AppendBasicBlock("whilecont");

        _builder.BuildBr(condBB);

        _builder.PositionAtEnd(condBB);
        var condValue = GenerateExpression(whileStmt.expr());
        var condBool = _builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, condValue,
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0), "whilecond");
        _builder.BuildCondBr(condBool, bodyBB, afterBB);

        _builder.PositionAtEnd(bodyBB);
        _loopContexts.Push((condBB, afterBB));

        if (whileStmt.body_stmt() is { } body)
        {
            GenerateBody(body, func, returnType);
        }

        _loopContexts.Pop();

        var currentBB = _builder.InsertBlock;
        if (currentBB.Handle != IntPtr.Zero && currentBB.Terminator.Handle == IntPtr.Zero)
        {
            _builder.BuildBr(condBB);
        }

        _builder.PositionAtEnd(afterBB);
    }

    private void GenerateForStmt(SolidLangParser.For_stmtContext forStmt, LLVMValueRef func, SolidType returnType)
    {
        var condBB = func.AppendBasicBlock("forcond");
        var bodyBB = func.AppendBasicBlock("forbody");
        var updateBB = func.AppendBasicBlock("forupdate");
        var afterBB = func.AppendBasicBlock("forcont");

        if (forStmt.var_decl_pseudo_expr() is { } initDecl)
        {
            var varName = initDecl.ID().GetText();
            var varType = GetLLVMTypeFromContext(initDecl.type());
            var alloca = _builder.BuildAlloca(varType, varName);

            if (initDecl.expr() is { } initExpr)
            {
                var initValue = GenerateExpression(initExpr);
                _builder.BuildStore(initValue, alloca);
            }
            _namedValues[varName] = (alloca, varType);
        }

        _builder.BuildBr(condBB);

        _builder.PositionAtEnd(condBB);
        if (forStmt.expr() is { } condExpr)
        {
            var condValue = GenerateExpression(condExpr);
            var condBool = _builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, condValue,
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0), "forcond");
            _builder.BuildCondBr(condBool, bodyBB, afterBB);
        }
        else
        {
            _builder.BuildBr(bodyBB);
        }

        _builder.PositionAtEnd(bodyBB);
        _loopContexts.Push((updateBB, afterBB));
        if (forStmt.body_stmt() is { } body)
        {
            GenerateBody(body, func, returnType);
        }
        _loopContexts.Pop();

        if (bodyBB.Terminator.Handle == IntPtr.Zero)
        {
            _builder.BuildBr(updateBB);
        }

        _builder.PositionAtEnd(updateBB);
        if (forStmt.assign_pseudo_expr() is { } update)
        {
            GenerateAssignPseudoExpr(update);
        }
        _builder.BuildBr(condBB);

        _builder.PositionAtEnd(afterBB);
    }

    private void GenerateSwitchStmt(SolidLangParser.Switch_stmtContext switchStmt, LLVMValueRef func, SolidType returnType)
    {
        var switchValue = GenerateExpression(switchStmt.expr());
        var defaultBB = func.AppendBasicBlock("switch.default");
        var afterBB = func.AppendBasicBlock("switch.after");

        var cases = switchStmt.switch_cases()?.switch_case();
        if (cases == null || cases.Length == 0)
        {
            _builder.BuildBr(afterBB);
            _builder.PositionAtEnd(defaultBB);
            _builder.BuildBr(afterBB);
            _builder.PositionAtEnd(afterBB);
            return;
        }

        var caseValues = new List<(LLVMValueRef Value, LLVMBasicBlockRef Block)>();

        foreach (var switchCase in cases)
        {
            var labels = switchCase.switch_labels().switch_label();
            foreach (var label in labels)
            {
                if (label.ELSE() != null) continue;

                var caseExpr = label.expr();
                var caseValue = GenerateExpression(caseExpr);
                var caseBB = func.AppendBasicBlock("switch.case");
                caseValues.Add((caseValue, caseBB));
            }
        }

        var switchInst = _builder.BuildSwitch(switchValue, defaultBB, (uint)caseValues.Count);

        var caseIndex = 0;
        foreach (var switchCase in cases)
        {
            var labels = switchCase.switch_labels().switch_label();
            foreach (var label in labels)
            {
                if (label.ELSE() != null) continue;

                var (caseValue, caseBB) = caseValues[caseIndex++];
                switchInst.AddCase(caseValue, caseBB);
            }
        }

        caseIndex = 0;
        foreach (var switchCase in cases)
        {
            var labels = switchCase.switch_labels().switch_label();
            foreach (var label in labels)
            {
                if (label.ELSE() != null)
                {
                    _builder.PositionAtEnd(defaultBB);
                }
                else
                {
                    var (_, caseBB) = caseValues[caseIndex++];
                    _builder.PositionAtEnd(caseBB);
                }

                GenerateStatement(switchCase.stmt(), func, returnType);

                if (_builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
                {
                    _builder.BuildBr(afterBB);
                }
            }
        }

        _builder.PositionAtEnd(defaultBB);
        if (defaultBB.Terminator.Handle == IntPtr.Zero)
        {
            _builder.BuildBr(afterBB);
        }

        _builder.PositionAtEnd(afterBB);
    }

    private void GenerateAssignStmt(SolidLangParser.Assign_stmtContext assignStmt)
    {
        GenerateAssignPseudoExpr(assignStmt.assign_pseudo_expr());
    }

    private void GenerateAssignPseudoExpr(SolidLangParser.Assign_pseudo_exprContext assign)
    {
        var targetExpr = assign.expr(0);
        var valueExpr = assign.expr(1);

        var (ptr, type) = GetLValue(targetExpr);
        if (ptr.Handle != IntPtr.Zero)
        {
            var value = GenerateExpression(valueExpr);

            var children = assign.children;
            if (children == null) return;

            var op = children.OfType<ITerminalNode>().FirstOrDefault();
            if (op == null) return;

            if (op.Symbol.Type == SolidLangLexer.EQ)
            {
                _builder.BuildStore(value, ptr);
            }
            else
            {
                var currentValue = _builder.BuildLoad2(type, ptr, "loadtmp");
                LLVMValueRef newValue = op.Symbol.Type switch
                {
                    SolidLangLexer.PLUSEQ => _builder.BuildAdd(currentValue, value, "addtmp"),
                    SolidLangLexer.MINUSEQ => _builder.BuildSub(currentValue, value, "subtmp"),
                    SolidLangLexer.STAREQ => _builder.BuildMul(currentValue, value, "multmp"),
                    SolidLangLexer.SLASHEQ => _builder.BuildSDiv(currentValue, value, "divtmp"),
                    SolidLangLexer.PERCENTEQ => _builder.BuildSRem(currentValue, value, "modtmp"),
                    _ => throw new NotSupportedException($"Unsupported assignment operator: {op.GetText()}")
                };
                _builder.BuildStore(newValue, ptr);
            }
        }
    }

    private (LLVMValueRef Ptr, LLVMTypeRef Type) GetLValue(SolidLangParser.ExprContext expr)
    {
        // Check for dereference: *ptr = value
        var unaryExpr = expr.conditional_expr().or_expr().and_expr(0).bit_or_expr(0)
            .bit_xor_expr(0).bit_and_expr(0).eq_expr(0).cmp_expr(0)
            .shift_expr(0).add_expr(0).mul_expr(0).unary_expr(0);

        // Handle *ptr = value
        if (unaryExpr != null && unaryExpr.children != null)
        {
            var op = unaryExpr.children.OfType<ITerminalNode>().FirstOrDefault();
            if (op != null && op.Symbol.Type == SolidLangLexer.STAR)
            {
                // Get the pointer value
                var ptrValue = GenerateUnaryExpr(unaryExpr.unary_expr());
                // Return the pointer as the lvalue
                return (ptrValue, LLVMTypeRef.Int32); // Assume i32 element type
            }
        }

        var postfix = unaryExpr?.postfix_expr();
        if (postfix == null) return (default, default);

        var primaryExpr = postfix.primary_expr();
        if (primaryExpr?.ID() is { } id)
        {
            var name = id.GetText();
            if (_namedValues.TryGetValue(name, out var varInfo))
            {
                var suffixes = postfix.postfix_suffix();
                if (suffixes == null || suffixes.Length == 0)
                {
                    return varInfo;
                }

                var currentPtr = varInfo.Ptr;
                var currentType = varInfo.Type;
                var currentTypeName = _namedValueTypes.TryGetValue(name, out var tn) ? tn : FindTypeName(currentType);
                var isPointer = false;

                // Check if this is a pointer type
                if (currentTypeName != null && currentTypeName.StartsWith("*"))
                {
                    isPointer = true;
                    // Load the pointer value
                    currentPtr = _builder.BuildLoad2(currentType, currentPtr, "ptrload");
                    var elementTypeName = currentTypeName.Substring(1);
                    if (_userTypes.TryGetValue(elementTypeName, out var elementType))
                    {
                        currentType = elementType;
                        currentTypeName = elementTypeName;
                    }
                }

                foreach (var suffix in suffixes)
                {
                    // Handle dot operator: p.x
                    if (suffix.DOT() != null && suffix.ID() is { } memberId)
                    {
                        var memberName = memberId.GetText();
                        if (currentTypeName != null && _structFieldIndices.TryGetValue(currentTypeName, out var fields))
                        {
                            if (fields.TryGetValue(memberName, out var fieldIndex))
                            {
                                var idx0 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
                                var idx1 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, fieldIndex);
                                currentPtr = _builder.BuildInBoundsGEP2(currentType, currentPtr, new[] { idx0, idx1 }, memberName);
                                var fieldKey = $"{currentTypeName}.{memberName}";
                                currentType = _structFieldTypes.TryGetValue(fieldKey, out var ft) ? ft : LLVMTypeRef.Int32;
                                currentTypeName = FindTypeName(currentType);
                            }
                        }
                    }
                    // Handle arrow operator: p->x
                    else if (suffix.MINUSRARROW() != null && suffix.ID() is { } arrowMemberId)
                    {
                        var memberName = arrowMemberId.GetText();
                        if (currentTypeName != null && _structFieldIndices.TryGetValue(currentTypeName, out var fields))
                        {
                            if (fields.TryGetValue(memberName, out var fieldIndex))
                            {
                                var idx0 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
                                var idx1 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, fieldIndex);
                                currentPtr = _builder.BuildInBoundsGEP2(currentType, currentPtr, new[] { idx0, idx1 }, memberName);
                                var fieldKey = $"{currentTypeName}.{memberName}";
                                currentType = _structFieldTypes.TryGetValue(fieldKey, out var ft) ? ft : LLVMTypeRef.Int32;
                                currentTypeName = FindTypeName(currentType);
                            }
                        }
                    }
                }

                return (currentPtr, currentType);
            }
        }

        return (default, default);
    }

    private void GenerateVarDecl(SolidLangParser.Var_decl_stmtContext varDecl)
    {
        var varName = varDecl.ID().GetText();
        var typeName = varDecl.type()?.GetText();
        var varType = varDecl.type() != null
            ? GetLLVMTypeFromContext(varDecl.type())
            : LLVMTypeRef.Int32;

        var alloca = _builder.BuildAlloca(varType, varName);

        if (varDecl.expr() is { } initExpr)
        {
            var initValue = GenerateExpression(initExpr);
            _builder.BuildStore(initValue, alloca);
        }

        _namedValues[varName] = (alloca, varType);
        // Store type name for user-defined types and pointer types
        if (typeName != null && (_userTypes.ContainsKey(typeName) || typeName.StartsWith("*")))
            _namedValueTypes[varName] = typeName;
    }

    private void GenerateReturn(SolidLangParser.Return_stmtContext returnStmt, SolidType returnType)
    {
        if (returnStmt.expr() is { } expr)
        {
            var value = GenerateExpression(returnStmt.expr());
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

        var children = bitAndExpr.children;
        if (children == null) return result;

        var ops = children.OfType<ITerminalNode>().ToArray();

        for (int i = 1; i < eqExprs.Length && i - 1 < ops.Length; i++)
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

        // Handle address-of operator &
        if (op.Symbol.Type == SolidLangLexer.AND)
        {
            return GenerateAddressOf(unaryExpr.unary_expr());
        }

        // Handle dereference operator *
        if (op.Symbol.Type == SolidLangLexer.STAR)
        {
            var ptrValue = GenerateUnaryExpr(unaryExpr.unary_expr());
            // We need the element type - assume i32 for now
            return _builder.BuildLoad2(LLVMTypeRef.Int32, ptrValue, "deref");
        }

        var operand = GenerateUnaryExpr(unaryExpr.unary_expr());

        return op.Symbol.Type switch
        {
            SolidLangLexer.MINUS => _builder.BuildNeg(operand, "negtmp"),
            SolidLangLexer.NOT => _builder.BuildNot(operand, "nottmp"),
            SolidLangLexer.TILDE => _builder.BuildNot(operand, "invtmp"),
            _ => throw new NotSupportedException($"Unknown unary operator: {op.GetText()}")
        };
    }

    private LLVMValueRef GenerateAddressOf(SolidLangParser.Unary_exprContext unaryExpr)
    {
        // Get the lvalue (pointer) for the expression
        var postfix = unaryExpr.postfix_expr();
        if (postfix == null)
            throw new InvalidOperationException("Cannot take address of this expression");

        var primaryExpr = postfix.primary_expr();
        if (primaryExpr?.ID() is { } id)
        {
            var name = id.GetText();
            if (_namedValues.TryGetValue(name, out var varInfo))
            {
                // The variable is already stored as a pointer, return it directly
                return varInfo.Ptr;
            }
        }

        throw new InvalidOperationException("Cannot take address of this expression");
    }

    private LLVMValueRef GeneratePostfixExpr(SolidLangParser.Postfix_exprContext postfixExpr)
    {
        var suffixes = postfixExpr.postfix_suffix();

        // Check if we have member access
        if (suffixes != null && suffixes.Length > 0)
        {
            var hasMemberAccess = suffixes.Any(s => s.DOT() != null || s.MINUSRARROW() != null);
            var hasCall = suffixes.Any(s => s.LPAREN() != null);

            if (hasMemberAccess)
            {
                // Handle member access: p.x or p->x (through pointer)
                var primaryExpr = postfixExpr.primary_expr();
                if (primaryExpr?.ID() is { } id)
                {
                    var name = id.GetText();
                    if (_namedValues.TryGetValue(name, out var varInfo))
                    {
                        var resultPtr = varInfo.Ptr;
                        var resultType = varInfo.Type;
                        var isPointer = false;

                        // Check if this is a pointer type
                        if (_namedValueTypes.TryGetValue(name, out var typeName) && typeName.StartsWith("*"))
                        {
                            isPointer = true;
                            // Load the pointer value
                            resultPtr = _builder.BuildLoad2(resultType, resultPtr, "ptrload");
                            // Get the element type
                            var elementTypeName = typeName.Substring(1);
                            if (_userTypes.TryGetValue(elementTypeName, out var elementType))
                            {
                                resultType = elementType;
                                typeName = elementTypeName;
                            }
                        }

                        var resultTypeName = _namedValueTypes.TryGetValue(name, out var tn) ? tn : FindTypeName(resultType);
                        if (isPointer && resultTypeName != null && resultTypeName.StartsWith("*"))
                        {
                            resultTypeName = resultTypeName.Substring(1);
                        }

                        foreach (var suffix in suffixes)
                        {
                            // Handle dot operator: p.x (direct member access)
                            if (suffix.DOT() != null && suffix.ID() is { } memberId)
                            {
                                var memberName = memberId.GetText();
                                if (resultTypeName != null && _structFieldIndices.TryGetValue(resultTypeName, out var fields))
                                {
                                    if (fields.TryGetValue(memberName, out var fieldIndex))
                                    {
                                        var idx0 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
                                        var idx1 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, fieldIndex);
                                        resultPtr = _builder.BuildInBoundsGEP2(resultType, resultPtr, new[] { idx0, idx1 }, memberName);
                                        var fieldKey = $"{resultTypeName}.{memberName}";
                                        resultType = _structFieldTypes.TryGetValue(fieldKey, out var ft) ? ft : LLVMTypeRef.Int32;
                                        resultTypeName = FindTypeName(resultType);
                                    }
                                }
                            }
                            // Handle arrow operator: p->x (member access through pointer)
                            else if (suffix.MINUSRARROW() != null && suffix.ID() is { } arrowMemberId)
                            {
                                var memberName = arrowMemberId.GetText();
                                if (resultTypeName != null && _structFieldIndices.TryGetValue(resultTypeName, out var fields))
                                {
                                    if (fields.TryGetValue(memberName, out var fieldIndex))
                                    {
                                        var idx0 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
                                        var idx1 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, fieldIndex);
                                        resultPtr = _builder.BuildInBoundsGEP2(resultType, resultPtr, new[] { idx0, idx1 }, memberName);
                                        var fieldKey = $"{resultTypeName}.{memberName}";
                                        resultType = _structFieldTypes.TryGetValue(fieldKey, out var ft) ? ft : LLVMTypeRef.Int32;
                                        resultTypeName = FindTypeName(resultType);
                                    }
                                }
                            }
                        }

                        return _builder.BuildLoad2(resultType, resultPtr, "member");
                    }
                }
            }
            else if (hasCall)
            {
                // Handle function call
                var result = GeneratePrimaryExpr(postfixExpr.primary_expr());

                foreach (var suffix in suffixes)
                {
                    if (suffix.LPAREN() != null)
                    {
                        result = GenerateCall(result, suffix.call_args());
                    }
                }

                return result;
            }
        }

        // No suffix - generate normally
        return GeneratePrimaryExpr(postfixExpr.primary_expr());
    }

    private string? FindTypeName(LLVMTypeRef type)
    {
        foreach (var kvp in _userTypes)
        {
            if (kvp.Value.Handle == type.Handle)
            {
                return kvp.Key;
            }
        }
        return null;
    }

    private LLVMTypeRef GetPrimaryExprType(SolidLangParser.Primary_exprContext primaryExpr)
    {
        if (primaryExpr.ID() is { } id)
        {
            var name = id.GetText();
            if (_namedValues.TryGetValue(name, out var varInfo))
            {
                return varInfo.Type;
            }
        }

        if (primaryExpr.struct_literal() is { } structLit)
        {
            var structName = structLit.ID().GetText();
            if (_userTypes.TryGetValue(structName, out var structType))
            {
                return structType;
            }
        }

        return LLVMTypeRef.Int32;
    }

    private string? GetPrimaryExprTypeName(SolidLangParser.Primary_exprContext primaryExpr)
    {
        if (primaryExpr.ID() is { } id)
        {
            var name = id.GetText();
            if (_namedValues.TryGetValue(name, out var varInfo))
            {
                foreach (var kvp in _userTypes)
                {
                    if (kvp.Value.Equals(varInfo.Type))
                        return kvp.Key;
                }
            }
        }

        if (primaryExpr.struct_literal() is { } structLit)
        {
            return structLit.ID().GetText();
        }

        return null;
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

        var funcName = callee.Name;
        LLVMTypeRef funcType;

        if (!string.IsNullOrEmpty(funcName) && _functionTypes.TryGetValue(funcName, out var storedType))
        {
            funcType = storedType;
        }
        else
        {
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
                // Load the value for use in expressions
                return _builder.BuildLoad2(valueInfo.Type, valueInfo.Ptr, name);
            }

            if (_functions.TryGetValue(name, out _))
            {
                var func = _module.GetNamedFunction(name);
                if (func.Handle != IntPtr.Zero)
                    return func;
            }

            if (name.Contains("::"))
            {
                return GenerateEnumMemberAccess(name);
            }

            throw new InvalidOperationException($"Unknown variable or function: {name}");
        }

        if (primaryExpr.expr() is { } expr)
        {
            return GenerateExpression(expr);
        }

        if (primaryExpr.struct_literal() is { } structLit)
        {
            return GenerateStructLiteral(structLit);
        }

        if (primaryExpr.tuple_literal() is { } tupleLit)
        {
            return GenerateTupleLiteral(tupleLit);
        }

        throw new NotSupportedException("Unsupported primary expression");
    }

    private LLVMValueRef GenerateEnumMemberAccess(string qualifiedName)
    {
        var parts = qualifiedName.Split("::");
        if (parts.Length == 2)
        {
            var enumName = parts[0];
            var memberName = parts[1];

            if (_userTypes.TryGetValue(enumName, out var enumType))
            {
                // Would need proper enum value tracking
                return LLVMValueRef.CreateConstInt(enumType, 0);
            }
        }

        throw new InvalidOperationException($"Unknown enum member: {qualifiedName}");
    }

    private LLVMValueRef GenerateStructLiteral(SolidLangParser.Struct_literalContext structLit)
    {
        var structName = structLit.ID().GetText();

        if (!_userTypes.TryGetValue(structName, out var structType))
        {
            throw new InvalidOperationException($"Unknown struct type: {structName}");
        }

        var alloca = _builder.BuildAlloca(structType, structName);

        var fields = structLit.struct_literal_fields()?.struct_literal_field();
        if (fields != null && _structFieldIndices.TryGetValue(structName, out var fieldIndices))
        {
            foreach (var field in fields)
            {
                var fieldName = field.ID().GetText();
                var fieldValue = GenerateExpression(field.expr());

                if (fieldIndices.TryGetValue(fieldName, out var fieldIndex))
                {
                    var idx0 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
                    var idx1 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, fieldIndex);
                    var fieldPtr = _builder.BuildInBoundsGEP2(structType, alloca, new[] { idx0, idx1 }, fieldName);
                    _builder.BuildStore(fieldValue, fieldPtr);
                }
            }
        }

        return _builder.BuildLoad2(structType, alloca, structName);
    }

    private LLVMValueRef GenerateTupleLiteral(SolidLangParser.Tuple_literalContext tupleLit)
    {
        var elements = tupleLit.tuple_elements()?.expr();
        if (elements == null || elements.Length == 0)
        {
            return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
        }

        var elementTypes = elements.Select(e => LLVMTypeRef.Int32).ToArray();
        var tupleType = LLVMTypeRef.CreateStruct(elementTypes, false);

        var alloca = _builder.BuildAlloca(tupleType, "tuple");

        for (uint i = 0; i < elements.Length; i++)
        {
            var elemValue = GenerateExpression(elements[i]);
            var idx0 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
            var idx1 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, i);
            var elemPtr = _builder.BuildInBoundsGEP2(tupleType, alloca, new[] { idx0, idx1 }, $"elem{i}");
            _builder.BuildStore(elemValue, elemPtr);
        }

        return _builder.BuildLoad2(tupleType, alloca, "tuple");
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
            Types.StructType structType => _userTypes.TryGetValue(structType.Name, out var t) ? t : LLVMTypeRef.Int32,
            Types.EnumType enumType => _userTypes.TryGetValue(enumType.Name, out var et) ? et : LLVMTypeRef.Int32,
            Types.UnionType unionType => _userTypes.TryGetValue(unionType.Name, out var ut) ? ut : LLVMTypeRef.Int32,
            Types.TupleType tupleType => LLVMTypeRef.CreateStruct(
                tupleType.Elements.Select(e => GetLLVMType(e)).ToArray(), false),
            Types.PointerType pointerType => LLVMTypeRef.CreatePointer(GetLLVMType(pointerType.ElementType), 0),
            _ => throw new NotSupportedException($"Unknown type: {type.Name}")
        };
    }

    private LLVMTypeRef GetLLVMTypeFromContext(SolidLangParser.TypeContext typeContext)
    {
        // Handle pointer type: *T or !*T
        if (typeContext.pointer_type() is { } pointerType)
        {
            var elementType = GetLLVMTypeFromContext(pointerType.type());
            return LLVMTypeRef.CreatePointer(elementType, 0);
        }

        if (typeContext.named_type() is { } namedType)
        {
            var typeName = namedType.ID().GetText();

            if (_userTypes.TryGetValue(typeName, out var userType))
                return userType;

            return typeName switch
            {
                "i32" => LLVMTypeRef.Int32,
                "bool" => LLVMTypeRef.Int1,
                "void" => LLVMTypeRef.Void,
                _ => throw new NotSupportedException($"Unknown type: {typeName}")
            };
        }

        if (typeContext.tuple_type() is { } tupleType)
        {
            var types = tupleType.type();
            if (types == null || types.Length == 0)
                return LLVMTypeRef.Void;

            var elementTypes = types.Select(t => GetLLVMTypeFromContext(t)).ToArray();
            return LLVMTypeRef.CreateStruct(elementTypes, false);
        }

        return LLVMTypeRef.Int32;
    }

    private LLVMTypeRef GetLLVMType(string typeName)
    {
        if (_userTypes.TryGetValue(typeName, out var userType))
            return userType;

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
