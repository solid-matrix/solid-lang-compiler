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

        // Verify key function names are present
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

        // @import func malloc(size: usize)cdecl:*opaque;
        var malloc = funcs.First(f => f.Name == "malloc");
        Assert.True(malloc.IsForwardDecl);
        Assert.NotNull(malloc.Annotations);
        Assert.NotNull(malloc.CallingConvention);
        Assert.Equal(SyntaxKind.CDeclKeyword, malloc.CallingConvention!.Convention);

        // @import func create_window(size: usize)stdcall:*opaque;
        var createWindow = funcs.First(f => f.Name == "create_window");
        Assert.True(createWindow.IsForwardDecl);
        Assert.NotNull(createWindow.CallingConvention);
        Assert.Equal(SyntaxKind.StdCallKeyword, createWindow.CallingConvention!.Convention);

        // @export func add(a: i32, b: i32)cdecl:i32{ return a+b; }
        var add = funcs.First(f => f.Name == "add" && f.NamespacePrefix == null && f.WhereClauses == null);
        Assert.False(add.IsForwardDecl);
        Assert.NotNull(add.Body);
        Assert.NotNull(add.Parameters);
        Assert.Equal(2, add.Parameters!.Parameters.Count);

        // func get_size<T>(value: T):usize{ ... }
        var getSize = funcs.First(f => f.Name == "get_size");
        Assert.NotNull(getSize.GenericParams);
        Assert.Single(getSize.GenericParams!.Parameters);

        // func flat<T>(value: List<List<T>>):List<T>{ ... }
        var flat = funcs.First(f => f.Name == "flat" && f.GenericParams != null);
        Assert.NotNull(flat.Parameters);
        Assert.Single(flat.Parameters!.Parameters);

        // func main(args: Slice<String>):i32{ return 0; }
        var main = funcs.Last(f => f.Name == "main");
        Assert.NotNull(main.Parameters);
        Assert.Single(main.Parameters!.Parameters);
        Assert.Equal("args", main.Parameters.Parameters[0].Name);

        // func resursive(n: i32):i32{ if n <= 0 {return 1;} ... }
        var recursive = funcs.First(f => f.Name == "resursive");
        Assert.NotNull(recursive.Body);

        // func Vector2::add — extension method with namespace prefix
        var vecAdd2 = funcs.FirstOrDefault(f => f.Name == "add" && f.NamespacePrefix != null);
        Assert.NotNull(vecAdd2);
        Assert.NotNull(vecAdd2.Body);

        // func Vector2<T>::add — generic extension method with where clause
        var vecAddT = funcs.FirstOrDefault(f => f.Name == "add" && f.WhereClauses != null);
        Assert.NotNull(vecAddT);
        Assert.NotNull(vecAddT.GenericParams);
        Assert.NotNull(vecAddT.WhereClauses);
        Assert.Single(vecAddT.WhereClauses!.Clauses);
        Assert.Equal("T", vecAddT.WhereClauses.Clauses[0].TypeParamName);
    }

    // Quick verification: parse all new example files
    [Fact]
    public void Parse_NewExamples_Verify()
    {
        foreach (var f in new[] { "10-operators.solid", "11-control-flow.solid", "12-pointers.solid", "13-interface.solid", "14-array.solid", "15-generics-edge.solid" })
        {
            var (program, hasErrors, diagnostics) = ParseFile(f);
            Console.WriteLine($"\n=== {f} ===");
            if (hasErrors)
            {
                foreach (var d in diagnostics)
                    Console.WriteLine($"  ERROR: {d}");
            }
            Console.WriteLine($"  Declarations: {program.Declarations.Count}, HasErrors: {hasErrors}");
        }
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
