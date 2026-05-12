using SolidLang.Parser.Nodes.Declarations;
using SolidLang.Parser.Parser;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Orchestrates the two-pass semantic binding process:
/// Pass 1 (SymbolBuilderPass): collects all symbols and builds the scope hierarchy.
/// Pass 2 (BoundTreeBuilder): builds the BoundNode tree with resolved names and types.
/// </summary>
public sealed class Binder
{
    /// <summary>
    /// Bind all programs into a single BoundProgram.
    /// </summary>
    public BoundProgram Bind(IReadOnlyList<ProgramNode> programs, DiagnosticBag diagnostics)
    {
        // Pass 1: Build the symbol table across all files
        var pass1 = new SymbolBuilderPass(diagnostics);
        var (globalScope, namespaces, scopeMap) = pass1.Run(programs);

        // Pass 2: Build the BoundNode tree with resolved names/types
        var pass2 = new BoundTreeBuilder(globalScope, namespaces, scopeMap, diagnostics);
        return pass2.Build(programs);
    }
}
