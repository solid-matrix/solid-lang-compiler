namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Represents a pointer type: *T (immutable) or *!T (mutable).
/// </summary>
public sealed class PointerType : SolidType
{
    public SolidType PointeeType { get; }
    public bool IsMutable { get; }

    public override SolidTypeKind Kind => SolidTypeKind.Pointer;
    public override string DisplayName => IsMutable ? $"*!{PointeeType.DisplayName}" : $"*{PointeeType.DisplayName}";

    public PointerType(SolidType pointeeType, bool isMutable = false)
    {
        PointeeType = pointeeType;
        IsMutable = isMutable;
    }

    public override bool Equals(object? obj) =>
        obj is PointerType other && other.IsMutable == IsMutable && other.PointeeType.Equals(PointeeType);
    public override int GetHashCode() => HashCode.Combine(PointeeType, IsMutable);
}
