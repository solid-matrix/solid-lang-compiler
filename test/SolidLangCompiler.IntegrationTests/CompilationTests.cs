using FluentAssertions;
using SolidLangCompiler.CodeGenerators;
using SolidLangCompiler.SemanticAnalyzers;
using Xunit;

namespace SolidLangCompiler.IntegrationTests;

public class CompilationTests : IDisposable
{
    private readonly string _tempDir;

    public CompilationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"solid_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private string WriteSourceFile(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Compile_ShouldGenerateIrFile()
    {
        var sourcePath = WriteSourceFile("test.solid", """
            namespace app;
            func main(): i32 {
                return 0;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "test");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        File.Exists(outputPath + ".ll").Should().BeTrue();
        var irContent = File.ReadAllText(outputPath + ".ll");
        irContent.Should().Contain("define i32 @main()");
    }

    [Fact]
    public void Compile_ShouldGenerateObjectFile()
    {
        var sourcePath = WriteSourceFile("test.solid", """
            namespace app;
            func main(): i32 {
                return 0;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "test");
        codeGen.GenerateObjective(semanticTree, outputPath + ".o", CodeGenerator.DefaultTriple);

        File.Exists(outputPath + ".o").Should().BeTrue();
        var fileInfo = new FileInfo(outputPath + ".o");
        fileInfo.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compile_ShouldReportSyntaxErrors()
    {
        var sourcePath = WriteSourceFile("error.solid", """
            namespace app;
            func main( {
                return 0;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeFalse();
        semanticTree.Errors.Should().NotBeEmpty();
    }

    [Fact(Skip = "Empty programs are not supported yet - need namespace declaration")]
    public void Compile_ShouldHandleEmptyProgram()
    {
        var sourcePath = WriteSourceFile("empty.solid", "");

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Compile_ShouldHandleNamespaceOnly()
    {
        var sourcePath = WriteSourceFile("namespace_only.solid", "namespace app;");

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Compile_ShouldHandleMultipleFunctions()
    {
        var sourcePath = WriteSourceFile("multi_func.solid", """
            namespace app;

            func add(a: i32, b: i32): i32 {
                return a + b;
            }

            func main(): i32 {
                return add(1, 2);
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Compile_ShouldHandleStruct()
    {
        var sourcePath = WriteSourceFile("struct.solid", """
            namespace app;

            struct Point {
                x: f32,
                y: f32,
            }

            func main(): i32 {
                return 0;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Compile_ShouldHandleEnum()
    {
        var sourcePath = WriteSourceFile("enum.solid", """
            namespace app;

            enum Color: u8 {
                Red = 0,
                Green = 1,
                Blue = 2,
            }

            func main(): i32 {
                return 0;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Compile_ShouldHandleInfiniteFor()
    {
        var sourcePath = WriteSourceFile("for_infinite.solid", """
            namespace app;

            func main(): i32 {
                var count: i32 = 0;
                for {
                    count = count + 1;
                    if count > 10 {
                        break;
                    }
                }
                return count;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();
        semanticTree.Errors.Should().BeEmpty();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "for_infinite");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        var irContent = File.ReadAllText(outputPath + ".ll");
        irContent.Should().Contain("for.body");
        irContent.Should().Contain("for.end");
    }

    [Fact]
    public void Compile_ShouldHandleConditionalFor()
    {
        var sourcePath = WriteSourceFile("for_cond.solid", """
            namespace app;

            func countdown(n: i32): i32 {
                var result: i32 = 0;
                for n > 0 {
                    result = result + n;
                    n = n - 1;
                }
                return result;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "for_cond");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        var irContent = File.ReadAllText(outputPath + ".ll");
        irContent.Should().Contain("for.cond");
        irContent.Should().Contain("for.body");
    }

    [Fact]
    public void Compile_ShouldHandleCStyleFor()
    {
        // Note: In solid-lang, assignment is a statement, not an expression
        // So update part uses just an expression (which is evaluated but result discarded)
        var sourcePath = WriteSourceFile("for_cstyle.solid", """
            namespace app;

            func sum(n: i32): i32 {
                var total: i32 = 0;
                var i: i32 = 0;
                for ; i < n; i + 1 {
                    total = total + i;
                    i = i + 1;
                }
                return total;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "for_cstyle");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        var irContent = File.ReadAllText(outputPath + ".ll");
        irContent.Should().Contain("for.cond");
        irContent.Should().Contain("for.body");
    }

    [Fact]
    public void Compile_ShouldHandleBreakAndContinue()
    {
        // Using infinite for with break/continue
        var sourcePath = WriteSourceFile("break_continue.solid", """
            namespace app;

            func sum_odds(n: i32): i32 {
                var sum: i32 = 0;
                var i: i32 = 0;
                for i < n {
                    if i % 2 == 0 {
                        i = i + 1;
                        continue;
                    }
                    sum = sum + i;
                    if sum > 100 {
                        break;
                    }
                    i = i + 1;
                }
                return sum;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "break_continue");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        var irContent = File.ReadAllText(outputPath + ".ll");
        irContent.Should().Contain("br label");
    }

    [Fact]
    public void Compile_ShouldHandleNestedLoops()
    {
        // Using conditional for loops
        var sourcePath = WriteSourceFile("nested_loops.solid", """
            namespace app;

            func matrix_sum(rows: i32, cols: i32): i32 {
                var sum: i32 = 0;
                var i: i32 = 0;
                for i < rows {
                    var j: i32 = 0;
                    for j < cols {
                        sum = sum + 1;
                        j = j + 1;
                    }
                    i = i + 1;
                }
                return sum;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "nested_loops");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        var irContent = File.ReadAllText(outputPath + ".ll");
        irContent.Should().Contain("for.cond");
        irContent.Should().Contain("for.body");
    }

    [Fact]
    public void Compile_ShouldHandleSwitchStatement()
    {
        var sourcePath = WriteSourceFile("switch.solid", """
            namespace app;

            func classify(n: i32): i32 {
                switch n {
                    1 => return 1;
                    2 => return 2;
                    else => return 0;
                }
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "switch");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        var irContent = File.ReadAllText(outputPath + ".ll");
        irContent.Should().Contain("switch");
    }

    [Fact]
    public void Compile_ShouldHandleSwitchWithDefault()
    {
        var sourcePath = WriteSourceFile("switch_default.solid", """
            namespace app;

            func weekday(day: i32): i32 {
                switch day {
                    1 => return 1;
                    2 => return 2;
                    3 => return 3;
                    4 => return 4;
                    5 => return 5;
                    else => return 0;
                }
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "switch_default");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        var irContent = File.ReadAllText(outputPath + ".ll");
        irContent.Should().Contain("switch");
        irContent.Should().Contain("switch.default");
    }

    [Fact]
    public void Compile_ShouldHandleSwitchWithEnum()
    {
        var sourcePath = WriteSourceFile("switch_enum.solid", """
            namespace app;

            enum Color: u8 {
                Red = 0,
                Green = 1,
                Blue = 2,
            }

            func process(c: Color): i32 {
                switch c {
                    Color::Red => return 1;
                    Color::Green => return 2;
                    else => return 0;
                }
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Compile_ShouldHandleArrayLiteral()
    {
        var sourcePath = WriteSourceFile("array_literal.solid", """
            namespace app;

            func test(): i32 {
                var arr = [3]i32{1, 2, 3};
                return 0;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "array_literal");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        var irContent = File.ReadAllText(outputPath + ".ll");
        irContent.Should().Contain("alloca");
    }

    [Fact]
    public void Compile_ShouldHandleStructLiteral()
    {
        var sourcePath = WriteSourceFile("struct_literal.solid", """
            namespace app;

            struct Point {
                x: f32,
                y: f32,
            }

            func test(): i32 {
                var p = Point{x = 1.0, y = 2.0};
                return 0;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "struct_literal");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        var irContent = File.ReadAllText(outputPath + ".ll");
        irContent.Should().Contain("alloca");
    }

    [Fact]
    public void Compile_ShouldHandleEnumLiteralInExpression()
    {
        var sourcePath = WriteSourceFile("enum_expr.solid", """
            namespace app;

            enum Color: u8 {
                Red = 0,
                Green = 1,
                Blue = 2,
            }

            func get_color(): Color {
                return Color::Green;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Compile_ShouldHandleFieldAccess()
    {
        var sourcePath = WriteSourceFile("field_access.solid", """
            namespace app;

            struct Point {
                x: f32,
                y: f32,
            }

            func test(): f32 {
                var p = Point{x = 1.0, y = 2.0};
                return p.x;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "field_access");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        var irContent = File.ReadAllText(outputPath + ".ll");
        irContent.Should().Contain("getelementptr");
    }

    [Fact]
    public void Compile_ShouldHandleArrayIndexing()
    {
        var sourcePath = WriteSourceFile("array_index.solid", """
            namespace app;

            func get_first(): i32 {
                var arr = [3]i32{10, 20, 30};
                return arr[0];
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "array_index");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        var irContent = File.ReadAllText(outputPath + ".ll");
        irContent.Should().Contain("getelementptr");
    }

    [Fact]
    public void Compile_ShouldHandleChainedAccess()
    {
        var sourcePath = WriteSourceFile("chained_access.solid", """
            namespace app;

            struct Inner {
                value: i32,
            }

            struct Outer {
                inner: Inner,
            }

            func get_nested(): i32 {
                var o = Outer{inner = Inner{value = 42}};
                return o.inner.value;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Compile_ShouldHandleDefer()
    {
        // Test defer with expression statement (not function call, which has a pre-existing bug)
        var sourcePath = WriteSourceFile("defer.solid", """
            namespace app;

            func test(): i32 {
                var x: i32 = 0;
                defer x + 1;
                defer x + 2;
                return x;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "defer");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        File.Exists(outputPath + ".ll").Should().BeTrue();
    }

    [Fact]
    public void Compile_ShouldHandleDeferWithBlock()
    {
        var sourcePath = WriteSourceFile("defer_block.solid", """
            namespace app;

            func test(): i32 {
                var x: i32 = 0;
                defer {
                    x + 1;
                    x + 2;
                }
                return x;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "defer_block");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        File.Exists(outputPath + ".ll").Should().BeTrue();
    }

    [Fact]
    public void Compile_ShouldHandleDeferWithAssignment()
    {
        var sourcePath = WriteSourceFile("defer_assign.solid", """
            namespace app;

            func test(): i32 {
                var x: i32 = 0;
                defer x = 42;
                return x;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "defer_assign");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        File.Exists(outputPath + ".ll").Should().BeTrue();
    }

    [Fact]
    public void Compile_ShouldHandleRefType()
    {
        var sourcePath = WriteSourceFile("ref_type.solid", """
            namespace app;

            func increment(x: ^!i32) {
            }

            func test(): i32 {
                var value: i32 = 42;
                increment(^!value);
                return value;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "ref_type");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        File.Exists(outputPath + ".ll").Should().BeTrue();
    }

    [Fact]
    public void Compile_ShouldHandleRefExpression()
    {
        var sourcePath = WriteSourceFile("ref_expr.solid", """
            namespace app;

            func test(): i32 {
                var x: i32 = 42;
                var ref: ^i32 = ^x;
                return 0;
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();

        using var codeGen = new CodeGenerator();
        var outputPath = Path.Combine(_tempDir, "ref_expr");
        codeGen.GenerateIr(semanticTree, outputPath + ".ll", CodeGenerator.DefaultTriple);

        File.Exists(outputPath + ".ll").Should().BeTrue();
    }

    [Fact]
    public void Compile_ShouldHandleRefFieldAccess()
    {
        var sourcePath = WriteSourceFile("ref_field.solid", """
            namespace app;

            struct Point {
                x: i32,
                y: i32,
            }

            func get_x(p: ^Point): i32 {
                return 0;
            }

            func test(): i32 {
                var pt = Point{x = 10, y = 20};
                return get_x(^pt);
            }
            """);

        using var analyzer = new SemanticAnalyzer();
        var semanticTree = analyzer.Analyze(File.ReadAllText(sourcePath), sourcePath);

        semanticTree.IsSuccessful.Should().BeTrue();
    }
}
