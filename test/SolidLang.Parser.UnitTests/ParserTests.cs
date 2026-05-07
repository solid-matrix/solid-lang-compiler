using SolidLang.Parser;
using SolidLang.Parser.Nodes.Declarations;
using SolidLang.Parser.Parser;
using SolidParser = SolidLang.Parser.Parser.Parser;

namespace SolidLang.Parser.UnitTests;

public class ParserTests
{
    private static string GetExamplePath(string filename)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, "example", filename);
    }

    private static (ProgramNode program, bool hasErrors, IReadOnlyList<Diagnostic> diagnostics) ParseFile(string filename)
    {
        var path = GetExamplePath(filename);
        var source = SourceText.From(File.ReadAllText(path));
        var parser = new SolidParser(source);
        var program = parser.ParseProgram();
        return (program, parser.HasErrors, parser.Diagnostics);
    }

    private static (ProgramNode program, bool hasErrors, IReadOnlyList<Diagnostic> diagnostics) ParseCode(string code)
    {
        var source = SourceText.From(code);
        var parser = new SolidParser(source);
        var program = parser.ParseProgram();
        return (program, parser.HasErrors, parser.Diagnostics);
    }

    [Fact]
    public void Parse_1_Main_ShouldSucceed()
    {
        var (program, hasErrors, diagnostics) = ParseFile("1-main.solid");

        Assert.False(hasErrors, diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error");
        Assert.NotNull(program);
        Assert.Single(program.Declarations);

        var func = Assert.IsType<FunctionDeclNode>(program.Declarations[0]);
        Assert.Equal("main", func.Name);
    }

    [Fact]
    public void Parse_2_NamespaceUsing_ShouldSucceed()
    {
        var (program, hasErrors, diagnostics) = ParseFile("2-namespace-using.solid");

        Assert.False(hasErrors, diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error");
        Assert.NotNull(program);
        Assert.NotNull(program.Namespace);
        Assert.Equal("main", program.Namespace!.Path.GetFullText());
        Assert.Equal(2, program.Usings.Count);
    }

    [Fact]
    public void Parse_3_Struct_ShouldSucceed()
    {
        var (program, hasErrors, diagnostics) = ParseFile("3-struct.solid");

        // Report all diagnostics for debugging
        if (hasErrors)
        {
            foreach (var d in diagnostics)
                Console.WriteLine($"Diagnostic: {d}");
        }

        Assert.False(hasErrors, diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error");
        Assert.NotNull(program);
        Assert.True(program.Declarations.Count >= 2, "Should have at least 2 struct declarations");
    }

    [Fact]
    public void Parse_4_Enum_ShouldSucceed()
    {
        var (program, hasErrors, diagnostics) = ParseFile("4-enum.solid");

        if (hasErrors)
        {
            foreach (var d in diagnostics)
                Console.WriteLine($"Diagnostic: {d}");
        }

        Assert.False(hasErrors, diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error");
        Assert.NotNull(program);
        Assert.True(program.Declarations.Count >= 2, "Should have at least 2 enum declarations");
    }

    [Fact]
    public void Parse_5_Union_ShouldSucceed()
    {
        var (program, hasErrors, diagnostics) = ParseFile("5-union.solid");

        if (hasErrors)
        {
            foreach (var d in diagnostics)
                Console.WriteLine($"Diagnostic: {d}");
        }

        Assert.False(hasErrors, diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error");
        Assert.NotNull(program);
    }

    [Fact]
    public void Parse_6_Variant_ShouldSucceed()
    {
        var (program, hasErrors, diagnostics) = ParseFile("6-variant.solid");

        if (hasErrors)
        {
            foreach (var d in diagnostics)
                Console.WriteLine($"Diagnostic: {d}");
        }

        Assert.False(hasErrors, diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error");
        Assert.NotNull(program);
    }

    [Fact]
    public void Parse_7_ConstStaticVar_ShouldSucceed()
    {
        var (program, hasErrors, diagnostics) = ParseFile("7-const-static-var.solid");

        if (hasErrors)
        {
            foreach (var d in diagnostics)
                Console.WriteLine($"Diagnostic: {d}");
        }

        Assert.False(hasErrors, diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error");
        Assert.NotNull(program);
    }

    [Fact]
    public void Parse_8_CommentDoc_ShouldSucceed()
    {
        var (program, hasErrors, diagnostics) = ParseFile("8-comment-doc.solid");

        if (hasErrors)
        {
            foreach (var d in diagnostics)
                Console.WriteLine($"Diagnostic: {d}");
        }

        Assert.False(hasErrors, diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error");
        Assert.NotNull(program);
    }

    // Generic nested type test - the key test case for >> ambiguity
    [Fact]
    public void Parse_NestedGenerics_ShouldSucceed()
    {
        var code = """
            func test() {
                var nested = List<Vector<f32>>{};
            }
            """;

        var (program, hasErrors, diagnostics) = ParseCode(code);

        Assert.False(hasErrors, diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error");
        Assert.NotNull(program);
    }

    // Deeply nested generics
    [Fact]
    public void Parse_DeeplyNestedGenerics_ShouldSucceed()
    {
        var code = """
            func test() {
                var deep = Map<String, List<Vector<f32>>>{};
            }
            """;

        var (program, hasErrors, diagnostics) = ParseCode(code);

        Assert.False(hasErrors, diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error");
        Assert.NotNull(program);
    }

    // Right shift in expression (not generic)
    [Fact]
    public void Parse_RightShiftExpression_ShouldSucceed()
    {
        var code = """
            func test(): i32 {
                return 8 >> 2;
            }
            """;

        var (program, hasErrors, diagnostics) = ParseCode(code);

        Assert.False(hasErrors, diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error");
        Assert.NotNull(program);
    }

    // Mixed: generic and right shift
    [Fact]
    public void Parse_GenericAndRightShift_ShouldSucceed()
    {
        var code = """
            func test<T>(x: T): i32 {
                var list = List<T>{};
                return 8 >> 2;
            }
            """;

        var (program, hasErrors, diagnostics) = ParseCode(code);

        Assert.False(hasErrors, diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error");
        Assert.NotNull(program);
    }
}
