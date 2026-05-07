using SolidLang.Parser;
using SolidParser = SolidLang.Parser.Parser.Parser;

var file = args.Length > 0 ? args[0] : "10-operators.solid";
var baseDir = "/workspace/solid-lang-compiler/example";
var path = Path.Combine(baseDir, file);

Console.WriteLine($"Parsing: {path}");

var source = SourceText.From(File.ReadAllText(path));
var parser = new SolidParser(source);
var program = parser.ParseProgram();

Console.WriteLine($"Declarations: {program.Declarations.Count}");
Console.WriteLine($"HasErrors: {parser.HasErrors}");
if (parser.HasErrors)
{
    foreach (var d in parser.Diagnostics)
        Console.WriteLine($"  {d}");
}
return parser.HasErrors ? 1 : 0;
