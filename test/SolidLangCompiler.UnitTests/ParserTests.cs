using Antlr4.Runtime;
using FluentAssertions;
using SolidLangCompiler.Generated;
using Xunit;

namespace SolidLangCompiler.UnitTests;

public class ParserTests
{
    private SolidLangParser CreateParser(string input)
    {
        var inputStream = new AntlrInputStream(input);
        var lexer = new SolidLangLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        return new SolidLangParser(tokenStream);
    }

    [Fact]
    public void Parser_ShouldParseNamespaceDeclaration()
    {
        var input = "namespace app;";
        var parser = CreateParser(input);

        var tree = parser.program();

        tree.Should().NotBeNull();
        tree.namespace_decl().Should().NotBeNull();
        parser.NumberOfSyntaxErrors.Should().Be(0);
    }

    [Fact]
    public void Parser_ShouldParseNestedNamespace()
    {
        var input = "namespace foo::bar::baz;";
        var parser = CreateParser(input);

        var tree = parser.program();

        tree.Should().NotBeNull();
        tree.namespace_decl().Should().NotBeNull();
        parser.NumberOfSyntaxErrors.Should().Be(0);
    }

    [Fact]
    public void Parser_ShouldParseUsingDeclaration()
    {
        var input = """
            namespace app;
            using std;
            using foo::bar;
            """;
        var parser = CreateParser(input);

        var tree = parser.program();

        tree.Should().NotBeNull();
        tree.using_decl().Should().HaveCount(2);
        parser.NumberOfSyntaxErrors.Should().Be(0);
    }

    [Fact]
    public void Parser_ShouldParseSimpleFunction()
    {
        var input = """
            namespace app;
            func main(): i32 {
                return 0;
            }
            """;
        var parser = CreateParser(input);

        var tree = parser.program();

        tree.Should().NotBeNull();
        tree.func_decl().Should().HaveCount(1);
        parser.NumberOfSyntaxErrors.Should().Be(0);
    }

    [Fact]
    public void Parser_ShouldParseFunctionWithParameters()
    {
        var input = """
            namespace app;
            func add(a: i32, b: i64): f64 {
                return 0.0;
            }
            """;
        var parser = CreateParser(input);

        var tree = parser.program();

        tree.Should().NotBeNull();
        var func = tree.func_decl()[0];
        func.func_header().func_parameters().func_parameter().Should().HaveCount(2);
        parser.NumberOfSyntaxErrors.Should().Be(0);
    }

    [Fact]
    public void Parser_ShouldParseStructDeclaration()
    {
        var input = """
            namespace app;
            struct Point {
                x: f32,
                y: f32,
            }
            """;
        var parser = CreateParser(input);

        var tree = parser.program();

        tree.Should().NotBeNull();
        tree.struct_decl().Should().HaveCount(1);
        parser.NumberOfSyntaxErrors.Should().Be(0);
    }

    [Fact]
    public void Parser_ShouldParseEnumDeclaration()
    {
        var input = """
            namespace app;
            enum Color: u8 {
                Red = 0,
                Green = 1,
                Blue = 2,
            }
            """;
        var parser = CreateParser(input);

        var tree = parser.program();

        tree.Should().NotBeNull();
        tree.enum_decl().Should().HaveCount(1);
        parser.NumberOfSyntaxErrors.Should().Be(0);
    }

    [Fact]
    public void Parser_ShouldParseConstDeclaration()
    {
        var input = """
            namespace app;
            const PI: f64 = 3.14159;
            """;
        var parser = CreateParser(input);

        var tree = parser.program();

        tree.Should().NotBeNull();
        tree.const_decl().Should().HaveCount(1);
        parser.NumberOfSyntaxErrors.Should().Be(0);
    }

    [Fact]
    public void Parser_ShouldParseStaticDeclaration()
    {
        var input = """
            namespace app;
            static counter: i32 = 0;
            """;
        var parser = CreateParser(input);

        var tree = parser.program();

        tree.Should().NotBeNull();
        tree.static_decl().Should().HaveCount(1);
        parser.NumberOfSyntaxErrors.Should().Be(0);
    }

    [Fact]
    public void Parser_ShouldParseIfStatement()
    {
        var input = """
            namespace app;
            func test(x: i32): i32 {
                if x > 0 {
                    return x;
                } else {
                    return -x;
                }
            }
            """;
        var parser = CreateParser(input);

        var tree = parser.program();

        tree.Should().NotBeNull();
        parser.NumberOfSyntaxErrors.Should().Be(0);
    }
}
