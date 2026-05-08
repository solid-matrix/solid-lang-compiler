using SolidLang.Parser;
using SolidParser = SolidLang.Parser.Parser.Parser;

var file = args.Length > 0 ? args[0] : null;

if (file == null)
{
    Console.WriteLine("Usage: dotnet run -- <filename>");
    return 1;
}

var baseDir = "/workspace/solid-lang-compiler/example";
var path = Path.Combine(baseDir, file);

if (!File.Exists(path))
{
    Console.WriteLine($"File not found: {path}");
    return 1;
}

var source = SourceText.From(File.ReadAllText(path));
var parser = new SolidParser(source);
var program = parser.ParseProgram();

// Save AST to .ast file
var astPath = Path.ChangeExtension(path, ".solid.ast");
var astText = program.ToString();
File.WriteAllText(astPath, astText);

Console.WriteLine($"{file}: {program.Declarations.Count} decls, errors={parser.HasErrors}");
if (parser.HasErrors)
{
    foreach (var d in parser.Diagnostics)
        Console.WriteLine($"  {d}");
}

return parser.HasErrors ? 1 : 0;
