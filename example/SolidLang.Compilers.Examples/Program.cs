using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using LLVMSharp;
using LLVMSharp.Interop;
using SolidLang.Compilers;
using System.Diagnostics;
using System.Runtime.InteropServices;



namespace SolidLang.Compilers.Examples;

class Program
{
    static void Main(string[] args)
    {
        // Initialize LLVM
        LLVM.InitializeNativeTarget();
        LLVM.InitializeNativeAsmPrinter();

        var sourceFile = args.Length > 0 ? args[0] : "example1.solid";

        var sourcePath = Path.GetFullPath(sourceFile);
        if (!File.Exists(sourcePath))
        {
            sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, sourceFile);
        }

        if (!File.Exists(sourcePath))
        {
            Console.WriteLine($"Source file not found: {sourceFile}");
            Console.WriteLine($"  Tried: {Path.GetFullPath(sourceFile)}");
            Console.WriteLine($"  Tried: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, sourceFile)}");
            return;
        }

        var sourceCode = File.ReadAllText(sourcePath);
        Console.WriteLine("=== Source Code ===");
        Console.WriteLine(sourceCode);
        Console.WriteLine();

        var baseOutputPath = Path.ChangeExtension(sourcePath, null);
        var irOutputPath = baseOutputPath + ".ll";
        var objectOutputPath = baseOutputPath + ".o";
        var exeOutputPath = baseOutputPath + ".exe";

        try
        {
            Compiler.Compile(sourceCode, irOutputPath);
            Console.WriteLine($"IR compilation successful! Output: {irOutputPath}");
            Console.WriteLine();

            var ir = File.ReadAllText(irOutputPath);
            Console.WriteLine("=== Generated LLVM IR ===");
            Console.WriteLine(ir);
            Console.WriteLine();

            Compiler.CompileToObjectFile(sourceCode, objectOutputPath);
            Console.WriteLine($"Object file compilation successful! Output: {objectOutputPath}");
            Console.WriteLine();

            LinkExecutable(objectOutputPath, exeOutputPath);
            Console.WriteLine($"Executable created: {exeOutputPath}");
            Console.WriteLine();

            Console.WriteLine("=== Running executable ===");
            RunExecutable(exeOutputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Compilation failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static void LinkExecutable(string objectFile, string outputFile)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var clangPath = FindClang();
            if (clangPath != null)
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = clangPath,
                        Arguments = $"\"{objectFile}\" -o \"{outputFile}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Linking failed: {error}\n{output}");
                }
            }
            else
            {
                throw new InvalidOperationException("Could not find clang.exe for linking. Please install LLVM.");
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                 RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var clangPath = FindExecutable("clang");
            if (clangPath != null)
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = clangPath,
                        Arguments = $"\"{objectFile}\" -o \"{outputFile}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Linking failed: {error}\n{output}");
                }
            }
            else
            {
                throw new InvalidOperationException("Could not find clang for linking.");
            }
        }
    }

    static string? FindClang()
    {
        // First check common LLVM installation paths on Windows
        var commonPaths = new[]
        {
            @"C:\Program Files\LLVM\bin\clang.exe",
            @"C:\Program Files (x86)\LLVM\bin\clang.exe",
        };

        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
                return path;
        }

        // Then check PATH
        return FindExecutable("clang.exe");
    }

    static string? FindExecutable(string name)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv == null) return null;

        var paths = pathEnv.Split(Path.PathSeparator);
        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path, name);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }

    static void RunExecutable(string exePath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(output))
            Console.WriteLine(output);
        if (!string.IsNullOrEmpty(error))
            Console.WriteLine($"Error: {error}");

        Console.WriteLine($"Exit code: {process.ExitCode}");
    }
}
