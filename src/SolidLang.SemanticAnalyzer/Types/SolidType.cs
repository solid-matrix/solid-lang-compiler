namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Abstract base for all resolved semantic types.
/// SolidType instances are immutable value objects — equality is structural.
/// </summary>
public abstract class SolidType
{
    public abstract SolidTypeKind Kind { get; }
    public abstract string DisplayName { get; }

    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();

    public override string ToString() => DisplayName;
}
