using System.Diagnostics;
using LLVMSharp.Interop;
using SolidLang.Parser;
using SolidLang.Parser.Nodes.Declarations;
using SolidLang.Parser.Parser;
using SolidLang.SemanticAnalyzer;
using SolidLang.IrGenerator;
using SolidParser = SolidLang.Parser.Parser.Parser;

namespace SolidLang.Compiler.UnitTests;

public class CompilerTests
{
    private static string CasesDir =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..",
            "test", "SolidLang.Parser.UnitTests", "cases");

    private static (ProgramNode program, bool hasParseErrors, IReadOnlyList<Diagnostic> diagnostics) ParseFile(string filename)
    {
        var path = Path.Combine(CasesDir, filename);
        var source = SourceText.From(File.ReadAllText(path));
        var parser = new SolidParser(source);
        var program = parser.ParseProgram();
        return (program, parser.HasErrors, parser.Diagnostics);
    }

    /// <summary>
    /// Compile a .solid file through parse → semantic → IR and return the LLVM IR text.
    /// </summary>
    private static (string? ir, int semanticErrors, string? error) CompileToIR(string filename)
    {
        var (program, hasParseErrors, diagnostics) = ParseFile(filename);
        if (hasParseErrors)
            return (null, 0, $"Parse errors: {diagnostics.FirstOrDefault()?.ToString() ?? "Unknown"}");

        var semanticModel = SemanticModel.Create(new[] { program });
        var semanticErrors = semanticModel.GetDiagnostics().Count;
        if (semanticErrors > 0)
            return (null, semanticErrors, null);

        var codegen = new CodeGenerator();
        using var module = codegen.Generate(semanticModel.BoundProgram);
        return (module.PrintToString(), 0, null);
    }

    // ========================================
    // LLVM IR snapshot tests
    // ========================================

    /// <summary>
    /// Files that pass both parse and semantic analysis — should produce valid IR.
    /// </summary>
    public static IEnumerable<object[]> CompilableSolidFiles =>
        new List<object[]>
        {
            new object[] { "1-main.solid" },
            new object[] { "12-if.solid" },
            new object[] { "18-integer-literal.solid" },
            new object[] { "19-float-literal.solid" },
            new object[] { "20-string-char.solid" },
            new object[] { "28-ternary.solid" },
            new object[] { "29-compound-assign.solid" },
            new object[] { "33-while.solid" },
            new object[] { "42-int-literals.solid" },
        };

    [Theory]
    [MemberData(nameof(CompilableSolidFiles))]
    public void File_CompilesToValidLLVMIR(string filename)
    {
        var (ir, semanticErrors, error) = CompileToIR(filename);

        if (error != null)
            Assert.Fail($"{filename}: {error}");
        if (semanticErrors > 0)
            Assert.Fail($"{filename}: {semanticErrors} semantic errors");

        Assert.NotNull(ir);
        Assert.Contains("define", ir);       // has at least one function
        Assert.Contains("ModuleID", ir);     // has module header
        Assert.DoesNotContain("i64 0", ir);  // no type mismatch (the bug we fixed)
    }

    /// <summary>
    /// Verify specific properties of the generated IR for 1-main.solid.
    /// </summary>
    [Fact]
    public void Main_IR_HasCorrectStructure()
    {
        var (ir, semanticErrors, error) = CompileToIR("1-main.solid");

        Assert.Null(error);
        Assert.Equal(0, semanticErrors);
        Assert.NotNull(ir);

        Assert.Contains("define i32 @main()", ir);
        Assert.Contains("ret i32 0", ir);
        Assert.Contains("entry:", ir);
    }

    [Fact]
    public void If_IR_GeneratesBranchingStructure()
    {
        var (ir, semanticErrors, error) = CompileToIR("12-if.solid");

        Assert.Null(error);
        Assert.Equal(0, semanticErrors);
        Assert.NotNull(ir);

        Assert.Contains("then:", ir);
        Assert.Contains("if_merge:", ir);
    }

    [Fact]
    public void While_IR_GeneratesLoopStructure()
    {
        var (ir, semanticErrors, error) = CompileToIR("33-while.solid");

        Assert.Null(error);
        Assert.Equal(0, semanticErrors);
        Assert.NotNull(ir);

        Assert.Contains("while_cond:", ir);
        Assert.Contains("while_body:", ir);
        Assert.Contains("while_merge:", ir);
    }

    // ========================================
    // End-to-end executable test
    // ========================================

    [Fact]
    public void CompileAndRun_Main_ExitCodeZero()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "solid_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var inputFile = Path.Combine(CasesDir, "1-main.solid");
            var outputExe = Path.Combine(tempDir, "test.exe");

            // Compile to IR
            var source = SourceText.From(File.ReadAllText(inputFile));
            var parser = new SolidParser(source);
            var program = parser.ParseProgram();
            Assert.False(parser.HasErrors);

            var semanticModel = SemanticModel.Create(new[] { program });
            Assert.Empty(semanticModel.GetDiagnostics());

            var codegen = new CodeGenerator();
            using var module = codegen.Generate(semanticModel.BoundProgram);

            // Emit object file
            var objPath = Path.Combine(tempDir, "test.obj");
            codegen.EmitObjectFile(objPath);
            Assert.True(File.Exists(objPath));

            // Link with clang
            var psi = new ProcessStartInfo("clang", $"\"{objPath}\" -o \"{outputExe}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi)!;
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                var stderr = process.StandardError.ReadToEnd();
                Assert.Fail($"Clang link failed (exit {process.ExitCode}): {stderr}");
            }

            Assert.True(File.Exists(outputExe));

            // Run the executable
            var runPsi = new ProcessStartInfo(outputExe)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var runProcess = Process.Start(runPsi)!;
            runProcess.WaitForExit();
            Assert.Equal(0, runProcess.ExitCode);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
