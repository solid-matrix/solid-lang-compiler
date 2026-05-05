namespace SolidLangCompiler.SemanticTree;

/// <summary>
/// Integer type kinds (matches AST IntegerKind for compatibility).
/// </summary>
public enum SemaIntegerKind
{
    I8, I16, I32, I64, ISize,
    U8, U16, U32, U64, USize
}

/// <summary>
/// Floating-point type kinds.
/// </summary>
public enum SemaFloatKind
{
    F32, F64
}

/// <summary>
/// Base class for all semantic types.
/// These are concrete, monomorphized types ready for code generation.
/// </summary>
public abstract record SemaType
{
    /// <summary>
    /// Gets the size in bytes for this type.
    /// </summary>
    public abstract int SizeBytes { get; }
}

/// <summary>
/// Represents an integer type (i8, i16, i32, i64, isize, u8, u16, u32, u64, usize).
/// </summary>
public record SemaIntType(SemaIntegerKind Kind) : SemaType
{
    public override int SizeBytes => Kind switch
    {
        SemaIntegerKind.I8 or SemaIntegerKind.U8 => 1,
        SemaIntegerKind.I16 or SemaIntegerKind.U16 => 2,
        SemaIntegerKind.I32 or SemaIntegerKind.U32 => 4,
        SemaIntegerKind.I64 or SemaIntegerKind.U64 => 8,
        SemaIntegerKind.ISize or SemaIntegerKind.USize => 8, // 64-bit platform
        _ => throw new InvalidOperationException()
    };

    public bool IsSigned => Kind is SemaIntegerKind.I8 or SemaIntegerKind.I16 or
        SemaIntegerKind.I32 or SemaIntegerKind.I64 or SemaIntegerKind.ISize;

    public override string ToString() => Kind switch
    {
        SemaIntegerKind.I8 => "i8",
        SemaIntegerKind.I16 => "i16",
        SemaIntegerKind.I32 => "i32",
        SemaIntegerKind.I64 => "i64",
        SemaIntegerKind.ISize => "isize",
        SemaIntegerKind.U8 => "u8",
        SemaIntegerKind.U16 => "u16",
        SemaIntegerKind.U32 => "u32",
        SemaIntegerKind.U64 => "u64",
        SemaIntegerKind.USize => "usize",
        _ => throw new InvalidOperationException()
    };
}

/// <summary>
/// Represents a floating-point type (f32, f64).
/// </summary>
public record SemaFloatType(SemaFloatKind Kind) : SemaType
{
    public override int SizeBytes => Kind == SemaFloatKind.F32 ? 4 : 8;

    public override string ToString() => Kind == SemaFloatKind.F32 ? "f32" : "f64";
}

/// <summary>
/// Represents the boolean type.
/// </summary>
public record SemaBoolType() : SemaType
{
    public override int SizeBytes => 1;
    public override string ToString() => "bool";
}

/// <summary>
/// Represents the void type (for functions with no return value).
/// </summary>
public record SemaVoidType() : SemaType
{
    public override int SizeBytes => 0;
    public override string ToString() => "void";
}

/// <summary>
/// Represents a pointer type *T.
/// </summary>
public record SemaPointerType(SemaType TargetType) : SemaType
{
    public override int SizeBytes => 8; // 64-bit platform
    public override string ToString() => $"*{TargetType}";
}

/// <summary>
/// Represents a reference type ^T or ^!T.
/// </summary>
public record SemaRefType(SemaType TargetType, bool IsMutable) : SemaType
{
    public override int SizeBytes => 8; // 64-bit platform
    public override string ToString() => IsMutable ? $"^!{TargetType}" : $"^{TargetType}";
}

/// <summary>
/// Represents an array type [N]T.
/// </summary>
public record SemaArrayType(SemaType ElementType, ulong Size) : SemaType
{
    public override int SizeBytes => (int)Size * ElementType.SizeBytes;
    public override string ToString() => $"[{Size}]{ElementType}";
}

/// <summary>
/// Represents a tuple type (T1, T2, ...).
/// </summary>
public record SemaTupleType(IReadOnlyList<SemaType> Elements) : SemaType
{
    public override int SizeBytes => Elements.Sum(e => e.SizeBytes);
    public override string ToString() => $"({string.Join(", ", Elements)})";
}

/// <summary>
/// Represents a function type func(T1, T2, ...) -> R.
/// </summary>
public record SemaFuncType(IReadOnlyList<SemaType> ParameterTypes, SemaType? ReturnType) : SemaType
{
    public override int SizeBytes => 8; // Function pointer
    public override string ToString()
    {
        var paramsStr = string.Join(", ", ParameterTypes);
        var returnStr = ReturnType?.ToString() ?? "void";
        return $"func({paramsStr}) -> {returnStr}";
    }
}

/// <summary>
/// Represents a named struct type.
/// </summary>
public record SemaStructType(string Name, IReadOnlyList<SemaStructField> Fields) : SemaType
{
    public override int SizeBytes => Fields.Sum(f => f.Type.SizeBytes);
    public override string ToString() => Name;
}

/// <summary>
/// Represents a field in a struct.
/// </summary>
public record SemaStructField(string Name, SemaType Type, int Offset);

/// <summary>
/// Represents a named type reference (resolved to actual struct/enum/union).
/// </summary>
public record SemaNamedType(string FullyQualifiedName, SemaType? UnderlyingType) : SemaType
{
    public override int SizeBytes => UnderlyingType?.SizeBytes ?? 0;
    public override string ToString() => FullyQualifiedName;
}

/// <summary>
/// Represents a named union type.
/// Union size is determined by the largest field.
/// </summary>
public record SemaUnionType(string Name, IReadOnlyList<SemaUnionField> Fields) : SemaType
{
    public override int SizeBytes => Fields.Count > 0 ? Fields.Max(f => f.Type.SizeBytes) : 0;
    public override string ToString() => Name;
}

/// <summary>
/// Represents a field in a union.
/// All fields share the same memory location (offset 0).
/// </summary>
public record SemaUnionField(string Name, SemaType Type);
