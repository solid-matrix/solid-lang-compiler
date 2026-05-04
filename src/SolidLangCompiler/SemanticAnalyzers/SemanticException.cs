using SolidLangCompiler.AST;

namespace SolidLangCompiler.SemanticAnalyzers;

/// <summary>
/// Exception thrown during semantic analysis.
/// </summary>
public class SemanticException : Exception
{
    public SourceLocation Location { get; }

    public SemanticException(string message, SourceLocation location)
        : base($"{location}: {message}")
    {
        Location = location;
    }

    public SemanticException(string message)
        : base(message)
    {
        Location = SourceLocation.Unknown;
    }
}
