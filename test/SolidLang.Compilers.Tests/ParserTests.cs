using Antlr4.Runtime;

namespace SolidLang.Compilers.Tests;

public class ParserTests
{
    [Fact]
    public void Parser_ShouldParse_MinimalProgram()
    {
        var input = "namespace test;";
        var inputStream = new AntlrInputStream(input);
        var lexer = new SolidLangLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new SolidLangParser(tokenStream);

        var tree = parser.program();

        Assert.NotNull(tree);
        Assert.False(parser.NumberOfSyntaxErrors > 0);
    }

    [Fact]
    public void Parser_ShouldParse_StructDeclaration()
    {
        var input = @"
namespace test;

struct Point {
    x: f32,
    y: f32,
}
";
        var inputStream = new AntlrInputStream(input);
        var lexer = new SolidLangLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new SolidLangParser(tokenStream);

        var tree = parser.program();

        Assert.NotNull(tree);
        Assert.False(parser.NumberOfSyntaxErrors > 0);
    }

    [Fact]
    public void Parser_ShouldParse_FunctionDeclaration()
    {
        var input = @"
namespace test;

func add(a: i32, b: i32): i32 {
    return a + b;
}
";
        var inputStream = new AntlrInputStream(input);
        var lexer = new SolidLangLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new SolidLangParser(tokenStream);

        var tree = parser.program();

        Assert.NotNull(tree);
        Assert.False(parser.NumberOfSyntaxErrors > 0);
    }
}