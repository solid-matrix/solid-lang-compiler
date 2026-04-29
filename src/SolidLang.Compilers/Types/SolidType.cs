namespace SolidLang.Compilers.Types;

public abstract record SolidType(string Name);

// Signed integer types
public sealed record I8Type() : SolidType("i8");
public sealed record I16Type() : SolidType("i16");
public sealed record I32Type() : SolidType("i32");
public sealed record I64Type() : SolidType("i64");
public sealed record I128Type() : SolidType("i128");
public sealed record IsizeType() : SolidType("isize");

// Unsigned integer types
public sealed record U8Type() : SolidType("u8");
public sealed record U16Type() : SolidType("u16");
public sealed record U32Type() : SolidType("u32");
public sealed record U64Type() : SolidType("u64");
public sealed record U128Type() : SolidType("u128");
public sealed record UsizeType() : SolidType("usize");

// Floating-point types
public sealed record F16Type() : SolidType("f16");
public sealed record F32Type() : SolidType("f32");
public sealed record F64Type() : SolidType("f64");
public sealed record F128Type() : SolidType("f128");

// Other types
public sealed record VoidType() : SolidType("void");
public sealed record BoolType() : SolidType("bool");

public sealed record StructType(
    string Name,
    IReadOnlyList<(string Name, SolidType Type)> Fields
) : SolidType(Name);

public sealed record UnionType(
    string Name,
    IReadOnlyList<(string Name, SolidType Type)> Fields
) : SolidType(Name);

public sealed record EnumType(
    string Name,
    SolidType UnderlyingType,
    IReadOnlyList<(string Name, long Value)> Fields
) : SolidType(Name);

public sealed record TupleType(
    IReadOnlyList<SolidType> Elements
) : SolidType($"({string.Join(", ", Elements.Select(e => e.Name))})");

public sealed record PointerType(
    SolidType ElementType,
    bool IsMutable
) : SolidType($"{(IsMutable ? "" : "!")}{ElementType.Name}");

