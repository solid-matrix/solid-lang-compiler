namespace SolidLang.Compilers.Types;

public abstract record SolidType(string Name);

public sealed record I32Type() : SolidType("i32");

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

