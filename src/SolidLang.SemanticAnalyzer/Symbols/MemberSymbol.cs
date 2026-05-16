using SolidLang.Parser.Nodes;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Represents a member declared inside a type: field, enum value, variant arm, or interface method.
/// </summary>
public sealed class MemberSymbol : Symbol
{
    public override SymbolKind Kind { get; }
    public override string Name { get; }
    public override SyntaxNode Declaration { get; internal set; }

    /// <summary>
    /// The type of this member: field type, enum discriminant type, variant payload type,
    /// or interface method return type.
    /// </summary>
    public SolidType? MemberType { get; internal set; }

    public bool IsField => Kind is SymbolKind.StructField or SymbolKind.UnionField or
                                  SymbolKind.EnumField or SymbolKind.VariantField;
    public bool IsMethod => Kind == SymbolKind.InterfaceMethod;

    public MemberSymbol(SymbolKind kind, string name, SyntaxNode declaration, SolidType? memberType = null)
    {
        Kind = kind;
        Name = name;
        Declaration = declaration;
        MemberType = memberType;
    }
}
