namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Represents an unresolved generic type parameter (e.g., T inside a generic function body).
/// During monomorphization (Phase 2), these are replaced with concrete types.
/// </summary>
public sealed class TypeParameter : SolidType
{
    public GenericParamSymbol ParamSymbol { get; }

    public override SolidTypeKind Kind => SolidTypeKind.TypeParameter;
    public override string DisplayName => ParamSymbol.Name;

    public TypeParameter(GenericParamSymbol paramSymbol)
    {
        ParamSymbol = paramSymbol;
    }

    public override bool Equals(object? obj) =>
        obj is TypeParameter other && other.ParamSymbol == ParamSymbol;
    public override int GetHashCode() => ParamSymbol.GetHashCode();
}
