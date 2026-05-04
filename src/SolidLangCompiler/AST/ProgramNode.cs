using SolidLangCompiler.AST.Declarations;

namespace SolidLangCompiler.AST;

/// <summary>
/// Represents a complete program (compilation unit).
/// </summary>
public record ProgramNode(
    NamespaceDeclarationNode? Namespace,
    IReadOnlyList<UsingDeclarationNode>? Usings,
    IReadOnlyList<DeclarationNode>? Declarations
) : AstNode
{
    public override string ToString()
    {
        var lines = new List<string>();

        if (Namespace != null)
            lines.Add(Namespace.ToString()!);

        if (Usings != null)
            foreach (var usingDecl in Usings)
                lines.Add(usingDecl.ToString()!);

        if (Declarations != null)
            foreach (var decl in Declarations)
                lines.Add(decl.ToString()!);

        return string.Join("\n", lines);
    }
}
