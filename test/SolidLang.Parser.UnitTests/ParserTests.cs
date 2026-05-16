using SolidLang.Parser;
using SolidLang.Parser.Nodes.Declarations;
using SolidLang.Parser.Parser;
using SolidParser = SolidLang.Parser.Parser.Parser;

namespace SolidLang.Parser.UnitTests;

public class ParserTests
{
    private static string CasesDir =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cases");

    private static (ProgramNode program, bool hasErrors, IReadOnlyList<Diagnostic> diagnostics) ParseFile(string filename)
    {
        var path = Path.Combine(CasesDir, filename);
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

    // ========================================
    // Snapshot tests — every .solid case file
    // ========================================

    public static IEnumerable<object[]> AllSolidFiles =>
        Directory.GetFiles(CasesDir, "*.solid")
            .Select(Path.GetFileName)
            .OrderBy(f => int.TryParse(f!.Split('-')[0], out var n) ? n : int.MaxValue)
            .ThenBy(f => f)
            .Select(f => new object[] { f! });

    [Theory]
    [MemberData(nameof(AllSolidFiles))]
    public void CaseFile_ParsesWithZeroErrorsAndMatchesSnapshot(string filename)
    {
        var (program, hasErrors, diagnostics) = ParseFile(filename);

        if (hasErrors)
        {
            foreach (var d in diagnostics)
                Console.WriteLine($"  ERROR: {d}");
        }

        Assert.False(hasErrors, $"{filename}: {diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error"}");
        Assert.NotNull(program);

        // Snapshot match
        var astPath = Path.Combine(CasesDir, Path.ChangeExtension(filename, ".solid.ast"));
        Assert.True(File.Exists(astPath), $"Missing .ast snapshot for {filename}");
        var expectedAst = File.ReadAllText(astPath);
        var actualAst = program.ToString();
        Assert.Equal(expectedAst, actualAst);
    }

    // ========================================
    // Detailed assertions for key cases
    // ========================================

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

    [Fact]
    public void Parse_9_Function_ShouldSucceed()
    {
        var (program, hasErrors, diagnostics) = ParseFile("9-function.solid");

        if (hasErrors)
        {
            foreach (var d in diagnostics)
                Console.WriteLine($"Diagnostic: {d}");
        }

        Assert.False(hasErrors, diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error");
        Assert.NotNull(program);

        var funcs = program.Declarations.OfType<FunctionDeclNode>().ToList();
        var structs = program.Declarations.OfType<StructDeclNode>().ToList();

        Assert.True(funcs.Count >= 8, $"Should have at least 8 functions, got {funcs.Count}");
        Assert.Equal(2, structs.Count);

        var names = funcs.Select(f => f.Name).ToHashSet();
        Assert.Contains("malloc", names);
        Assert.Contains("create_window", names);
        Assert.Contains("add", names);
        Assert.Contains("get_size", names);
        Assert.Contains("flat", names);
        Assert.Contains("resursive", names);
        Assert.Contains("main", names);
    }

    [Fact]
    public void Parse_9a_FunctionDetails()
    {
        var (program, hasErrors, diagnostics) = ParseFile("9-function.solid");
        Assert.False(hasErrors, diagnostics.FirstOrDefault()?.ToString() ?? "Unknown error");

        var funcs = program.Declarations.OfType<FunctionDeclNode>().ToList();

        var malloc = funcs.First(f => f.Name == "malloc");
        Assert.Null(malloc.Body);
        Assert.NotNull(malloc.Annotations);
        Assert.NotNull(malloc.CallingConvention);
        Assert.Equal(SyntaxKind.CDeclKeyword, malloc.CallingConvention!.Convention);

        var createWindow = funcs.First(f => f.Name == "create_window");
        Assert.Null(createWindow.Body);
        Assert.NotNull(createWindow.CallingConvention);
        Assert.Equal(SyntaxKind.StdCallKeyword, createWindow.CallingConvention!.Convention);

        var add = funcs.First(f => f.Name == "add" && f.NamedTypePrefix == null && f.WhereClauses.Count == 0);
        Assert.NotNull(add.Body);
        Assert.NotNull(add.Body);
        Assert.NotNull(add.Parameters);
        Assert.Equal(2, add.Parameters.Count);

        var getSize = funcs.First(f => f.Name == "get_size");
        Assert.NotNull(getSize.GenericParams);
        Assert.Single(getSize.GenericParams);

        var flat = funcs.First(f => f.Name == "flat" && f.GenericParams.Count > 0);
        Assert.NotNull(flat.Parameters);
        Assert.Single(flat.Parameters);

        var main = funcs.Last(f => f.Name == "main");
        Assert.NotNull(main.Parameters);
        Assert.Single(main.Parameters);
        Assert.Equal("args", main.Parameters[0].Name);

        var recursive = funcs.First(f => f.Name == "resursive");
        Assert.NotNull(recursive.Body);

        var vecAdd2 = funcs.FirstOrDefault(f => f.Name == "add" && f.NamedTypePrefix != null);
        Assert.NotNull(vecAdd2);
        Assert.NotNull(vecAdd2.Body);

        var vecAddT = funcs.FirstOrDefault(f => f.Name == "add" && f.NamedTypePrefix != null && f.WhereClauses.Count > 0);
        Assert.NotNull(vecAddT);
        Assert.NotNull(vecAddT.NamedTypePrefix);
        Assert.NotNull(vecAddT.WhereClauses);
        Assert.Single(vecAddT.WhereClauses);
        Assert.Equal("T", vecAddT.WhereClauses[0].TypeParamName);
    }

    // ========================================
    // Generic / right-shift ambiguity tests
    // ========================================

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
