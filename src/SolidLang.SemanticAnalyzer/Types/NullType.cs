namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Singleton sentinel for the null literal type.
/// </summary>
public sealed class NullType : SolidType
{
    public static readonly NullType Instance = new();

    public override SolidTypeKind Kind => SolidTypeKind.Null;
    public override string DisplayName => "null";

    private NullType() { }

    public override bool Equals(object? obj) => obj is NullType;
    public override int GetHashCode() => 1;
}
