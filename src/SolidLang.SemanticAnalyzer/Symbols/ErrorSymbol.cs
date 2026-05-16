using SolidLang.Parser.Nodes;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Sentinel symbol for error recovery. Returned when name resolution fails.
/// </summary>
public sealed class ErrorSymbol : Symbol
{
    public static readonly ErrorSymbol Instance = new();

    public override SymbolKind Kind => SymbolKind.Error;
    public override string Name => "?error?";
    public override SyntaxNode? Declaration { get; internal set; } = null;

    private ErrorSymbol() { }
}
