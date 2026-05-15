using LLVMSharp.Interop;
using SolidLang.Parser;
using SolidLang.SemanticAnalyzer;

namespace SolidLang.IrGenerator;

public sealed partial class CodeGenerator
{
    private void GenerateStatement(BoundStatement stmt)
    {
        switch (stmt)
        {
            case BoundBlock block:
                GenerateBlock(block);
                break;
            case BoundVariableStmt varStmt:
                GenerateVariableStmt(varStmt);
                break;
            case BoundExprStmt exprStmt:
                GenerateExpression(exprStmt.Expression); // value discarded
                break;
            case BoundAssignStmt assignStmt:
                GenerateAssignment(assignStmt);
                break;
            case BoundReturnStmt returnStmt:
                GenerateReturn(returnStmt);
                break;
            case BoundIfStmt ifStmt:
                GenerateIf(ifStmt);
                break;
            case BoundWhileStmt whileStmt:
                GenerateWhile(whileStmt);
                break;
            case BoundForCStyleStmt forStmt:
                GenerateForCStyle(forStmt);
                break;
            case BoundBreakStmt:
                GenerateBreak();
                break;
            case BoundContinueStmt:
                GenerateContinue();
                break;
            case BoundSwitchStmt switchStmt:
                GenerateSwitch(switchStmt);
                break;
            case BoundDeferStmt deferStmt:
                // Register deferred statement at front for LIFO order
                _deferred.Insert(0, deferStmt.DeferredStatement);
                break;
        }
    }

    private void GenerateBlock(BoundBlock block)
    {
        foreach (var stmt in block.Statements)
            GenerateStatement(stmt);
    }

    private void GenerateVariableStmt(BoundVariableStmt stmt)
    {
        var decl = stmt.Declaration;
        var llvmTy = GetLLVMType(decl.DeclaredType);

        // Allocate stack space
        var alloca = _builder.BuildAlloca(llvmTy, decl.Symbol.Name);
        _variables[decl.Symbol] = alloca;

        // Store initializer if present
        if (decl.Initializer != null)
        {
            var initVal = GenerateExpression(decl.Initializer);
            _builder.BuildStore(initVal, alloca);
        }
    }

    /// <summary>
    /// Generate the init for <c>for target = expr; cond; upd { }</c>.
    /// The init expression is a BoundBinaryExpr wrapping the assignment.
    /// </summary>
    private void GenerateForInitAssign(BoundExpression initExpr)
    {
        if (initExpr is BoundBinaryExpr bin
            && (bin.Operator == SyntaxKind.EqualsToken || IsCompoundAssignment(bin.Operator)))
        {
            var rhs = GenerateExpression(bin.Right);
            var targetAddr = GetTargetAddress(bin.Left);
            if (bin.Operator != SyntaxKind.EqualsToken)
            {
                var targetTy = GetLLVMType(bin.Left.Type);
                var oldVal = _builder.BuildLoad2(targetTy, targetAddr, "load");
                rhs = bin.Operator switch
                {
                    SyntaxKind.PlusEqualsToken => _builder.BuildAdd(oldVal, rhs, "add"),
                    SyntaxKind.MinusEqualsToken => _builder.BuildSub(oldVal, rhs, "sub"),
                    SyntaxKind.StarEqualsToken => _builder.BuildMul(oldVal, rhs, "mul"),
                    SyntaxKind.SlashEqualsToken => _builder.BuildSDiv(oldVal, rhs, "div"),
                    _ => rhs,
                };
            }
            _builder.BuildStore(rhs, targetAddr);
        }
        else
        {
            GenerateExpression(initExpr);
        }
    }

    private void GenerateAssignment(BoundAssignStmt stmt)
    {
        var rhs = GenerateExpression(stmt.Value);
        var targetAddr = GetTargetAddress(stmt.Target);

        if (stmt.Operator != SyntaxKind.EqualsToken)
        {
            // Compound assignment: load, compute, store
            var targetTy = GetLLVMType(stmt.Target.Type);
            var oldVal = _builder.BuildLoad2(targetTy, targetAddr, "load");
            rhs = stmt.Operator switch
            {
                SyntaxKind.PlusEqualsToken => _builder.BuildAdd(oldVal, rhs, "add"),
                SyntaxKind.MinusEqualsToken => _builder.BuildSub(oldVal, rhs, "sub"),
                SyntaxKind.StarEqualsToken => _builder.BuildMul(oldVal, rhs, "mul"),
                SyntaxKind.SlashEqualsToken => _builder.BuildSDiv(oldVal, rhs, "div"),
                _ => rhs,
            };
        }

        _builder.BuildStore(rhs, targetAddr);
    }

    private void GenerateReturn(BoundReturnStmt stmt)
    {
        // Emit deferred statements before return
        // _deferred is stacked with newest at front (Insert(0)), so forward iteration = LIFO
        foreach (var d in _deferred)
            GenerateStatement(d);

        if (stmt.Expression != null)
        {
            var val = GenerateExpression(stmt.Expression);
            _builder.BuildRet(val);
        }
        else
        {
            _builder.BuildRetVoid();
        }
    }

    private void GenerateSwitch(BoundSwitchStmt stmt)
    {
        var func = _currentFunc;
        var mergeBlock = func.AppendBasicBlock("switch_merge");

        // Evaluate switch expression once, store in alloca
        var switchVal = GenerateExpression(stmt.Expression);
        var valTy = GetLLVMType(stmt.Expression.Type);
        var valAlloca = _builder.BuildAlloca(valTy, "switch.val");
        _builder.BuildStore(switchVal, valAlloca);

        // Build check+body chain forward. First, create all blocks.
        var checkBlocks = new List<LLVMBasicBlockRef>();
        var bodyBlocks = new List<LLVMBasicBlockRef>();
        LLVMBasicBlockRef? elseBodyBlock = null;

        foreach (var arm in stmt.Arms)
        {
            var bodyBlock = func.AppendBasicBlock($"switch_arm{bodyBlocks.Count}");
            bodyBlocks.Add(bodyBlock);

            if (arm.IsElse)
            {
                elseBodyBlock = bodyBlock;
            }
            else
            {
                var checkBlock = func.AppendBasicBlock($"switch_check{checkBlocks.Count}");
                checkBlocks.Add(checkBlock);
            }
        }

        // Branch from current position to first check (or else body, or merge)
        _builder.BuildBr(checkBlocks.Count > 0 ? checkBlocks[0]
            : elseBodyBlock ?? mergeBlock);

        // Generate check blocks (forward chain)
        for (int ci = 0; ci < checkBlocks.Count; ci++)
        {
            var arm = stmt.Arms[ci];  // checkBlocks[i] corresponds to arms[i] (non-else arms in order)
            _builder.PositionAtEnd(checkBlocks[ci]);

            var loadedVal = _builder.BuildLoad2(valTy, valAlloca, "switch.load");
            LLVMValueRef matches = default;

            foreach (var pattern in arm.Patterns)
            {
                var patternVal = GenerateSwitchPatternValue(pattern, valTy);
                if (patternVal.Handle == default) continue;
                var eq = _builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ,
                    loadedVal, patternVal, "switch.cmp");
                matches = matches.Handle != default
                    ? _builder.BuildOr(matches, eq, "switch.or")
                    : eq;
            }

            var fallthrough = ci + 1 < checkBlocks.Count ? checkBlocks[ci + 1]
                : elseBodyBlock ?? mergeBlock;
            if (matches.Handle != default)
                _builder.BuildCondBr(matches, bodyBlocks[ci], fallthrough);
            else
                _builder.BuildBr(fallthrough);
        }

        // Generate body blocks
        for (int i = 0; i < bodyBlocks.Count; i++)
        {
            _builder.PositionAtEnd(bodyBlocks[i]);
            GenerateStatement(stmt.Arms[i].Body);
            if (!IsTerminated(stmt.Arms[i].Body))
                _builder.BuildBr(mergeBlock);
        }

        _builder.PositionAtEnd(mergeBlock);
    }

    /// <summary>
    /// Generate the LLVM value for a switch pattern. Handles Literal patterns
    /// (including enum members that resolve to integer values).
    /// </summary>
    private LLVMValueRef GenerateSwitchPatternValue(BoundSwitchPattern pattern, LLVMTypeRef expectedTy)
    {
        switch (pattern.PatternKind)
        {
            case SwitchPatternKind.Literal:
                if (pattern.Literal != null)
                    return GenerateLiteral(pattern.Literal);
                break;

            case SwitchPatternKind.NamedTypeMember:
                // Enum member: extract the underlying integer value from the member's declaration
                if (pattern.MemberSymbol?.Declaration != null)
                {
                    var decl = pattern.MemberSymbol.Declaration;
                    if (decl is SolidLang.Parser.Nodes.Declarations.EnumFieldNode enumField
                        && enumField.Value != null)
                    {
                        var valExpr = enumField.Value;
                        if (valExpr is SolidLang.Parser.Nodes.Expressions.PrimaryExprNode primary
                            && primary.PrimaryKind == SolidLang.Parser.Nodes.Expressions.PrimaryExprKind.Literal
                            && primary.Literal is SolidLang.Parser.Nodes.Literals.IntegerLiteralNode intLit)
                        {
                            var val = Convert.ToInt64(intLit.Value);
                            return LLVMValueRef.CreateConstInt(expectedTy, (ulong)val, true);
                        }
                    }
                }
                // Fallback: try to evaluate based on member index
                if (pattern.NamedTypeSymbol?.TypeScope != null)
                {
                    var members = pattern.NamedTypeSymbol.TypeScope.Members.Values
                        .OfType<MemberSymbol>()
                        .OrderBy(m => m.Declaration!.Span.Start)
                        .ToList();
                    var idx = members.IndexOf(pattern.MemberSymbol!);
                    if (idx >= 0)
                        return LLVMValueRef.CreateConstInt(expectedTy, (ulong)idx, false);
                }
                break;

            case SwitchPatternKind.Identifier:
                // Identifier capture: always matches. Handled in the arm as a binding variable.
                // Return a sentinel that indicates "always matches".
                break;
        }

        return default;
    }

    private void GenerateIf(BoundIfStmt stmt)
    {
        var cond = GenerateExpression(stmt.Condition);

        var func = _currentFunc;
        var thenBlock = func.AppendBasicBlock("then");
        var elseBlock = stmt.ElseBody != null ? func.AppendBasicBlock("else") : default;
        var mergeBlock = func.AppendBasicBlock("if_merge");

        _builder.BuildCondBr(cond, thenBlock, stmt.ElseBody != null ? elseBlock : mergeBlock);

        // Then block
        _builder.PositionAtEnd(thenBlock);
        GenerateStatement(stmt.ThenBody);
        if (!IsTerminated(stmt.ThenBody))
            _builder.BuildBr(mergeBlock);

        // Else block
        if (stmt.ElseBody != null)
        {
            _builder.PositionAtEnd(elseBlock);
            GenerateStatement(stmt.ElseBody);
            if (!IsTerminated(stmt.ElseBody))
                _builder.BuildBr(mergeBlock);
        }

        _builder.PositionAtEnd(mergeBlock);
    }

    private void GenerateWhile(BoundWhileStmt stmt)
    {
        var func = _currentFunc;
        var condBlock = func.AppendBasicBlock("while_cond");
        var bodyBlock = func.AppendBasicBlock("while_body");
        var mergeBlock = func.AppendBasicBlock("while_merge");

        // Push loop context for break/continue
        _loopStack.Push((Merge: mergeBlock, ContinueTarget: condBlock));

        // Jump to condition
        _builder.BuildBr(condBlock);

        // Condition block
        _builder.PositionAtEnd(condBlock);
        if (stmt.Condition != null)
        {
            var cond = GenerateExpression(stmt.Condition);
            _builder.BuildCondBr(cond, bodyBlock, mergeBlock);
        }
        else
        {
            // Infinite loop
            _builder.BuildBr(bodyBlock);
        }

        // Body block
        _builder.PositionAtEnd(bodyBlock);
        GenerateStatement(stmt.Body);
        if (!IsTerminated(stmt.Body))
            _builder.BuildBr(condBlock);

        _builder.PositionAtEnd(mergeBlock);
        _loopStack.Pop();
    }

    private void GenerateForCStyle(BoundForCStyleStmt stmt)
    {
        var func = _currentFunc;
        var condBlock = func.AppendBasicBlock("for_cond");
        var bodyBlock = func.AppendBasicBlock("for_body");
        var updateBlock = func.AppendBasicBlock("for_update");
        var mergeBlock = func.AppendBasicBlock("for_merge");

        // Push loop context: continue jumps to update block (not cond)
        _loopStack.Push((Merge: mergeBlock, ContinueTarget: updateBlock));

        // Generate init
        if (stmt.InitVariable != null)
            GenerateVariableDecl(stmt.InitVariable);
        else if (stmt.InitExpression != null)
            GenerateForInitAssign(stmt.InitExpression);

        // Jump to condition
        _builder.BuildBr(condBlock);

        // Condition block
        _builder.PositionAtEnd(condBlock);
        if (stmt.Condition != null)
        {
            var cond = GenerateExpression(stmt.Condition);
            _builder.BuildCondBr(cond, bodyBlock, mergeBlock);
        }
        else
        {
            _builder.BuildBr(bodyBlock);
        }

        // Body block
        _builder.PositionAtEnd(bodyBlock);
        if (stmt.Body != null)
            GenerateStatement(stmt.Body);
        // Branch to update block (unless body already terminated)
        if (!IsTerminated(stmt.Body))
            _builder.BuildBr(updateBlock);

        // Update block
        _builder.PositionAtEnd(updateBlock);
        if (stmt.Update != null)
            GenerateUpdate(stmt.Update);
        _builder.BuildBr(condBlock);

        _builder.PositionAtEnd(mergeBlock);
        _loopStack.Pop();
    }

    private void GenerateBreak()
    {
        var (mergeBlock, _) = _loopStack.Peek();
        _builder.BuildBr(mergeBlock);
    }

    private void GenerateContinue()
    {
        var (_, continueTarget) = _loopStack.Peek();
        _builder.BuildBr(continueTarget);
    }

    private void GenerateVariableDecl(BoundVariableDecl decl)
    {
        var llvmTy = GetLLVMType(decl.DeclaredType);
        var alloca = _builder.BuildAlloca(llvmTy, decl.Symbol.Name);
        _variables[decl.Symbol] = alloca;
        if (decl.Initializer != null)
        {
            var initVal = GenerateExpression(decl.Initializer);
            _builder.BuildStore(initVal, alloca);
        }
    }

    private static bool IsCompoundAssignment(SyntaxKind kind) => kind is
        SyntaxKind.PlusEqualsToken or SyntaxKind.MinusEqualsToken or SyntaxKind.StarEqualsToken
        or SyntaxKind.SlashEqualsToken or SyntaxKind.PercentEqualsToken;

    private void GenerateUpdate(BoundExpression update)
    {
        if (update is BoundBinaryExpr bin && IsCompoundAssignment(bin.Operator))
        {
            // Load old value, compute new value, store back
            var targetAddr = GetTargetAddress(bin.Left);
            if (targetAddr.Handle == default) return;
            var targetTy = GetLLVMType(bin.Left.Type);
            var oldVal = _builder.BuildLoad2(targetTy, targetAddr, "update.load");
            var rhs = GenerateExpression(bin.Right);
            var newVal = bin.Operator switch
            {
                SyntaxKind.PlusEqualsToken => _builder.BuildAdd(oldVal, rhs, "update.add"),
                SyntaxKind.MinusEqualsToken => _builder.BuildSub(oldVal, rhs, "update.sub"),
                _ => rhs,
            };
            _builder.BuildStore(newVal, targetAddr);
        }
        else
        {
            GenerateExpression(update);
        }
    }

    /// <summary>
    /// Check if a block ends with a terminator instruction.
    /// </summary>
    private static bool IsTerminated(BoundStatement stmt)
    {
        if (stmt is BoundBlock block && block.Statements.Count > 0)
            return IsTerminated(block.Statements[^1]);
        if (stmt is BoundReturnStmt or BoundBreakStmt or BoundContinueStmt)
            return true;
        return false;
    }

    /// <summary>
    /// Get the address (alloca) for an assignment target.
    /// </summary>
    private LLVMValueRef GetTargetAddress(BoundExpression expr)
    {
        if (expr is BoundVarExpr varExpr && varExpr.Symbol is VariableSymbol vs)
            return _variables[vs];
        // Dereference: *ptr → the pointer value IS the address
        if (expr is BoundUnaryExpr un && un.Operator == SyntaxKind.StarToken)
            return GenerateExpression(un.Operand);
        return default;
    }
}
