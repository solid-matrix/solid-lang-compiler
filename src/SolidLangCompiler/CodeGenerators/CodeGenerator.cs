using LLVMSharp.Interop;
using SolidLangCompiler.SemanticTree;

namespace SolidLangCompiler.CodeGenerators;

/// <summary>
/// Generates LLVM IR from SemanticTree.
/// </summary>
public sealed class CodeGenerator : IDisposable
{
    private LLVMContextRef _ctx;
    private LLVMBuilderRef _builder;
    private LLVMModuleRef _currentModule;
    private readonly Dictionary<string, LLVMValueRef> _functions = new();
    private readonly Dictionary<int, LLVMValueRef> _locals = new();
    private readonly Dictionary<string, LLVMValueRef> _globals = new();

    // Stack of loop targets for break/continue
    private readonly Stack<LLVMBasicBlockRef> _breakTargets = new();
    private readonly Stack<LLVMBasicBlockRef> _continueTargets = new();

    // Stack of deferred expressions for defer statement
    private readonly Stack<SemaDefer> _defers = new();

    public static string DefaultTriple => LLVMTargetRef.DefaultTriple;

    public CodeGenerator()
    {
        _ctx = LLVMContextRef.Create();
        _builder = _ctx.CreateBuilder();

        LLVM.InitializeAllTargetInfos();
        LLVM.InitializeAllTargets();
        LLVM.InitializeAllTargetMCs();
        LLVM.InitializeAllAsmPrinters();
        LLVM.InitializeAllAsmParsers();
    }

    public void Dispose()
    {
        _builder.Dispose();
        _ctx.Dispose();
    }

    public void GenerateObjective(SemaProgram program, string dstPath, string triple)
    {
        using var module = GenerateModule(program, triple);

        if (!LLVMTargetRef.TryGetTargetFromTriple(triple, out var target, out var error))
            throw new Exception($"failed to get target from triple {error}");

        var machine = target.CreateTargetMachine(triple, "", "",
            LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
            LLVMRelocMode.LLVMRelocDefault,
            LLVMCodeModel.LLVMCodeModelDefault);

        if (!machine.TryEmitToFile(module, dstPath, LLVMCodeGenFileType.LLVMObjectFile, out error))
            throw new Exception($"failed to emit to file {error}");
    }

    public void GenerateIr(SemaProgram program, string dstPath, string triple)
    {
        using var module = GenerateModule(program, triple);
        module.PrintToFile(dstPath);
    }

    private LLVMModuleRef GenerateModule(SemaProgram program, string triple)
    {
        var module = _ctx.CreateModuleWithName("solid_module");
        _currentModule = module;
        module.Target = triple;

        foreach (var func in program.Functions)
            DeclareFunction(module, func);

        foreach (var global in program.Globals)
            DeclareGlobal(module, global);

        foreach (var func in program.Functions)
        {
            if (func.Body != null)
                GenerateFunctionBody(func);
        }

        return module;
    }

    private void DeclareFunction(LLVMModuleRef module, SemaFunction func)
    {
        var paramTypes = func.Parameters.Select(p => ConvertType(p.Type)).ToArray();
        var returnType = func.ReturnType != null ? ConvertType(func.ReturnType) : _ctx.VoidType;
        var funcType = LLVMTypeRef.CreateFunction(returnType, paramTypes);
        var funcValue = module.AddFunction(func.MangledName, funcType);

        for (int i = 0; i < func.Parameters.Count; i++)
        {
            var paramValue = funcValue.GetParam((uint)i);
            paramValue.Name = func.Parameters[i].Name;
        }

        _functions[func.MangledName] = funcValue;
    }

    private void DeclareGlobal(LLVMModuleRef module, SemaGlobal global)
    {
        var globalType = ConvertType(global.Type);
        var initialValue = global.Initializer != null
            ? GenerateConstantValue(global.Initializer)
            : LLVMValueRef.CreateConstNull(globalType);

        var globalValue = module.AddGlobal(globalType, global.MangledName);
        globalValue.Linkage = LLVMLinkage.LLVMInternalLinkage;
        globalValue.Initializer = initialValue;
        globalValue.IsGlobalConstant = global.IsConstant;

        _globals[global.Name] = globalValue;
    }

    private void GenerateFunctionBody(SemaFunction func)
    {
        if (!_functions.TryGetValue(func.MangledName, out var funcValue))
            return;

        _locals.Clear();
        _defers.Clear();  // Clear defer stack for new function

        var entry = funcValue.AppendBasicBlock("entry");
        _builder.PositionAtEnd(entry);

        for (int i = 0; i < func.Parameters.Count; i++)
        {
            var param = func.Parameters[i];
            var paramValue = funcValue.GetParam((uint)i);
            var alloca = _builder.BuildAlloca(ConvertType(param.Type), param.Name);
            _builder.BuildStore(paramValue, alloca);
            _locals[param.Index] = alloca;
        }

        GenerateBlock(func.Body!);

        var currentBlock = _builder.InsertBlock;
        if (currentBlock.Terminator == null)
        {
            // Execute deferred statements before implicit return
            while (_defers.Count > 0)
            {
                var defer = _defers.Pop();
                GenerateStatement(defer.DeferredStatement);
            }

            if (func.ReturnType != null)
                _builder.BuildRet(LLVMValueRef.CreateConstInt(ConvertType(func.ReturnType), 0, false));
            else
                _builder.BuildRetVoid();
        }
    }

    private void GenerateBlock(SemaBlock block)
    {
        foreach (var stmt in block.Statements)
            GenerateStatement(stmt);
    }

    private void GenerateStatement(SemaStatement stmt)
    {
        switch (stmt)
        {
            case SemaReturn ret:
                GenerateReturn(ret);
                break;
            case SemaLocalDecl local:
                GenerateLocalDecl(local);
                break;
            case SemaAssignment assign:
                GenerateAssignment(assign);
                break;
            case SemaExprStmt exprStmt:
                GenerateExpression(exprStmt.Expression);
                break;
            case SemaIf ifStmt:
                GenerateIf(ifStmt);
                break;
            case SemaWhile whileStmt:
                GenerateWhile(whileStmt);
                break;
            case SemaForInfinite forInfinite:
                GenerateForInfinite(forInfinite);
                break;
            case SemaForConditional forCond:
                GenerateForConditional(forCond);
                break;
            case SemaForCStyle forCStyle:
                GenerateForCStyle(forCStyle);
                break;
            case SemaForeach foreachStmt:
                GenerateForeach(foreachStmt);
                break;
            case SemaBreak:
                GenerateBreak();
                break;
            case SemaContinue:
                GenerateContinue();
                break;
            case SemaSwitch switchStmt:
                GenerateSwitch(switchStmt);
                break;
            case SemaBlock block:
                GenerateBlock(block);
                break;
            case SemaDefer defer:
                _defers.Push(defer);
                break;
        }
    }

    private void GenerateReturn(SemaReturn ret)
    {
        // Execute deferred statements in LIFO order before return
        while (_defers.Count > 0)
        {
            var defer = _defers.Pop();
            GenerateStatement(defer.DeferredStatement);
        }

        if (ret.Value != null)
        {
            var value = GenerateExpression(ret.Value);
            _builder.BuildRet(value);
        }
        else
        {
            _builder.BuildRetVoid();
        }
    }

    private void GenerateLocalDecl(SemaLocalDecl local)
    {
        var alloca = _builder.BuildAlloca(ConvertType(local.Type), local.Name);
        _locals[local.Index] = alloca;

        if (local.Initializer != null)
        {
            var value = GenerateExpression(local.Initializer);
            _builder.BuildStore(value, alloca);
        }
    }

    private void GenerateAssignment(SemaAssignment assign)
    {
        var value = GenerateExpression(assign.Value);

        switch (assign.Target)
        {
            case SemaLocalRef localRef:
                if (_locals.TryGetValue(localRef.Index, out var localAlloca))
                {
                    if (assign.Operator == SemaAssignOp.Assign)
                    {
                        _builder.BuildStore(value, localAlloca);
                    }
                    else
                    {
                        var current = _builder.BuildLoad2(ConvertType(localRef.Type), localAlloca, "tmp");
                        var result = GenerateCompoundAssignment(current, value, assign.Operator);
                        _builder.BuildStore(result, localAlloca);
                    }
                }
                break;
            case SemaParamRef paramRef:
                if (_locals.TryGetValue(paramRef.Index, out var paramAlloca))
                    _builder.BuildStore(value, paramAlloca);
                break;
            case SemaGlobalRef globalRef:
                if (_globals.TryGetValue(globalRef.Name, out var globalValue))
                    _builder.BuildStore(value, globalValue);
                break;
        }
    }

    private void GenerateIf(SemaIf ifStmt)
    {
        var condValue = GenerateExpression(ifStmt.Condition);
        if (condValue.TypeOf != _ctx.Int1Type)
            condValue = _builder.BuildIntCast(condValue, _ctx.Int1Type, "cond");

        var func = _builder.InsertBlock.Parent;
        var thenBlock = func.AppendBasicBlock("then");
        var elseBlock = ifStmt.ElseBranch != null ? func.AppendBasicBlock("else") : null;
        var mergeBlock = func.AppendBasicBlock("merge");

        if (elseBlock != null)
            _builder.BuildCondBr(condValue, thenBlock, elseBlock);
        else
            _builder.BuildCondBr(condValue, thenBlock, mergeBlock);

        _builder.PositionAtEnd(thenBlock);
        GenerateBlock(ifStmt.ThenBlock);
        if (_builder.InsertBlock.Terminator == null)
            _builder.BuildBr(mergeBlock);

        if (elseBlock != null && ifStmt.ElseBranch != null)
        {
            _builder.PositionAtEnd(elseBlock);
            GenerateStatement(ifStmt.ElseBranch);
            if (_builder.InsertBlock.Terminator == null)
                _builder.BuildBr(mergeBlock);
        }

        _builder.PositionAtEnd(mergeBlock);
    }

    private void GenerateWhile(SemaWhile whileStmt)
    {
        var func = _builder.InsertBlock.Parent;
        var condBlock = func.AppendBasicBlock("while.cond");
        var bodyBlock = func.AppendBasicBlock("while.body");
        var endBlock = func.AppendBasicBlock("while.end");

        // Push break/continue targets
        _breakTargets.Push(endBlock);
        _continueTargets.Push(condBlock);

        _builder.BuildBr(condBlock);

        _builder.PositionAtEnd(condBlock);
        var condValue = GenerateExpression(whileStmt.Condition);
        if (condValue.TypeOf != _ctx.Int1Type)
            condValue = _builder.BuildIntCast(condValue, _ctx.Int1Type, "cond");
        _builder.BuildCondBr(condValue, bodyBlock, endBlock);

        _builder.PositionAtEnd(bodyBlock);
        GenerateBlock(whileStmt.Body);
        if (_builder.InsertBlock.Terminator == null)
            _builder.BuildBr(condBlock);

        // Pop break/continue targets
        _breakTargets.Pop();
        _continueTargets.Pop();

        _builder.PositionAtEnd(endBlock);
    }

    private void GenerateForInfinite(SemaForInfinite forStmt)
    {
        var func = _builder.InsertBlock.Parent;
        var bodyBlock = func.AppendBasicBlock("for.body");
        var endBlock = func.AppendBasicBlock("for.end");

        // Push break/continue targets
        _breakTargets.Push(endBlock);
        _continueTargets.Push(bodyBlock);

        _builder.BuildBr(bodyBlock);

        _builder.PositionAtEnd(bodyBlock);
        GenerateBlock(forStmt.Body);
        if (_builder.InsertBlock.Terminator == null)
            _builder.BuildBr(bodyBlock);

        // Pop break/continue targets
        _breakTargets.Pop();
        _continueTargets.Pop();

        _builder.PositionAtEnd(endBlock);
    }

    private void GenerateForConditional(SemaForConditional forStmt)
    {
        var func = _builder.InsertBlock.Parent;
        var condBlock = func.AppendBasicBlock("for.cond");
        var bodyBlock = func.AppendBasicBlock("for.body");
        var endBlock = func.AppendBasicBlock("for.end");

        // Push break/continue targets
        _breakTargets.Push(endBlock);
        _continueTargets.Push(condBlock);

        _builder.BuildBr(condBlock);

        _builder.PositionAtEnd(condBlock);
        var condValue = GenerateExpression(forStmt.Condition);
        if (condValue.TypeOf != _ctx.Int1Type)
            condValue = _builder.BuildIntCast(condValue, _ctx.Int1Type, "cond");
        _builder.BuildCondBr(condValue, bodyBlock, endBlock);

        _builder.PositionAtEnd(bodyBlock);
        GenerateBlock(forStmt.Body);
        if (_builder.InsertBlock.Terminator == null)
            _builder.BuildBr(condBlock);

        // Pop break/continue targets
        _breakTargets.Pop();
        _continueTargets.Pop();

        _builder.PositionAtEnd(endBlock);
    }

    private void GenerateForCStyle(SemaForCStyle forStmt)
    {
        var func = _builder.InsertBlock.Parent;
        var initBlock = func.AppendBasicBlock("for.init");
        var condBlock = func.AppendBasicBlock("for.cond");
        var bodyBlock = func.AppendBasicBlock("for.body");
        var updateBlock = func.AppendBasicBlock("for.update");
        var endBlock = func.AppendBasicBlock("for.end");

        // Push break/continue targets
        _breakTargets.Push(endBlock);
        _continueTargets.Push(updateBlock);

        // Init
        _builder.BuildBr(initBlock);
        _builder.PositionAtEnd(initBlock);
        if (forStmt.Init != null)
            GenerateStatement(forStmt.Init);
        _builder.BuildBr(condBlock);

        // Condition
        _builder.PositionAtEnd(condBlock);
        if (forStmt.Condition != null)
        {
            var condValue = GenerateExpression(forStmt.Condition);
            if (condValue.TypeOf != _ctx.Int1Type)
                condValue = _builder.BuildIntCast(condValue, _ctx.Int1Type, "cond");
            _builder.BuildCondBr(condValue, bodyBlock, endBlock);
        }
        else
        {
            // No condition means infinite loop
            _builder.BuildBr(bodyBlock);
        }

        // Body
        _builder.PositionAtEnd(bodyBlock);
        GenerateBlock(forStmt.Body);
        if (_builder.InsertBlock.Terminator == null)
            _builder.BuildBr(updateBlock);

        // Update
        _builder.PositionAtEnd(updateBlock);
        if (forStmt.Update != null)
            GenerateExpression(forStmt.Update);
        _builder.BuildBr(condBlock);

        // Pop break/continue targets
        _breakTargets.Pop();
        _continueTargets.Pop();

        _builder.PositionAtEnd(endBlock);
    }

    private void GenerateForeach(SemaForeach forStmt)
    {
        // Simplified: treat as infinite loop for now
        // Real implementation would need iterator protocol
        var func = _builder.InsertBlock.Parent;
        var bodyBlock = func.AppendBasicBlock("foreach.body");
        var endBlock = func.AppendBasicBlock("foreach.end");

        // Push break/continue targets
        _breakTargets.Push(endBlock);
        _continueTargets.Push(bodyBlock);

        _builder.BuildBr(bodyBlock);

        _builder.PositionAtEnd(bodyBlock);
        GenerateBlock(forStmt.Body);
        if (_builder.InsertBlock.Terminator == null)
            _builder.BuildBr(bodyBlock);

        // Pop break/continue targets
        _breakTargets.Pop();
        _continueTargets.Pop();

        _builder.PositionAtEnd(endBlock);
    }

    private void GenerateBreak()
    {
        if (_breakTargets.Count > 0)
        {
            var target = _breakTargets.Peek();
            _builder.BuildBr(target);
        }
    }

    private void GenerateContinue()
    {
        if (_continueTargets.Count > 0)
        {
            var target = _continueTargets.Peek();
            _builder.BuildBr(target);
        }
    }

    private void GenerateSwitch(SemaSwitch switchStmt)
    {
        var func = _builder.InsertBlock.Parent;
        var exprValue = GenerateExpression(switchStmt.Expression);

        // Create basic blocks
        var defaultBlock = func.AppendBasicBlock("switch.default");
        var endBlock = func.AppendBasicBlock("switch.end");

        // Collect case values and blocks
        var caseValues = new List<ulong>();
        var caseBlocks = new List<LLVMBasicBlockRef>();
        var armBlocks = new List<(SemaSwitchArm arm, LLVMBasicBlockRef block)>();

        // Create a block for each arm
        foreach (var arm in switchStmt.Arms)
        {
            var armBlock = func.AppendBasicBlock("switch.arm");
            armBlocks.Add((arm, armBlock));

            if (arm.Pattern is SemaLiteralPattern litPattern && litPattern.Literal is SemaIntLiteral intLit)
            {
                caseValues.Add(intLit.Value);
                caseBlocks.Add(armBlock);
            }
            else if (arm.Pattern == null)
            {
                // else/default branch
                defaultBlock = armBlock;
            }
        }

        // Build the switch instruction
        var switchInst = _builder.BuildSwitch(exprValue, defaultBlock, (uint)caseValues.Count);
        for (int i = 0; i < caseValues.Count; i++)
        {
            var caseValue = LLVMValueRef.CreateConstInt(exprValue.TypeOf, caseValues[i], false);
            switchInst.AddCase(caseValue, caseBlocks[i]);
        }

        // Generate code for each arm
        foreach (var (arm, armBlock) in armBlocks)
        {
            _builder.PositionAtEnd(armBlock);
            GenerateStatement(arm.Statement);
            if (_builder.InsertBlock.Terminator == null)
                _builder.BuildBr(endBlock);
        }

        // Ensure default block has terminator
        _builder.PositionAtEnd(defaultBlock);
        if (defaultBlock.Terminator == null)
            _builder.BuildBr(endBlock);

        _builder.PositionAtEnd(endBlock);
    }

    private LLVMValueRef GenerateExpression(SemaExpression expr)
    {
        return expr switch
        {
            SemaIntLiteral intLit => GenerateIntLiteral(intLit),
            SemaFloatLiteral floatLit => GenerateFloatLiteral(floatLit),
            SemaBoolLiteral boolLit => GenerateBoolLiteral(boolLit),
            SemaStringLiteral strLit => GenerateStringLiteral(strLit),
            SemaNullLiteral => LLVMValueRef.CreateConstNull(_ctx.Int8Type),
            SemaLocalRef localRef => GenerateLocalRef(localRef),
            SemaParamRef paramRef => GenerateParamRef(paramRef),
            SemaGlobalRef globalRef => GenerateGlobalRef(globalRef),
            SemaFuncRef funcRef => GenerateFuncRef(funcRef),
            SemaBinaryExpr binary => GenerateBinary(binary),
            SemaUnaryExpr unary => GenerateUnary(unary),
            SemaCallExpr call => GenerateCall(call),
            SemaArrayLiteral arrLit => GenerateArrayLiteral(arrLit),
            SemaStructLiteral structLit => GenerateStructLiteral(structLit),
            SemaFieldAccessExpr fieldAccess => GenerateFieldAccess(fieldAccess),
            SemaIndexExpr indexExpr => GenerateIndexExpr(indexExpr),
            _ => LLVMValueRef.CreateConstInt(_ctx.Int32Type, 0, false)
        };
    }

    private LLVMValueRef GenerateIntLiteral(SemaIntLiteral lit)
    {
        var type = ConvertType(lit.Type);
        return LLVMValueRef.CreateConstInt(type, lit.Value, lit.Type is SemaIntType { IsSigned: true });
    }

    private LLVMValueRef GenerateFloatLiteral(SemaFloatLiteral lit)
    {
        var type = ConvertType(lit.Type);
        return LLVMValueRef.CreateConstReal(type, lit.Value);
    }

    private LLVMValueRef GenerateBoolLiteral(SemaBoolLiteral lit)
    {
        return LLVMValueRef.CreateConstInt(_ctx.Int1Type, lit.Value ? 1UL : 0UL, false);
    }

    private LLVMValueRef GenerateStringLiteral(SemaStringLiteral lit)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(lit.Value + "\0");
        var arrayType = LLVMTypeRef.CreateArray(_ctx.Int8Type, (uint)bytes.Length);
        var stringConst = _currentModule.AddGlobal(arrayType, ".str");
        stringConst.Linkage = LLVMLinkage.LLVMPrivateLinkage;
        stringConst.IsGlobalConstant = true;

        var constArray = LLVMValueRef.CreateConstArray(_ctx.Int8Type,
            bytes.Select(b => LLVMValueRef.CreateConstInt(_ctx.Int8Type, b, false)).ToArray());
        stringConst.Initializer = constArray;

        var zero = LLVMValueRef.CreateConstInt(_ctx.Int32Type, 0, false);
        var indices = new[] { zero, zero };
        return _builder.BuildInBoundsGEP2(arrayType, stringConst, indices, "str");
    }

    private LLVMValueRef GenerateLocalRef(SemaLocalRef local)
    {
        if (_locals.TryGetValue(local.Index, out var alloca))
            return _builder.BuildLoad2(ConvertType(local.Type), alloca, local.Name);
        return LLVMValueRef.CreateConstInt(ConvertType(local.Type), 0, false);
    }

    private LLVMValueRef GenerateParamRef(SemaParamRef param)
    {
        if (_locals.TryGetValue(param.Index, out var alloca))
            return _builder.BuildLoad2(ConvertType(param.Type), alloca, param.Name);
        return LLVMValueRef.CreateConstInt(ConvertType(param.Type), 0, false);
    }

    private LLVMValueRef GenerateGlobalRef(SemaGlobalRef global)
    {
        if (_globals.TryGetValue(global.Name, out var globalValue))
            return _builder.BuildLoad2(ConvertType(global.Type), globalValue, global.Name);
        return LLVMValueRef.CreateConstInt(ConvertType(global.Type), 0, false);
    }

    private LLVMValueRef GenerateFuncRef(SemaFuncRef func)
    {
        if (_functions.TryGetValue(func.MangledName, out var funcValue))
            return funcValue;
        throw new InvalidOperationException($"Function not found: {func.Name}");
    }

    private LLVMValueRef GenerateBinary(SemaBinaryExpr binary)
    {
        var left = GenerateExpression(binary.Left);
        var right = GenerateExpression(binary.Right);

        return binary.Operator switch
        {
            SemaBinaryOp.Add => GenerateAdd(left, right, binary.Type),
            SemaBinaryOp.Subtract => GenerateSub(left, right, binary.Type),
            SemaBinaryOp.Multiply => GenerateMul(left, right, binary.Type),
            SemaBinaryOp.Divide => GenerateDiv(left, right, binary.Type),
            SemaBinaryOp.Modulo => GenerateMod(left, right, binary.Type),
            SemaBinaryOp.Equal => GenerateCmp(LLVMIntPredicate.LLVMIntEQ, left, right, binary.Left.Type),
            SemaBinaryOp.NotEqual => GenerateCmp(LLVMIntPredicate.LLVMIntNE, left, right, binary.Left.Type),
            SemaBinaryOp.Less => GenerateCmp(LLVMIntPredicate.LLVMIntSLT, left, right, binary.Left.Type),
            SemaBinaryOp.Greater => GenerateCmp(LLVMIntPredicate.LLVMIntSGT, left, right, binary.Left.Type),
            SemaBinaryOp.LessEqual => GenerateCmp(LLVMIntPredicate.LLVMIntSLE, left, right, binary.Left.Type),
            SemaBinaryOp.GreaterEqual => GenerateCmp(LLVMIntPredicate.LLVMIntSGE, left, right, binary.Left.Type),
            SemaBinaryOp.BitwiseAnd => _builder.BuildAnd(left, right, "and"),
            SemaBinaryOp.BitwiseOr => _builder.BuildOr(left, right, "or"),
            SemaBinaryOp.BitwiseXor => _builder.BuildXor(left, right, "xor"),
            SemaBinaryOp.ShiftLeft => _builder.BuildShl(left, right, "shl"),
            SemaBinaryOp.ShiftRight => _builder.BuildAShr(left, right, "shr"),
            _ => left
        };
    }

    private LLVMValueRef GenerateAdd(LLVMValueRef left, LLVMValueRef right, SemaType type)
    {
        if (type is SemaFloatType)
            return _builder.BuildFAdd(left, right, "add");
        return _builder.BuildAdd(left, right, "add");
    }

    private LLVMValueRef GenerateSub(LLVMValueRef left, LLVMValueRef right, SemaType type)
    {
        if (type is SemaFloatType)
            return _builder.BuildFSub(left, right, "sub");
        return _builder.BuildSub(left, right, "sub");
    }

    private LLVMValueRef GenerateMul(LLVMValueRef left, LLVMValueRef right, SemaType type)
    {
        if (type is SemaFloatType)
            return _builder.BuildFMul(left, right, "mul");
        return _builder.BuildMul(left, right, "mul");
    }

    private LLVMValueRef GenerateDiv(LLVMValueRef left, LLVMValueRef right, SemaType type)
    {
        if (type is SemaFloatType)
            return _builder.BuildFDiv(left, right, "div");
        if (type is SemaIntType { IsSigned: true })
            return _builder.BuildSDiv(left, right, "div");
        return _builder.BuildUDiv(left, right, "div");
    }

    private LLVMValueRef GenerateMod(LLVMValueRef left, LLVMValueRef right, SemaType type)
    {
        if (type is SemaFloatType)
            return _builder.BuildFRem(left, right, "mod");
        if (type is SemaIntType { IsSigned: true })
            return _builder.BuildSRem(left, right, "mod");
        return _builder.BuildURem(left, right, "mod");
    }

    private LLVMValueRef GenerateCmp(LLVMIntPredicate pred, LLVMValueRef left, LLVMValueRef right, SemaType type)
    {
        if (type is SemaFloatType)
            return _builder.BuildFCmp(ConvertToFloatPred(pred), left, right, "cmp");
        return _builder.BuildICmp(pred, left, right, "cmp");
    }

    private LLVMRealPredicate ConvertToFloatPred(LLVMIntPredicate pred) => pred switch
    {
        LLVMIntPredicate.LLVMIntEQ => LLVMRealPredicate.LLVMRealOEQ,
        LLVMIntPredicate.LLVMIntNE => LLVMRealPredicate.LLVMRealONE,
        LLVMIntPredicate.LLVMIntSLT => LLVMRealPredicate.LLVMRealOLT,
        LLVMIntPredicate.LLVMIntSGT => LLVMRealPredicate.LLVMRealOGT,
        LLVMIntPredicate.LLVMIntSLE => LLVMRealPredicate.LLVMRealOLE,
        LLVMIntPredicate.LLVMIntSGE => LLVMRealPredicate.LLVMRealOGE,
        _ => LLVMRealPredicate.LLVMRealOEQ
    };

    private LLVMValueRef GenerateUnary(SemaUnaryExpr unary)
    {
        var operand = GenerateExpression(unary.Operand);

        return unary.Operator switch
        {
            SemaUnaryOp.Negate => unary.Type is SemaFloatType
                ? _builder.BuildFNeg(operand, "neg")
                : _builder.BuildNeg(operand, "neg"),
            SemaUnaryOp.LogicalNot => _builder.BuildNot(_builder.BuildIntCast(operand, _ctx.Int1Type, "bool"), "not"),
            SemaUnaryOp.BitwiseNot => _builder.BuildNot(operand, "not"),
            SemaUnaryOp.Dereference when unary.Type is SemaPointerType ptrType =>
                _builder.BuildLoad2(ConvertType(ptrType.TargetType), operand, "deref"),
            SemaUnaryOp.Dereference when unary.Type is SemaRefType refType =>
                _builder.BuildLoad2(ConvertType(refType.TargetType), operand, "deref"),
            SemaUnaryOp.Ref or SemaUnaryOp.MutRef => GenerateRef(unary),
            _ => operand
        };
    }

    private LLVMValueRef GenerateRef(SemaUnaryExpr unary)
    {
        // Get pointer to the operand
        switch (unary.Operand)
        {
            case SemaLocalRef localRef:
                if (_locals.TryGetValue(localRef.Index, out var alloca))
                    return alloca;
                break;
            case SemaParamRef paramRef:
                if (_locals.TryGetValue(paramRef.Index, out var paramAlloca))
                    return paramAlloca;
                break;
            case SemaGlobalRef globalRef:
                if (_globals.TryGetValue(globalRef.Name, out var globalValue))
                    return globalValue;
                break;
            case SemaFieldAccessExpr fieldAccess:
                // Generate address of field
                return GenerateFieldAddress(fieldAccess);
            case SemaIndexExpr indexExpr:
                // Generate address of array element
                return GenerateIndexAddress(indexExpr);
        }

        // Fallback: allocate temporary and return pointer
        var operandValue = GenerateExpression(unary.Operand);
        var tmpAlloca = _builder.BuildAlloca(operandValue.TypeOf, "tmp.ref");
        _builder.BuildStore(operandValue, tmpAlloca);
        return tmpAlloca;
    }

    private LLVMValueRef GenerateFieldAddress(SemaFieldAccessExpr fieldAccess)
    {
        var targetType = fieldAccess.Target.Type;
        var llvmTargetType = ConvertType(targetType);

        // Get pointer to the target
        LLVMValueRef targetPtr;
        switch (fieldAccess.Target)
        {
            case SemaLocalRef localRef:
                targetPtr = _locals[localRef.Index];
                break;
            case SemaParamRef paramRef:
                targetPtr = _locals[paramRef.Index];
                break;
            case SemaGlobalRef globalRef:
                targetPtr = _globals[globalRef.Name];
                break;
            default:
                var targetValue = GenerateExpression(fieldAccess.Target);
                var alloca = _builder.BuildAlloca(llvmTargetType, "tmp.struct");
                _builder.BuildStore(targetValue, alloca);
                targetPtr = alloca;
                break;
        }

        // Use GEP to get pointer to the field
        var indices = new[] {
            LLVMValueRef.CreateConstInt(_ctx.Int64Type, 0, false),
            LLVMValueRef.CreateConstInt(_ctx.Int64Type, (ulong)fieldAccess.FieldIndex, false)
        };
        return _builder.BuildInBoundsGEP2(llvmTargetType, targetPtr, indices, "field.ptr");
    }

    private LLVMValueRef GenerateIndexAddress(SemaIndexExpr indexExpr)
    {
        var targetType = indexExpr.Target.Type;
        if (targetType is not SemaArrayType arrayType)
            return LLVMValueRef.CreateConstInt(_ctx.Int32Type, 0, false);

        var llvmArrayType = ConvertType(targetType);

        // Get pointer to the target array
        LLVMValueRef targetPtr;
        switch (indexExpr.Target)
        {
            case SemaLocalRef localRef:
                targetPtr = _locals[localRef.Index];
                break;
            case SemaParamRef paramRef:
                targetPtr = _locals[paramRef.Index];
                break;
            case SemaGlobalRef globalRef:
                targetPtr = _globals[globalRef.Name];
                break;
            default:
                var targetValue = GenerateExpression(indexExpr.Target);
                var alloca = _builder.BuildAlloca(llvmArrayType, "tmp.arr");
                _builder.BuildStore(targetValue, alloca);
                targetPtr = alloca;
                break;
        }

        // Generate the index value
        var indexValue = GenerateExpression(indexExpr.Index);

        // Use GEP to get pointer to the element
        var indices = new[] {
            LLVMValueRef.CreateConstInt(_ctx.Int64Type, 0, false),
            indexValue
        };
        return _builder.BuildInBoundsGEP2(llvmArrayType, targetPtr, indices, "elem.ptr");
    }

    private LLVMValueRef GenerateCall(SemaCallExpr call)
    {
        LLVMValueRef callee;

        if (call.Target is SemaFuncRef funcRef)
            callee = _functions[funcRef.MangledName];
        else
            callee = GenerateExpression(call.Target);

        var args = call.Arguments.Select(GenerateExpression).ToArray();
        return _builder.BuildCall2(callee.TypeOf, callee, args, "call");
    }

    private LLVMValueRef GenerateArrayLiteral(SemaArrayLiteral arrLit)
    {
        var arrayType = arrLit.Type as SemaArrayType;
        if (arrayType == null)
            return LLVMValueRef.CreateConstNull(_ctx.Int32Type);

        var llvmArrayType = LLVMTypeRef.CreateArray(ConvertType(arrayType.ElementType), (uint)arrayType.Size);

        // Allocate array on stack
        var alloca = _builder.BuildAlloca(llvmArrayType, "arr");

        // Initialize each element
        for (int i = 0; i < arrLit.Elements.Count; i++)
        {
            var elemValue = GenerateExpression(arrLit.Elements[i]);
            var index = LLVMValueRef.CreateConstInt(_ctx.Int64Type, (ulong)i, false);
            var indices = new[] { LLVMValueRef.CreateConstInt(_ctx.Int64Type, 0, false), index };
            var elemPtr = _builder.BuildInBoundsGEP2(llvmArrayType, alloca, indices, "elem.ptr");
            _builder.BuildStore(elemValue, elemPtr);
        }

        return _builder.BuildLoad2(llvmArrayType, alloca, "arr.val");
    }

    private LLVMValueRef GenerateStructLiteral(SemaStructLiteral structLit)
    {
        var structType = structLit.Type as SemaStructType;
        if (structType == null)
            return LLVMValueRef.CreateConstNull(_ctx.Int32Type);

        var llvmStructType = ConvertType(structType);

        // Allocate struct on stack
        var alloca = _builder.BuildAlloca(llvmStructType, "struct");

        // Initialize each field
        foreach (var field in structLit.Fields)
        {
            var fieldIndex = structType.Fields.ToList().FindIndex(f => f.Name == field.Name);
            if (fieldIndex < 0)
                continue;

            var fieldValue = GenerateExpression(field.Value);
            var indices = new[] {
                LLVMValueRef.CreateConstInt(_ctx.Int64Type, 0, false),
                LLVMValueRef.CreateConstInt(_ctx.Int64Type, (ulong)fieldIndex, false)
            };
            var fieldPtr = _builder.BuildInBoundsGEP2(llvmStructType, alloca, indices, "field.ptr");
            _builder.BuildStore(fieldValue, fieldPtr);
        }

        return _builder.BuildLoad2(llvmStructType, alloca, "struct.val");
    }

    private LLVMValueRef GenerateFieldAccess(SemaFieldAccessExpr fieldAccess)
    {
        // For field access, we need to:
        // 1. Get the target value (which might be a local variable holding a struct)
        // 2. Use GEP to access the field

        var targetType = fieldAccess.Target.Type;

        // Handle auto-dereference for references
        if (targetType is SemaRefType refType)
        {
            targetType = refType.TargetType;
        }

        var llvmTargetType = ConvertType(targetType);

        // Get pointer to the target
        LLVMValueRef targetPtr;
        switch (fieldAccess.Target)
        {
            case SemaLocalRef localRef:
                targetPtr = _locals[localRef.Index];
                // If the local is a reference, load it first
                if (fieldAccess.Target.Type is SemaRefType)
                {
                    var refPtrType = LLVMTypeRef.CreatePointer(llvmTargetType, 0);
                    targetPtr = _builder.BuildLoad2(refPtrType, targetPtr, "ref.load");
                }
                break;
            case SemaParamRef paramRef:
                targetPtr = _locals[paramRef.Index];
                // If the param is a reference, load it first
                if (fieldAccess.Target.Type is SemaRefType)
                {
                    var refPtrType = LLVMTypeRef.CreatePointer(llvmTargetType, 0);
                    targetPtr = _builder.BuildLoad2(refPtrType, targetPtr, "ref.load");
                }
                break;
            default:
                // For complex expressions, we need to allocate and store the result
                var targetValue = GenerateExpression(fieldAccess.Target);
                var alloca = _builder.BuildAlloca(llvmTargetType, "tmp.struct");
                _builder.BuildStore(targetValue, alloca);
                targetPtr = alloca;
                break;
        }

        // Use GEP to get pointer to the field
        var indices = new[] {
            LLVMValueRef.CreateConstInt(_ctx.Int64Type, 0, false),
            LLVMValueRef.CreateConstInt(_ctx.Int64Type, (ulong)fieldAccess.FieldIndex, false)
        };
        var fieldPtr = _builder.BuildInBoundsGEP2(llvmTargetType, targetPtr, indices, "field.ptr");

        // Load the field value
        var fieldType = ConvertType(fieldAccess.Type);
        return _builder.BuildLoad2(fieldType, fieldPtr, fieldAccess.FieldName);
    }

    private LLVMValueRef GenerateIndexExpr(SemaIndexExpr indexExpr)
    {
        // For array indexing, we need to:
        // 1. Get the target array value
        // 2. Use GEP to access the element at the given index

        var targetType = indexExpr.Target.Type;

        // Handle auto-dereference for references
        if (targetType is SemaRefType refType)
        {
            targetType = refType.TargetType;
        }

        if (targetType is not SemaArrayType arrayType)
            return LLVMValueRef.CreateConstInt(_ctx.Int32Type, 0, false);

        var llvmArrayType = ConvertType(targetType);

        // Get pointer to the target array
        LLVMValueRef targetPtr;
        switch (indexExpr.Target)
        {
            case SemaLocalRef localRef:
                targetPtr = _locals[localRef.Index];
                // If the local is a reference, load it first
                if (indexExpr.Target.Type is SemaRefType)
                {
                    var refPtrType = LLVMTypeRef.CreatePointer(llvmArrayType, 0);
                    targetPtr = _builder.BuildLoad2(refPtrType, targetPtr, "ref.load");
                }
                break;
            case SemaParamRef paramRef:
                targetPtr = _locals[paramRef.Index];
                // If the param is a reference, load it first
                if (indexExpr.Target.Type is SemaRefType)
                {
                    var refPtrType = LLVMTypeRef.CreatePointer(llvmArrayType, 0);
                    targetPtr = _builder.BuildLoad2(refPtrType, targetPtr, "ref.load");
                }
                break;
            default:
                // For complex expressions, allocate and store the result
                var targetValue = GenerateExpression(indexExpr.Target);
                var alloca = _builder.BuildAlloca(llvmArrayType, "tmp.arr");
                _builder.BuildStore(targetValue, alloca);
                targetPtr = alloca;
                break;
        }

        // Generate the index value
        var indexValue = GenerateExpression(indexExpr.Index);

        // Use GEP to get pointer to the element
        var indices = new[] {
            LLVMValueRef.CreateConstInt(_ctx.Int64Type, 0, false),
            indexValue
        };
        var elemPtr = _builder.BuildInBoundsGEP2(llvmArrayType, targetPtr, indices, "elem.ptr");

        // Load the element value
        var elemType = ConvertType(arrayType.ElementType);
        return _builder.BuildLoad2(elemType, elemPtr, "elem");
    }

    private LLVMValueRef GenerateCompoundAssignment(LLVMValueRef current, LLVMValueRef value, SemaAssignOp op)
    {
        return op switch
        {
            SemaAssignOp.AddAssign => _builder.BuildAdd(current, value, "add"),
            SemaAssignOp.SubtractAssign => _builder.BuildSub(current, value, "sub"),
            SemaAssignOp.MultiplyAssign => _builder.BuildMul(current, value, "mul"),
            SemaAssignOp.DivideAssign => _builder.BuildSDiv(current, value, "div"),
            SemaAssignOp.ModuloAssign => _builder.BuildSRem(current, value, "mod"),
            SemaAssignOp.AndAssign => _builder.BuildAnd(current, value, "and"),
            SemaAssignOp.OrAssign => _builder.BuildOr(current, value, "or"),
            SemaAssignOp.XorAssign => _builder.BuildXor(current, value, "xor"),
            SemaAssignOp.ShlAssign => _builder.BuildShl(current, value, "shl"),
            SemaAssignOp.ShrAssign => _builder.BuildAShr(current, value, "shr"),
            _ => value
        };
    }

    private LLVMValueRef GenerateConstantValue(SemaExpression expr)
    {
        return expr switch
        {
            SemaIntLiteral intLit => LLVMValueRef.CreateConstInt(ConvertType(intLit.Type), intLit.Value, true),
            SemaFloatLiteral floatLit => LLVMValueRef.CreateConstReal(ConvertType(floatLit.Type), floatLit.Value),
            SemaBoolLiteral boolLit => LLVMValueRef.CreateConstInt(_ctx.Int1Type, boolLit.Value ? 1UL : 0UL, false),
            _ => LLVMValueRef.CreateConstNull(ConvertType(expr.Type))
        };
    }

    private LLVMTypeRef ConvertType(SemaType type)
    {
        return type switch
        {
            SemaIntType intType => intType.Kind switch
            {
                SemaIntegerKind.I8 => _ctx.Int8Type,
                SemaIntegerKind.I16 => _ctx.Int16Type,
                SemaIntegerKind.I32 => _ctx.Int32Type,
                SemaIntegerKind.I64 => _ctx.Int64Type,
                SemaIntegerKind.ISize => _ctx.Int64Type,
                SemaIntegerKind.U8 => _ctx.Int8Type,
                SemaIntegerKind.U16 => _ctx.Int16Type,
                SemaIntegerKind.U32 => _ctx.Int32Type,
                SemaIntegerKind.U64 => _ctx.Int64Type,
                SemaIntegerKind.USize => _ctx.Int64Type,
                _ => _ctx.Int32Type
            },
            SemaFloatType floatType => floatType.Kind == SemaFloatKind.F32 ? _ctx.FloatType : _ctx.DoubleType,
            SemaBoolType => _ctx.Int1Type,
            SemaVoidType => _ctx.VoidType,
            SemaPointerType ptrType => LLVMTypeRef.CreatePointer(ConvertType(ptrType.TargetType), 0),
            SemaRefType refType => LLVMTypeRef.CreatePointer(ConvertType(refType.TargetType), 0),
            SemaArrayType arrType => LLVMTypeRef.CreateArray(ConvertType(arrType.ElementType), (uint)arrType.Size),
            SemaFuncType funcType => LLVMTypeRef.CreatePointer(LLVMTypeRef.CreateFunction(
                funcType.ReturnType != null ? ConvertType(funcType.ReturnType) : _ctx.VoidType,
                funcType.ParameterTypes.Select(ConvertType).ToArray()
            ), 0),
            SemaStructType structType => LLVMTypeRef.CreateStruct(
                structType.Fields.Select(f => ConvertType(f.Type)).ToArray(),
                false
            ),
            SemaNamedType namedType => namedType.UnderlyingType != null
                ? ConvertType(namedType.UnderlyingType)
                : _ctx.Int8Type,
            SemaTupleType tupleType => LLVMTypeRef.CreateStruct(
                tupleType.Elements.Select(ConvertType).ToArray(),
                false
            ),
            _ => _ctx.Int32Type
        };
    }
}
