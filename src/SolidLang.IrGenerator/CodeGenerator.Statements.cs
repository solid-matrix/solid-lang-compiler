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
            case BoundDeferStmt:
                // Defer is not implemented yet — skip
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

    private void GenerateAssignment(BoundAssignStmt stmt)
    {
        var value = GenerateExpression(stmt.Value);
        var targetAddr = GetTargetAddress(stmt.Target);
        _builder.BuildStore(value, targetAddr);
    }

    private void GenerateReturn(BoundReturnStmt stmt)
    {
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
    }

    /// <summary>
    /// Check if a block ends with a terminator instruction.
    /// </summary>
    private static bool IsTerminated(BoundStatement stmt)
    {
        if (stmt is BoundBlock block && block.Statements.Count > 0)
            return block.Statements[^1] is BoundReturnStmt or BoundBreakStmt or BoundContinueStmt;
        if (stmt is BoundReturnStmt)
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
        // For now, only support simple variable assignment targets
        return default;
    }
}
