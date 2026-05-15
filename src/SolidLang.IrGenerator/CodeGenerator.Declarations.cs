using LLVMSharp.Interop;
using SolidLang.SemanticAnalyzer;

namespace SolidLang.IrGenerator;

public sealed partial class CodeGenerator
{
    /// <summary>
    /// Generate the body of a previously declared function.
    /// </summary>
    private void GenerateFunction(BoundFunctionDecl func)
    {
        if (func.Body == null) return;

        var llvmFunc = _functions[func.Symbol];
        _currentFunc = llvmFunc;

        // Clear local state for this function
        _variables.Clear();
        _deferred.Clear();

        // Create entry block
        var entryBlock = llvmFunc.AppendBasicBlock("entry");
        _builder.PositionAtEnd(entryBlock);

        // Allocate space for parameters and register them
        for (int i = 0; i < func.Parameters.Count; i++)
        {
            var param = func.Parameters[i];
            var paramVal = llvmFunc.GetParam((uint)i);
            paramVal.Name = param.Symbol.Name;

            // Alloca for the parameter
            var alloca = _builder.BuildAlloca(GetLLVMType(param.DeclaredType), param.Symbol.Name + ".addr");
            _builder.BuildStore(paramVal, alloca);
            _variables[param.Symbol] = alloca;
        }

        // Generate body statements
        GenerateBlock(func.Body);

        // If the body didn't terminate with a return, emit deferred and add implicit return
        // (explicit returns handle their own deferred in GenerateReturn)
        if (func.ReturnType == null || func.ReturnType is PrimitiveType pt && pt.Name == "void")
        {
            foreach (var d in _deferred)
                GenerateStatement(d);
            _builder.BuildRetVoid();
        }
        else if (!func.Body.Statements.Any(s => s is BoundReturnStmt))
        {
            foreach (var d in _deferred)
                GenerateStatement(d);
            _builder.BuildRet(LLVMValueRef.CreateConstInt(GetLLVMType(func.ReturnType), 0, false));
        }
    }
}
