using System.Diagnostics;
using LLVMSharp.Interop;
using SolidLang.Parser;
using SolidLang.Parser.Nodes.Declarations;
using SolidLang.SemanticAnalyzer;
using SolidLang.IrGenerator;
using SolidParser = SolidLang.Parser.Parser.Parser;

// Parse arguments: [-o <output>] <filename>
string? file = null;
string? outputExe = null;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "-o" && i + 1 < args.Length)
    {
        outputExe = args[++i];
    }
    else
    {
        file = args[i];
    }
}

if (file == null)
{
    Console.WriteLine("Usage: dotnet run -- <filename> [-o <output>]");
    return 1;
}

var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var casesDir = Path.Combine(projectRoot, "test", "SolidLang.Parser.UnitTests", "cases");

// Resolve file path: try direct path, then cases/, then project-relative
var path = file;
if (!File.Exists(path))
    path = Path.Combine(casesDir, file);
if (!File.Exists(path))
    path = Path.Combine(projectRoot, file);

if (!File.Exists(path))
{
    Console.WriteLine($"File not found: {file}");
    return 1;
}

// Output directory: .build/ under the source file's directory
var buildDir = Path.Combine(Path.GetDirectoryName(path)!, ".build");
Directory.CreateDirectory(buildDir);

var baseName = Path.GetFileNameWithoutExtension(path);

// Parse main file
var source = SourceText.From(File.ReadAllText(path));
var parser = new SolidParser(source);
var program = parser.ParseProgram();

// Parse std library files
var stdDir = Path.Combine(projectRoot, "std");
var programs = new List<ProgramNode> { program };
var hasParseErrors = parser.HasErrors;

if (Directory.Exists(stdDir))
{
    foreach (var stdFile in Directory.GetFiles(stdDir, "*.solid", SearchOption.AllDirectories))
    {
        var stdSource = SourceText.From(File.ReadAllText(stdFile));
        var stdParser = new SolidParser(stdSource);
        var stdProgram = stdParser.ParseProgram();
        programs.Add(stdProgram);
        if (stdParser.HasErrors)
        {
            hasParseErrors = true;
            foreach (var d in stdParser.Diagnostics)
                Console.WriteLine($"  {stdFile}: {d}");
        }
    }
}

// Save AST to .build/
var astPath = Path.Combine(buildDir, baseName + ".solid.ast");
File.WriteAllText(astPath, program.ToString());

// Semantic analysis + IR generation
int semanticErrors = 0;
if (!hasParseErrors)
{
    var semanticModel = SemanticModel.Create(programs);
    semanticErrors = semanticModel.GetDiagnostics().Count;

    // Save bound tree to .build/
    var boundAstPath = Path.Combine(buildDir, baseName + ".bound.ast");
    File.WriteAllText(boundAstPath, BoundTreePrinter.Print(semanticModel.BoundProgram));

    if (semanticErrors > 0)
    {
        foreach (var d in semanticModel.GetDiagnostics())
            Console.WriteLine($"  {d}");
    }
    else
    {
        // Generate LLVM IR
        var codegen = new CodeGenerator();
        using var module = codegen.Generate(semanticModel.BoundProgram);

        var relativeDir = buildDir.Replace(projectRoot, "").TrimStart(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '/');

        // Save LLVM IR to .build/
        var llPath = Path.Combine(buildDir, baseName + ".ll");
        File.WriteAllText(llPath, module.PrintToString());
        Console.WriteLine($"  LLVM IR -> {relativeDir}/{baseName}.ll");

        // Emit object file to .build/
        var objPath = Path.Combine(buildDir, baseName + ".obj");
        codegen.EmitObjectFile(objPath);
        Console.WriteLine($"  Object file -> {relativeDir}/{baseName}.obj");

        // Determine executable path
        outputExe ??= Path.Combine(buildDir, baseName + (OperatingSystem.IsWindows() ? ".exe" : ""));

        // Link with clang
        var clang = "clang";
        var linkArgs = $"\"{objPath}\" -o \"{outputExe}\"";
        var psi = new ProcessStartInfo(clang, linkArgs)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)!;
        process.WaitForExit();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();

        if (process.ExitCode != 0)
        {
            Console.WriteLine($"  Linker failed (exit {process.ExitCode}):");
            if (!string.IsNullOrWhiteSpace(stdout)) Console.WriteLine(stdout);
            if (!string.IsNullOrWhiteSpace(stderr)) Console.WriteLine(stderr);
            return 1;
        }

        var exeDisplay = outputExe.Replace(projectRoot, "").TrimStart(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '/');
        Console.WriteLine($"  Executable -> {exeDisplay}");
    }
}

Console.WriteLine($"{file}: {program.Declarations.Count} decls, parse_errors={hasParseErrors}, semantic_errors={semanticErrors}");
if (parser.HasErrors)
{
    foreach (var d in parser.Diagnostics)
        Console.WriteLine($"  {d}");
}

return (hasParseErrors || semanticErrors > 0) ? 1 : 0;
