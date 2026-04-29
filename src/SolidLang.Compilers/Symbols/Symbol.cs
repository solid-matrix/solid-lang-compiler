namespace SolidLang.Compilers.Symbols;

public abstract record Symbol(string Name);

public sealed record VariableSymbol(string Name, Types.SolidType Type) : Symbol(Name);

public sealed record FunctionSymbol(
    string Name,
    Types.SolidType ReturnType,
    IReadOnlyList<VariableSymbol> Parameters
) : Symbol(Name);
