namespace SolidLangCompiler;

internal class Program
{
    private static void Main(string[] args)
    {
        using var cg = new CodeGenerator();

        using var sa = new SemanticAnalyzer();

        var st = new SemanticTree();

        cg.GenerateObjective(st, "hello.obj", CodeGenerator.DefaultTriple);

        cg.GenerateIr(st, "hello.ll", CodeGenerator.DefaultTriple);
    }
}