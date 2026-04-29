namespace SolidLang.Compilers.Symbols;

public abstract record Symbol(string Name);

public sealed record VariableSymbol(string Name, Types.SolidType Type) : Symbol(Name);

public sealed record FunctionSymbol(
    string Name,
    Types.SolidType ReturnType,
    IReadOnlyList<VariableSymbol> Parameters
) : Symbol(Name);

public sealed record StructSymbol(
    string Name,
    IReadOnlyList<(string Name, Types.SolidType Type)> Fields
) : Symbol(Name);

public sealed record UnionSymbol(
    string Name,
    IReadOnlyList<(string Name, Types.SolidType Type)> Fields
) : Symbol(Name);

public sealed record EnumSymbol(
    string Name,
    Types.SolidType UnderlyingType,
    IReadOnlyList<(string Name, long Value)> Fields
) : Symbol(Name);

// Compile-time constant (not stored in memory)
public sealed record ConstSymbol(
    string Name,
    Types.SolidType Type,
    string ValueExpression
) : Symbol(Name);

// Static variable with mutable storage
public sealed record StaticSymbol(
    string Name,
    Types.SolidType Type
) : Symbol(Name);

// Const static variable (read-only, stored in .rodata)
public sealed record ConstStaticSymbol(
    string Name,
    Types.SolidType Type,
    string ValueExpression
) : Symbol(Name);
