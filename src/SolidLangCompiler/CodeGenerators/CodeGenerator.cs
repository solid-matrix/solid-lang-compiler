using LLVMSharp.Interop;
using SolidLangCompiler.SemanticAnalyzers;

namespace SolidLangCompiler.CodeGenerators;

public sealed class CodeGenerator : IDisposable
{
    private LLVMContextRef _ctx;

    public static string DefaultTriple => LLVMTargetRef.DefaultTriple;

    public CodeGenerator()
    {
        _ctx = LLVMContextRef.Create();

        // Initialize all targets
        LLVM.InitializeAllTargetInfos();
        LLVM.InitializeAllTargets();
        LLVM.InitializeAllTargetMCs();
        LLVM.InitializeAllAsmPrinters();
        LLVM.InitializeAllAsmParsers();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    public void GenerateObjective(SemanticTree semanticTree, string dstPath, string triple)
    {
        using var module = _ctx.CreateModuleWithName("solid_module");
        module.Target = triple;

        ProcessModule(module, semanticTree);

        if (!LLVMTargetRef.TryGetTargetFromTriple(triple, out var target, out var error))
            throw new Exception($"failed to get target from triple {error}");

        var machine = target.CreateTargetMachine(triple,
            "",
            "",
            LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
            LLVMRelocMode.LLVMRelocDefault,
            LLVMCodeModel.LLVMCodeModelDefault);

        if (!machine.TryEmitToFile(module, dstPath, LLVMCodeGenFileType.LLVMObjectFile, out error))
            throw new Exception($"failed to emit to file {error}");
    }

    public void ProcessModule(LLVMModuleRef module, SemanticTree semanticTree)
    {
        using var builder = _ctx.CreateBuilder();

        // Generate main function
        var mainFuncType = LLVMTypeRef.CreateFunction(_ctx.Int32Type, []);
        var mainFunc = module.AddFunction("main", mainFuncType);
        var entry = mainFunc.AppendBasicBlock("entry");
        builder.PositionAtEnd(entry);

        // Return 0
        builder.BuildRet(LLVMValueRef.CreateConstInt(_ctx.Int32Type, 0, false));
    }

    public void GenerateIr(SemanticTree semanticTree, string dstPath, string triple)
    {
        using var module = _ctx.CreateModuleWithName("solid_module");
        module.Target = triple;

        ProcessModule(module, semanticTree);

        if (!LLVMTargetRef.TryGetTargetFromTriple(triple, out var target, out var error))
            throw new Exception($"failed to get target from triple {error}");

        module.PrintToFile(dstPath);
    }
}
