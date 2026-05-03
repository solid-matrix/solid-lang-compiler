using LLVMSharp.Interop;

namespace SolidLangCompiler;

public sealed class CodeGenerator : IDisposable
{
    private LLVMContextRef _ctx;

    public static string DefaultTriple => LLVMTargetRef.DefaultTriple;

    public CodeGenerator()
    {
        _ctx = LLVMContextRef.Create();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    public void GenerateObjective(SemanticTree semanticTree, string dstPath, string triple)
    {
        // hello world example


        using var module = _ctx.CreateModuleWithName("hello");

        module.Target = triple;

        ProcessModule(module, semanticTree);


        LLVM.InitializeAllTargetInfos();
        LLVM.InitializeAllTargets();
        LLVM.InitializeAllTargetMCs();
        LLVM.InitializeAllAsmPrinters();
        LLVM.InitializeAllAsmParsers();

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

        var mainFuncType = LLVMTypeRef.CreateFunction(_ctx.Int32Type, []);

        var mainFunc = module.AddFunction("main", mainFuncType);

        var entry = mainFunc.AppendBasicBlock("entry");

        builder.PositionAtEnd(entry);

        const string hello = "Hello,World!\n";
        var strConst = _ctx.GetConstString(hello, false);

        var arrType = LLVMTypeRef.CreateArray2(_ctx.Int8Type, (ulong)hello.Length + 1u);

        var globalStr = module.AddGlobal(arrType, ".str");
        globalStr.Initializer = strConst;
        globalStr.Linkage = LLVMLinkage.LLVMLinkerPrivateLinkage;


        LLVMValueRef[] indices =
        [
            LLVMValueRef.CreateConstInt(_ctx.Int32Type, 0, false),
            LLVMValueRef.CreateConstInt(_ctx.Int32Type, 0, false)
        ];

        var strPtr = builder.BuildInBoundsGEP2(arrType, globalStr, indices, "str");


        var putsFuncType = LLVMTypeRef.CreateFunction(_ctx.Int32Type, [LLVMTypeRef.CreatePointer(_ctx.Int8Type, 0)], false);

        var putsFunc = module.AddFunction("puts", putsFuncType);

        builder.BuildCall2(putsFuncType, putsFunc, [strPtr]);

        builder.BuildRet(LLVMValueRef.CreateConstInt(_ctx.Int32Type, 0, false));
    }

    public void GenerateIr(SemanticTree semanticTree, string dstPath, string triple)
    {
        using var module = _ctx.CreateModuleWithName("hello");

        module.Target = triple;

        ProcessModule(module, semanticTree);

        if (!LLVMTargetRef.TryGetTargetFromTriple(triple, out var target, out var error))
            throw new Exception($"failed to get target from triple {error}");

        module.PrintToFile(dstPath);
    }
}