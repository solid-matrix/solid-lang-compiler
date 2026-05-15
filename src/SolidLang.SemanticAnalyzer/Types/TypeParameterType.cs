namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Represents a generic type parameter (e.g., T in struct Vector&lt;T&gt;).
/// </summary>
public sealed class TypeParameterType : SolidType
{
    public string Name { get; }

    public override SolidTypeKind Kind => SolidTypeKind.TypeParameter;
    public override string DisplayName => Name;

    public TypeParameterType(string name) { Name = name; }

    public override bool Equals(object? obj) => obj is TypeParameterType other && other.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
}
