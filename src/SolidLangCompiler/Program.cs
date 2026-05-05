using SolidLangCompiler.CodeGenerators;
using SolidLangCompiler.SemanticAnalyzers;

namespace SolidLangCompiler;

internal class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var sourcePath = string.Empty;
        var outputPath = "a.out";
        var emitIr = false;
        var emitObject = false;
        var emitExe = false;
        var emitShared = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Error: -o requires an argument");
                        return 1;
                    }
                    outputPath = args[++i];
                    break;
                case "--ir":
                    emitIr = true;
                    break;
                case "--obj":
                    emitObject = true;
                    break;
                case "--exe":
                    emitExe = true;
                    break;
                case "--shared":
                    emitShared = true;
                    break;
                case "-h":
                case "--help":
                    PrintUsage();
                    return 0;
                default:
                    if (!args[i].StartsWith("-") && string.IsNullOrEmpty(sourcePath))
                    {
                        sourcePath = args[i];
                    }
                    else
                    {
                        Console.Error.WriteLine($"Error: Unknown option: {args[i]}");
                        return 1;
                    }
                    break;
            }
        }

        // Default to executable if no output type specified
        if (!emitIr && !emitObject && !emitExe && !emitShared)
        {
            emitExe = true;
        }

        // Validate only one output type
        var outputTypes = new[] { emitIr, emitObject, emitExe, emitShared }.Count(x => x);
        if (outputTypes > 1)
        {
            Console.Error.WriteLine("Error: Only one output type can be specified (--ir, --obj, --exe, --shared)");
            return 1;
        }

        if (string.IsNullOrEmpty(sourcePath))
        {
            Console.Error.WriteLine("Error: No source file specified");
            return 1;
        }

        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Error: File not found: {sourcePath}");
            return 1;
        }

        var source = File.ReadAllText(sourcePath);

        // Semantic analysis
        using var analyzer = new SemanticAnalyzer();
        var program = analyzer.Analyze(source, sourcePath);

        if (!program.IsSuccessful)
        {
            foreach (var error in program.Errors)
            {
                Console.Error.WriteLine(error);
            }
            return 1;
        }

        foreach (var warning in program.Warnings)
        {
            Console.WriteLine(warning);
        }

        // Code generation
        using var codeGen = new CodeGenerator();
        var triple = CodeGenerator.DefaultTriple;

        if (emitIr)
        {
            var irPath = outputPath.EndsWith(".ll") ? outputPath : outputPath + ".ll";
            codeGen.GenerateIr(program, irPath, triple);
            Console.WriteLine($"Generated: {irPath}");
        }
        else if (emitObject)
        {
            var objPath = outputPath.EndsWith(".o") ? outputPath : outputPath + ".o";
            codeGen.GenerateObjective(program, objPath, triple);
            Console.WriteLine($"Generated: {objPath}");
        }
        else if (emitShared)
        {
            var objPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.o");
            var soPath = outputPath.EndsWith(".so") ? outputPath : outputPath + ".so";

            try
            {
                codeGen.GenerateObjective(program, objPath, triple);

                if (!Linker.LinkSharedLibrary(objPath, soPath, out var error))
                {
                    Console.Error.WriteLine(error);
                    return 1;
                }

                Console.WriteLine($"Generated: {soPath}");
            }
            finally
            {
                if (File.Exists(objPath))
                    File.Delete(objPath);
            }
        }
        else // emitExe
        {
            var objPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.o");

            try
            {
                codeGen.GenerateObjective(program, objPath, triple);

                if (!Linker.LinkExecutable(objPath, outputPath, out var error))
                {
                    Console.Error.WriteLine(error);
                    return 1;
                }

                Console.WriteLine($"Generated: {outputPath}");
            }
            finally
            {
                if (File.Exists(objPath))
                    File.Delete(objPath);
            }
        }

        return 0;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("solidc - Solid Language Compiler");
        Console.WriteLine();
        Console.WriteLine("Usage: solidc <source.solid> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -o <path>     Output file path");
        Console.WriteLine("  --ir          Output LLVM IR (.ll)");
        Console.WriteLine("  --obj         Output object file (.o)");
        Console.WriteLine("  --exe         Output executable (default)");
        Console.WriteLine("  --shared      Output shared library (.so)");
        Console.WriteLine("  -h, --help    Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  solidc main.solid                    # Compile to executable 'a.out'");
        Console.WriteLine("  solidc main.solid -o program         # Compile to executable 'program'");
        Console.WriteLine("  solidc main.solid --ir               # Output LLVM IR to 'a.out.ll'");
        Console.WriteLine("  solidc main.solid --obj -o output    # Output object file 'output.o'");
        Console.WriteLine("  solidc lib.solid --shared -o mylib   # Output shared library 'mylib.so'");
    }
}
