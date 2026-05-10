namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Represents an array type: [N]T or []T (unsized).
/// </summary>
public sealed class ArrayType : SolidType
{
    public SolidType ElementType { get; }
    public int? Size { get; }  // null = unsized array

    public override SolidTypeKind Kind => SolidTypeKind.Array;
    public override string DisplayName =>
        Size.HasValue ? $"[{Size}]{ElementType.DisplayName}" : $"[]{ElementType.DisplayName}";

    public ArrayType(SolidType elementType, int? size = null)
    {
        ElementType = elementType;
        Size = size;
    }

    public override bool Equals(object? obj) =>
        obj is ArrayType other && other.Size == Size && other.ElementType.Equals(ElementType);
    public override int GetHashCode() => HashCode.Combine(ElementType, Size);
}
