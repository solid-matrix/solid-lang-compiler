namespace SolidLangCompiler.SemanticTree;

/// <summary>
/// Base class for all SemanticTree nodes.
/// SemanticTree is a lowered, monomorphized representation ready for code generation.
/// </summary>
public abstract record SemaNode
{
    /// <summary>
    /// Source location for error reporting.
    /// </summary>
    public SourceLocation Location { get; init; } = SourceLocation.Unknown;
}

/// <summary>
/// Represents a location in source code.
/// </summary>
public record SourceLocation(string FilePath, int Line, int Column)
{
    public static SourceLocation Unknown { get; } = new("<unknown>", 0, 0);
    public override string ToString() => $"{FilePath}:{Line}:{Column}";
}
