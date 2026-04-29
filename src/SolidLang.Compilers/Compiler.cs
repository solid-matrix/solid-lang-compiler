using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using SolidLang.Compilers.CodeGen;
using SolidLang.Compilers.Semantic;

namespace SolidLang.Compilers;

public static class Compiler
{
    public static void Compile(string sourceCode, string outputFilePath)
    {
        var inputStream = new AntlrInputStream(sourceCode);
        var lexer = new SolidLangLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new SolidLangParser(tokenStream);

        var tree = parser.program();

        if (parser.NumberOfSyntaxErrors > 0)
        {
            throw new InvalidOperationException($"Syntax errors: {parser.NumberOfSyntaxErrors}");
        }

        var semanticAnalyzer = new SemanticAnalyzer();
        ParseTreeWalker.Default.Walk(semanticAnalyzer, tree);

        using var codeGen = new LLVMCodeGenerator("solid_module");
        codeGen.SetFunctions(semanticAnalyzer.Functions);
        codeGen.GenerateModule(tree);

        var ir = codeGen.GetIR();
        File.WriteAllText(outputFilePath, ir);
    }

    public static void CompileToObjectFile(string sourceCode, string outputFilePath)
    {
        var inputStream = new AntlrInputStream(sourceCode);
        var lexer = new SolidLangLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new SolidLangParser(tokenStream);

        var tree = parser.program();

        if (parser.NumberOfSyntaxErrors > 0)
        {
            throw new InvalidOperationException($"Syntax errors: {parser.NumberOfSyntaxErrors}");
        }

        var semanticAnalyzer = new SemanticAnalyzer();
        ParseTreeWalker.Default.Walk(semanticAnalyzer, tree);

        using var codeGen = new LLVMCodeGenerator("solid_module");
        codeGen.SetFunctions(semanticAnalyzer.Functions);
        codeGen.GenerateModule(tree);

        codeGen.EmitObjectFile(outputFilePath);
    }
}
