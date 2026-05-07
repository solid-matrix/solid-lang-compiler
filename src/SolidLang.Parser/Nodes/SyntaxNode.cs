using System.Text;

namespace SolidLang.Parser.Nodes;

/// <summary>
/// Base class for all syntax nodes in the AST.
/// </summary>
public abstract class SyntaxNode
{
    /// <summary>
    /// Gets the kind of this syntax node.
    /// </summary>
    public abstract SyntaxKind Kind { get; }

    /// <summary>
    /// Gets the span of this node in the source text.
    /// </summary>
    public abstract TextSpan Span { get; }

    /// <summary>
    /// Gets the children of this node.
    /// </summary>
    public abstract IEnumerable<SyntaxNode> GetChildren();

    /// <summary>
    /// Gets the full text of this node.
    /// </summary>
    public abstract string GetFullText();

    /// <summary>
    /// Writes this node and its children to the specified writer.
    /// </summary>
    public virtual void WriteTo(TextWriter writer, string indent = "", bool isLast = true)
    {
        var marker = isLast ? "└──" : "├──";
        writer.Write(indent);
        writer.Write(marker);
        writer.Write(Kind);

        WriteAdditionalInfo(writer);
        writer.WriteLine();

        var children = GetChildren().ToList();
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var childIsLast = i == children.Count - 1;
            var childIndent = indent + (isLast ? "   " : "│  ");
            child.WriteTo(writer, childIndent, childIsLast);
        }
    }

    /// <summary>
    /// Override to write additional information about this node.
    /// </summary>
    protected virtual void WriteAdditionalInfo(TextWriter writer)
    {
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        WriteTo(writer);
        return sb.ToString();
    }
}
