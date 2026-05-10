using SolidLang.Parser.Nodes.Declarations;
using SolidLang.Parser.Parser;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Public API for semantic analysis. Takes parsed ASTs and produces a bound semantic
/// model with resolved names, types, and scopes, ready for IR generation.
/// </summary>
public sealed class SemanticModel
{
    public BoundProgram BoundProgram { get; }
    public Scope GlobalScope { get; }
    public DiagnosticBag Diagnostics { get; }

    public IReadOnlyList<Diagnostic> GetDiagnostics() => Diagnostics.Diagnostics;
    public bool HasErrors => Diagnostics.HasErrors;

    private SemanticModel(BoundProgram boundProgram, Scope globalScope, DiagnosticBag diagnostics)
    {
        BoundProgram = boundProgram;
        GlobalScope = globalScope;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// Parse and bind all programs. Creates a fresh DiagnosticBag internally.
    /// </summary>
    public static SemanticModel Create(IReadOnlyList<ProgramNode> programs)
    {
        return Create(programs, new DiagnosticBag());
    }

    /// <summary>
    /// Parse and bind all programs, appending diagnostics to the provided bag.
    /// </summary>
    public static SemanticModel Create(IReadOnlyList<ProgramNode> programs, DiagnosticBag diagnostics)
    {
        var binder = new Binder();
        var boundProgram = binder.Bind(programs, diagnostics);
        return new SemanticModel(boundProgram, boundProgram.GlobalScope, diagnostics);
    }
}
