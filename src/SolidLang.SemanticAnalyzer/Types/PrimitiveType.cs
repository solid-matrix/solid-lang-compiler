using SolidLang.Parser;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Represents a built-in primitive type: i8..i64, u8..u64, f32, f64, bool, usize, isize.
/// All instances are singletons — use static fields.
/// </summary>
public sealed class PrimitiveType : SolidType
{
    public static readonly PrimitiveType I8 = new("i8");
    public static readonly PrimitiveType I16 = new("i16");
    public static readonly PrimitiveType I32 = new("i32");
    public static readonly PrimitiveType I64 = new("i64");
    public static readonly PrimitiveType ISize = new("isize");
    public static readonly PrimitiveType U8 = new("u8");
    public static readonly PrimitiveType U16 = new("u16");
    public static readonly PrimitiveType U32 = new("u32");
    public static readonly PrimitiveType U64 = new("u64");
    public static readonly PrimitiveType USize = new("usize");
    public static readonly PrimitiveType F32 = new("f32");
    public static readonly PrimitiveType F64 = new("f64");
    public static readonly PrimitiveType Bool = new("bool");

    public override SolidTypeKind Kind => SolidTypeKind.Primitive;
    public string Name { get; }
    public override string DisplayName => Name;

    private PrimitiveType(string name) { Name = name; }

    /// <summary>
    /// Maps a primitive type keyword SyntaxKind to its PrimitiveType singleton.
    /// </summary>
    public static PrimitiveType? FromKeywordKind(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.I8Keyword => I8,
            SyntaxKind.I16Keyword => I16,
            SyntaxKind.I32Keyword => I32,
            SyntaxKind.I64Keyword => I64,
            SyntaxKind.ISizeKeyword => ISize,
            SyntaxKind.U8Keyword => U8,
            SyntaxKind.U16Keyword => U16,
            SyntaxKind.U32Keyword => U32,
            SyntaxKind.U64Keyword => U64,
            SyntaxKind.USizeKeyword => USize,
            SyntaxKind.F32Keyword => F32,
            SyntaxKind.F64Keyword => F64,
            SyntaxKind.BoolKeyword => Bool,
            _ => null,
        };
    }

    public override bool Equals(object? obj) => obj is PrimitiveType other && other.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
}
