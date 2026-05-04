using SolidLangCompiler.AST;
using SolidLangCompiler.AST.Declarations;
using SolidLangCompiler.SemanticAnalyzers.Symbols;

namespace SolidLangCompiler.SemanticAnalyzers;

/// <summary>
/// Represents the semantic tree after analysis.
/// Contains the AST and symbol information.
/// </summary>
public class SemanticTree
{
    /// <summary>
    /// The root program node.
    /// </summary>
    public ProgramNode? Program { get; set; }

    /// <summary>
    /// The global symbol table.
    /// </summary>
    public SymbolTable SymbolTable { get; }

    /// <summary>
    /// List of semantic errors found during analysis.
    /// </summary>
    public List<SemanticError> Errors { get; } = new();

    /// <summary>
    /// List of semantic warnings found during analysis.
    /// </summary>
    public List<SemanticError> Warnings { get; } = new();

    /// <summary>
    /// Whether the semantic analysis was successful.
    /// </summary>
    public bool IsSuccessful => Errors.Count == 0;

    public SemanticTree()
    {
        SymbolTable = new SymbolTable();
    }

    public SemanticTree(ProgramNode program)
    {
        Program = program;
        SymbolTable = new SymbolTable();
    }
}

/// <summary>
/// Represents a semantic error or warning.
/// </summary>
public record SemanticError(string Message, SourceLocation Location, bool IsWarning = false)
{
    public override string ToString()
    {
        var prefix = IsWarning ? "warning" : "error";
        return $"{Location}: {prefix}: {Message}";
    }
}
