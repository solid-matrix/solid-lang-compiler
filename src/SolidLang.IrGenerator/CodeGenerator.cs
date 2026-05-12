using LLVMSharp.Interop;
using SolidLang.Parser;
using SolidLang.SemanticAnalyzer;

namespace SolidLang.IrGenerator;

/// <summary>
/// Top-level driver for LLVM IR code generation from a semantic model.
/// Uses LLVMSharp.Interop types for all LLVM operations.
/// </summary>
public sealed partial class CodeGenerator
{
    private LLVMContextRef _context;
    private LLVMModuleRef _module;
    private LLVMBuilderRef _builder;
    private BoundProgram _program = null!;

    // Maps symbols to their LLVM values
    private readonly Dictionary<VariableSymbol, LLVMValueRef> _variables = new();
    private readonly Dictionary<FunctionSymbol, LLVMValueRef> _functions = new();
    private readonly Dictionary<FunctionSymbol, LLVMTypeRef> _functionTypes = new();
    private readonly Dictionary<VariableSymbol, LLVMValueRef> _globals = new();
    private readonly Dictionary<TypeSymbol, LLVMTypeRef> _structTypes = new();

    // Current function being generated
    private LLVMValueRef _currentFunc;

    /// <summary>
    /// Generate LLVM IR for the given bound program.
    /// </summary>
    public LLVMModuleRef Generate(BoundProgram program)
    {
        _program = program;
        _context = LLVMContextRef.Create();
        _module = _context.CreateModuleWithName("main");
        _builder = _context.CreateBuilder();

        // First pass: declare all globals and functions
        foreach (var decl in program.Declarations)
        {
            if (decl is BoundFunctionDecl func)
                DeclareFunction(func);
            else if (decl is BoundVariableDecl varDecl)
                DeclareGlobal(varDecl);
        }

        // Second pass: generate function bodies
        foreach (var decl in program.Declarations)
        {
            if (decl is BoundFunctionDecl func)
                GenerateFunction(func);
        }

        return _module;
    }

    private void DeclareGlobal(BoundVariableDecl decl)
    {
        var llvmTy = GetLLVMType(decl.DeclaredType);
        var isExternal = decl.Initializer == null;
        var global = _module.AddGlobal(llvmTy, decl.Symbol.ImportName ?? decl.Symbol.Name);
        global.Linkage = isExternal ? LLVMLinkage.LLVMExternalLinkage : LLVMLinkage.LLVMInternalLinkage;
        if (!isExternal)
            global.Initializer = GenerateConstant(decl.Initializer!);
        _globals[decl.Symbol] = global;
    }

    /// <summary>
    /// Emit the generated module to an object file.
    /// </summary>
    public unsafe void EmitObjectFile(string outputPath)
    {
        LLVM.InitializeAllTargetInfos();
        LLVM.InitializeAllTargets();
        LLVM.InitializeAllTargetMCs();
        LLVM.InitializeAllAsmPrinters();

        var triple = LLVMTargetRef.DefaultTriple;
        var target = LLVMTargetRef.GetTargetFromTriple(triple);
        var targetMachine = target.CreateTargetMachine(
            triple, "", "", LLVMCodeGenOptLevel.LLVMCodeGenLevelNone,
            LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);

        targetMachine.EmitToFile(_module, outputPath, LLVMCodeGenFileType.LLVMObjectFile);
        LLVM.DisposeTargetMachine(targetMachine);
    }

    public void Dispose()
    {
        _builder.Dispose();
        _module.Dispose();
        _context.Dispose();
    }

    /// <summary>
    /// LLVM type mapping from solid-lang types.
    /// </summary>
    private LLVMTypeRef GetLLVMType(SolidType? type)
    {
        if (type is PrimitiveType pt)
        {
            return pt.Kind switch
            {
                SolidTypeKind.Primitive => pt.Name switch
                {
                    "i8" => _context.Int8Type,
                    "i16" => _context.Int16Type,
                    "i32" => _context.Int32Type,
                    "i64" => _context.Int64Type,
                    "u8" => _context.Int8Type,
                    "u16" => _context.Int16Type,
                    "u32" => _context.Int32Type,
                    "u64" => _context.Int64Type,
                    "isize" => _context.Int64Type,  // assume 64-bit
                    "usize" => _context.Int64Type,  // assume 64-bit
                    "f32" => _context.FloatType,
                    "f64" => _context.DoubleType,
                    "bool" => _context.Int1Type,
                    "void" => _context.VoidType,
                    _ => _context.Int32Type,
                },
                _ => _context.Int32Type,
            };
        }
        // Named types (structs, enums, etc.)
        if (type is NamedType nt)
        {
            if (!_structTypes.TryGetValue(nt.TypeSymbol, out var structTy))
            {
                var fields = nt.TypeSymbol.TypeScope!.Members.Values
                    .OfType<MemberSymbol>()
                    .OrderBy(m => m.Declaration!.Span.Start)
                    .Select(m => GetLLVMType(m.MemberType))
                    .ToArray();
                structTy = LLVMTypeRef.CreateStruct(fields, false);
                _structTypes[nt.TypeSymbol] = structTy;
            }
            return structTy;
        }
        // Pointer types
        if (type is PointerType)
            return LLVMTypeRef.CreatePointer(_context.Int8Type, 0);
        // Fallback
        return _context.Int32Type;
    }

    /// <summary>
    /// Declare an LLVM function (without body).
    /// </summary>
    private void DeclareFunction(BoundFunctionDecl func)
    {
        var returnTy = func.ReturnType != null ? GetLLVMType(func.ReturnType) : _context.VoidType;
        var paramTys = func.Parameters.Select(p => GetLLVMType(p.DeclaredType)).ToArray();
        var funcTy = LLVMTypeRef.CreateFunction(returnTy, paramTys, false);
        var llvmFunc = _module.AddFunction(func.Symbol.ImportName ?? func.Symbol.Name, funcTy);
        _functions[func.Symbol] = llvmFunc;
        _functionTypes[func.Symbol] = funcTy;
    }
}
