using SolidLang.Parser.Nodes;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Represents a function declaration.
/// </summary>
public sealed class FunctionSymbol : Symbol
{
    public override SymbolKind Kind => SymbolKind.Function;
    public override string Name { get; }
    public override SyntaxNode Declaration { get; }

    public bool IsForwardDecl { get; internal set; }
    public bool IsIntrinsic { get; internal set; }
    public IReadOnlyList<GenericParamSymbol> GenericParams { get; }
    public IReadOnlyList<VariableSymbol> Parameters { get; internal set; }
    public SolidType? ReturnType { get; }           // null = void return
    public string? CallingConvention { get; }       // "cdecl", "stdcall", or null for default
    public Scope? BodyScope { get; internal set; }  // function body scope (for locals)
    public string? ImportName { get; internal set; } // @import(name) linker symbol, null = use Name

    public FunctionSymbol(string name, SyntaxNode declaration, bool isForwardDecl,
        IReadOnlyList<GenericParamSymbol>? genericParams = null,
        IReadOnlyList<VariableSymbol>? parameters = null,
        SolidType? returnType = null,
        string? callingConvention = null,
        bool isIntrinsic = false)
    {
        Name = name;
        Declaration = declaration;
        IsForwardDecl = isForwardDecl;
        GenericParams = genericParams ?? Array.Empty<GenericParamSymbol>();
        Parameters = parameters ?? Array.Empty<VariableSymbol>();
        ReturnType = returnType;
        CallingConvention = callingConvention;
        IsIntrinsic = isIntrinsic;
    }
}
