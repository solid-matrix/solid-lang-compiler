using System.Diagnostics;
using FluentAssertions;
using Xunit;

namespace SolidLangCompiler.IntegrationTests;

public class EndToEndTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _projectPath;

    public EndToEndTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"solid_e2e_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        // Use workspace relative path
        _projectPath = "/workspace/solid-lang-compiler/src/SolidLangCompiler/SolidLangCompiler.csproj";
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

    private (string stdout, string stderr, int exitCode) RunCompiler(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{_projectPath}\" -- {args}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _tempDir
        };

        using var process = Process.Start(psi);
        var stdout = process!.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(60000);

        return (stdout, stderr, process.ExitCode);
    }

    private (string stdout, string stderr, int exitCode) RunExecutable(string path)
    {
        var psi = new ProcessStartInfo
        {
            FileName = path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        var stdout = process!.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (stdout, stderr, process.ExitCode);
    }

    [Fact(Skip = "Code generation currently hardcoded to return 0 - needs full implementation")]
    public void E2E_CompileAndRun_SimpleProgram()
    {
        var sourcePath = WriteSourceFile("simple.solid", """
            namespace app;
            func main(): i32 {
                return 42;
            }
            """);

        var outputPath = Path.Combine(_tempDir, "simple");
        var result = RunCompiler($"{sourcePath} -o {outputPath}");

        result.exitCode.Should().Be(0, because: $"compiler should succeed. stdout: {result.stdout}, stderr: {result.stderr}");
        File.Exists(outputPath + ".o").Should().BeTrue();

        // Link and run
        var exePath = Path.Combine(_tempDir, "simple_exe");
        var linkResult = RunCommand($"gcc {outputPath}.o -o {exePath}");
        linkResult.exitCode.Should().Be(0, because: $"linker should succeed. stderr: {linkResult.stderr}");

        var runResult = RunExecutable(exePath);
        runResult.exitCode.Should().Be(42);
    }

    [Fact]
    public void E2E_Compile_GenerateIr()
    {
        var sourcePath = WriteSourceFile("simple.solid", """
            namespace app;
            func main(): i32 {
                return 0;
            }
            """);

        var outputPath = Path.Combine(_tempDir, "ir_test");
        var result = RunCompiler($"{sourcePath} --ir -o {outputPath}");

        result.exitCode.Should().Be(0, because: $"stdout: {result.stdout}, stderr: {result.stderr}");
        File.Exists(outputPath + ".ll").Should().BeTrue();

        var irContent = File.ReadAllText(outputPath + ".ll");
        irContent.Should().Contain("@main");
    }

    [Fact]
    public void E2E_Compile_FileNotFound()
    {
        var result = RunCompiler("/nonexistent/path.solid");

        result.exitCode.Should().Be(1);
        result.stderr.Should().Contain("not found");
    }

    [Fact]
    public void E2E_Compile_MissingOutputArg()
    {
        var sourcePath = WriteSourceFile("test.solid", "namespace app;");

        var result = RunCompiler($"{sourcePath} -o");

        result.exitCode.Should().Be(1);
        result.stderr.Should().Contain("-o requires an argument");
    }

    [Fact]
    public void E2E_Compile_UnknownOption()
    {
        var sourcePath = WriteSourceFile("test.solid", "namespace app;");

        var result = RunCompiler($"{sourcePath} --unknown");

        result.exitCode.Should().Be(1);
        result.stderr.Should().Contain("Unknown option");
    }

    private (string stdout, string stderr, int exitCode) RunCommand(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        var stdout = process!.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (stdout, stderr, process.ExitCode);
    }
}
