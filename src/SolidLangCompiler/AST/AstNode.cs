namespace SolidLangCompiler.AST;

/// <summary>
/// Base class for all AST nodes.
/// </summary>
public abstract record AstNode
{
    /// <summary>
    /// The source location of this node.
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
