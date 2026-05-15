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

    // Loop context stack: tracks (mergeBlock, continueTarget) for break/continue
    // continueTarget = condBlock for while loops, updateBlock for for loops
    private readonly Stack<(LLVMBasicBlockRef Merge, LLVMBasicBlockRef ContinueTarget)> _loopStack = new();

    // Deferred statements for the current function (LIFO — last registered runs first)
    // Cleared at the start of each function.
    private readonly List<BoundStatement> _deferred = new();

    // When main has Slice<String> parameter, we rename it and generate a wrapper
    private FunctionSymbol? _solidMainSymbol;
    private static bool NeedsMainWrapper(BoundFunctionDecl func) =>
        func.Symbol.Name == "main"
        && func.Parameters.Count == 1
        && func.Parameters[0].DeclaredType is NamedType nt
        && nt.TypeSymbol.Name == "Slice"
        && nt.TypeArguments.Count == 1
        && nt.TypeArguments[0] is NamedType tnt
        && tnt.TypeSymbol.Name == "String";

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
            if (decl is BoundFunctionDecl func && !func.Symbol.IsIntrinsic)
                DeclareFunction(func);
            else if (decl is BoundVariableDecl varDecl)
                DeclareGlobal(varDecl);
        }

        // Second pass: generate function bodies
        foreach (var decl in program.Declarations)
        {
            if (decl is BoundFunctionDecl func && !func.Symbol.IsIntrinsic)
                GenerateFunction(func);
        }

        // If main needs argv wrapper, generate it
        if (_solidMainSymbol != null)
            GenerateMainWrapper();

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
    /// When main has a Slice&lt;String&gt; parameter, generate a C-compatible
    /// wrapper: int main(int argc, char** argv) that converts argc/argv
    /// to a Slice&lt;String&gt; and calls __solid_main.
    /// </summary>
    private void GenerateMainWrapper()
    {
        if (_solidMainSymbol == null) return;

        // Declare the solid main function reference
        var solidFunc = _functions[_solidMainSymbol];
        var solidFuncTy = _functionTypes[_solidMainSymbol];

        // Declare strlen: i64 strlen(ptr)
        var strlenTy = LLVMTypeRef.CreateFunction(_context.Int64Type, new[] { LLVMTypeRef.CreatePointer(_context.Int8Type, 0) }, false);
        var strlen = _module.AddFunction("strlen", strlenTy);

        // Declare C-compatible main: i32 main(i32 argc, i8** argv)
        var mainReturnTy = _context.Int32Type;
        var mainParamTys = new[] { _context.Int32Type, LLVMTypeRef.CreatePointer(LLVMTypeRef.CreatePointer(_context.Int8Type, 0), 0) };
        var mainFuncTy = LLVMTypeRef.CreateFunction(mainReturnTy, mainParamTys, false);
        var mainFunc = _module.AddFunction("main", mainFuncTy);

        _currentFunc = mainFunc;
        var entryBlock = mainFunc.AppendBasicBlock("entry");
        _builder.PositionAtEnd(entryBlock);

        var argc = mainFunc.GetParam(0u);
        var argv = mainFunc.GetParam(1u);
        argc.Name = "argc";
        argv.Name = "argv";

        // String struct type: { i8*, i64 }
        var stringTy = GetLLVMType(new NamedType(
            (_solidMainSymbol.ContainingScope!.LookupRecursive("String") as TypeSymbol)!));

        // Allocate dynamic array of String structs: alloca String, i32 %argc
        var stringsAlloca = _builder.BuildArrayAlloca(stringTy, argc, "strings");

        // Loop to convert each argv[i] to a String struct
        var loopCond = mainFunc.AppendBasicBlock("argv_loop_cond");

        // Alloca for loop counter
        var iAlloca = _builder.BuildAlloca(_context.Int32Type, "i");
        _builder.BuildStore(LLVMValueRef.CreateConstInt(_context.Int32Type, 0, false), iAlloca);
        _builder.BuildBr(loopCond);

        // Loop condition
        _builder.PositionAtEnd(loopCond);
        var iVal = _builder.BuildLoad2(_context.Int32Type, iAlloca, "i");
        var cond = _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, iVal, argc, "cmp");
        var loopBody = mainFunc.AppendBasicBlock("argv_loop_body");
        var loopExit = mainFunc.AppendBasicBlock("argv_loop_exit");
        _builder.BuildCondBr(cond, loopBody, loopExit);

        // Loop body
        _builder.PositionAtEnd(loopBody);

        // argv[i]
        var argvPtr = _builder.BuildGEP2(LLVMTypeRef.CreatePointer(_context.Int8Type, 0), argv, new[] { iVal }, "argv.i");
        var arg = _builder.BuildLoad2(LLVMTypeRef.CreatePointer(_context.Int8Type, 0), argvPtr, "arg");

        // strlen(arg)
        var argLen = _builder.BuildCall2(strlenTy, strlen, new[] { arg }, "len");

        // strings[i]
        var stringsITy = _builder.BuildGEP2(stringTy, stringsAlloca, new[] { iVal }, "strings.i");

        // Store ptr to strings[i].ptr
        var stringPtrField = _builder.BuildStructGEP2(stringTy, stringsITy, 0u, "str.ptr");
        _builder.BuildStore(arg, stringPtrField);

        // Store len to strings[i].len
        var stringLenField = _builder.BuildStructGEP2(stringTy, stringsITy, 1u, "str.len");
        _builder.BuildStore(argLen, stringLenField);

        // i++
        var iNext = _builder.BuildAdd(iVal, LLVMValueRef.CreateConstInt(_context.Int32Type, 1, false), "i.next");
        _builder.BuildStore(iNext, iAlloca);
        _builder.BuildBr(loopCond);

        // Loop exit: construct Slice<String> and call __solid_main
        _builder.PositionAtEnd(loopExit);

        // Slice<String> = { String*, i64 } — same layout as String { i8*, i64 }
        var sliceTy = GetLLVMType(_solidMainSymbol.ContainingScope!.LookupRecursive("Slice") is TypeSymbol sliceSym
            ? new NamedType(sliceSym, new List<SolidType> { new NamedType(
                (_solidMainSymbol.ContainingScope.LookupRecursive("String") as TypeSymbol)!) })
            : null!);

        // Alloca for the slice, then store fields
        var sliceAlloca = _builder.BuildAlloca(sliceTy, "slice");
        var slicePtrField = _builder.BuildStructGEP2(sliceTy, sliceAlloca, 0u, "slice.ptr");
        _builder.BuildStore(stringsAlloca, slicePtrField);

        // zext argc to i64 for the len field
        var argc64 = _builder.BuildZExt(argc, _context.Int64Type, "argc.64");
        var sliceLenField = _builder.BuildStructGEP2(sliceTy, sliceAlloca, 1u, "slice.len");
        _builder.BuildStore(argc64, sliceLenField);

        // Load the struct and call __solid_main
        var sliceVal = _builder.BuildLoad2(sliceTy, sliceAlloca, "slice.val");
        var ret = _builder.BuildCall2(solidFuncTy, solidFunc, new[] { sliceVal }, "ret");
        _builder.BuildRet(ret);
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
                    "bool" => _context.Int8Type,
                    "void" => _context.VoidType,
                    _ => _context.Int32Type,
                },
                _ => _context.Int32Type,
            };
        }
        // Named types (structs, enums, etc.)
        if (type is NamedType nt)
        {
            // Enum types are just integers
            if (nt.TypeSymbol.Kind == SymbolKind.Enum)
                return _context.Int32Type;

            if (!_structTypes.TryGetValue(nt.TypeSymbol, out var structTy))
            {
                // Forward-declared struct (e.g. `struct opaque;`) — use empty opaque struct
                if (nt.TypeSymbol.TypeScope == null)
                {
                    structTy = LLVMTypeRef.CreateStruct(Array.Empty<LLVMTypeRef>(), false);
                    _structTypes[nt.TypeSymbol] = structTy;
                    return structTy;
                }

                var memberFields = nt.TypeSymbol.TypeScope.Members.Values
                    .OfType<MemberSymbol>()
                    .OrderBy(m => m.Declaration!.Span.Start)
                    .Select(m => GetLLVMType(m.MemberType))
                    .ToArray();

                // Variant/Tagged union: prefix with i32 discriminant
                if (nt.TypeSymbol.Kind == SymbolKind.Variant)
                {
                    var fields = new LLVMTypeRef[memberFields.Length + 1];
                    fields[0] = _context.Int32Type;
                    Array.Copy(memberFields, 0, fields, 1, memberFields.Length);
                    structTy = LLVMTypeRef.CreateStruct(fields, false);
                }
                else
                {
                    structTy = LLVMTypeRef.CreateStruct(memberFields, false);
                }
                _structTypes[nt.TypeSymbol] = structTy;
            }
            return structTy;
        }
        // Pointer types
        if (type is PointerType ptr)
            return LLVMTypeRef.CreatePointer(GetLLVMType(ptr.PointeeType), 0);
        // Array types
        if (type is ArrayType arr)
        {
            var elemTy = GetLLVMType(arr.ElementType);
            var size = arr.Size ?? 0;
            return LLVMTypeRef.CreateArray(elemTy, (uint)size);
        }
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

        // If main has Slice<String> param, rename it to __solid_main
        var llvmName = func.Symbol.ImportName ?? func.Symbol.Name;
        if (NeedsMainWrapper(func))
        {
            _solidMainSymbol = func.Symbol;
            llvmName = "__solid_main";
        }

        var llvmFunc = _module.AddFunction(llvmName, funcTy);
        _functions[func.Symbol] = llvmFunc;
        _functionTypes[func.Symbol] = funcTy;
    }
}
