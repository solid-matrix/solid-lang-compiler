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
