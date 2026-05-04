using SolidLangCompiler.CodeGenerators;
using SolidLangCompiler.SemanticAnalyzers;

namespace SolidLangCompiler;

internal class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: solidc <source.solid> [-o output]");
            Console.WriteLine("  -o <path>  Output file path (without extension)");
            Console.WriteLine("  --ir       Output LLVM IR instead of object file");
            return 1;
        }

        var sourcePath = args[0];
        var outputPath = "a.out";
        var emitIr = false;

        for (var i = 1; i < args.Length; i++)
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
                default:
                    Console.Error.WriteLine($"Error: Unknown option: {args[i]}");
                    return 1;
            }
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

        if (emitIr)
        {
            codeGen.GenerateIr(program, outputPath + ".ll", CodeGenerator.DefaultTriple);
            Console.WriteLine($"Generated: {outputPath}.ll");
        }
        else
        {
            codeGen.GenerateObjective(program, outputPath + ".o", CodeGenerator.DefaultTriple);
            Console.WriteLine($"Generated: {outputPath}.o");
        }

        return 0;
    }
}
