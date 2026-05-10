namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Abstract base for all bound (semantic) tree nodes.
/// BoundNodes carry resolved semantic information — names are Symbols, types are SolidTypes.
/// Unlike SyntaxNodes, BoundNodes do NOT carry source text or spans.
/// </summary>
public abstract class BoundNode
{
    public abstract BoundKind Kind { get; }

    /// <summary>
    /// The resolved type of this node, if it represents a typed expression.
    /// </summary>
    public virtual SolidType? Type => null;
}
