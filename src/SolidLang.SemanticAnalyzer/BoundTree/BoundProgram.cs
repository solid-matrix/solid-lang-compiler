namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// The root of the bound (semantic) tree. Contains all declarations
/// from all source files in the project, merged into a unified scope.
/// </summary>
public sealed class BoundProgram : BoundNode
{
    public override BoundKind Kind => BoundKind.Program;

    /// <summary>
    /// All top-level declarations (types, functions, variables) across all files.
    /// </summary>
    public IReadOnlyList<BoundDeclaration> Declarations { get; }

    /// <summary>
    /// The global scope containing all top-level symbols.
    /// </summary>
    public Scope GlobalScope { get; }

    public BoundProgram(IReadOnlyList<BoundDeclaration> declarations, Scope globalScope)
    {
        Declarations = declarations;
        GlobalScope = globalScope;
    }
}
