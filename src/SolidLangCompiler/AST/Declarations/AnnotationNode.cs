using SolidLangCompiler.AST.Expressions;
using SolidLangCompiler.AST.Types;

namespace SolidLangCompiler.AST.Declarations;

/// <summary>
/// Base class for all declaration nodes.
/// </summary>
public abstract record DeclarationNode : AstNode;

/// <summary>
/// Represents a compile-time annotation (@name or @name(args)).
/// </summary>
public record AnnotationNode(string Name, IReadOnlyList<AnnotationArgumentNode>? Arguments = null)
{
    public override string ToString()
    {
        if (Arguments != null)
        {
            var args = string.Join(", ", Arguments);
            return $"@{Name}({args})";
        }
        return $"@{Name}";
    }
}

/// <summary>
/// Represents an argument in an annotation.
/// </summary>
public record AnnotationArgumentNode
{
    public TypeNode? Type { get; init; }
    public ExpressionNode? Expression { get; init; }
    public string? Identifier { get; init; }

    public override string ToString()
    {
        if (Type != null) return Type.ToString()!;
        if (Expression != null) return Expression.ToString()!;
        if (Identifier != null) return Identifier;
        return "";
    }
}
