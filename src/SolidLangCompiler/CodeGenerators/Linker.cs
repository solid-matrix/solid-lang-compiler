namespace SolidLangCompiler.CodeGenerators;

/// <summary>
/// Handles linking object files to executables or shared libraries.
/// </summary>
public static class Linker
{
    /// <summary>
    /// Links an object file into an executable.
    /// </summary>
    public static bool LinkExecutable(string objectPath, string outputPath, out string error)
    {
        error = string.Empty;

        var linker = FindLinker();
        if (linker == null)
        {
            error = "Error: No linker found (gcc or clang required)";
            return false;
        }

        var args = new List<string>
        {
            objectPath,
            "-o", outputPath
        };

        return RunCommand(linker, args, out error);
    }

    /// <summary>
    /// Links an object file into a shared library.
    /// </summary>
    public static bool LinkSharedLibrary(string objectPath, string outputPath, out string error)
    {
        error = string.Empty;

        var linker = FindLinker();
        if (linker == null)
        {
            error = "Error: No linker found (gcc or clang required)";
            return false;
        }

        var args = new List<string>
        {
            "-shared",
            "-fPIC",
            objectPath,
            "-o", outputPath
        };

        return RunCommand(linker, args, out error);
    }

    private static string? FindLinker()
    {
        // Prefer clang over gcc
        if (CommandExists("clang"))
            return "clang";

        if (CommandExists("gcc"))
            return "gcc";

        return null;
    }

    private static bool CommandExists(string command)
    {
        try
        {
            var whichResult = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "which",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            whichResult?.WaitForExit();
            return whichResult?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool RunCommand(string command, IEnumerable<string> args, out string error)
    {
        error = string.Empty;

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = string.Join(" ", args),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                error = $"Error: Failed to start linker: {command}";
                return false;
            }

            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                error = $"Linker error:\n{stderr}";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = $"Error running linker: {ex.Message}";
            return false;
        }
    }
}
