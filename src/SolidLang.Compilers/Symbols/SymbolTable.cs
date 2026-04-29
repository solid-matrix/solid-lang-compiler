namespace SolidLang.Compilers.Symbols;

public sealed class SymbolTable
{
    private readonly Dictionary<string, Symbol> _symbols = new();
    private readonly SymbolTable? _parent;

    public SymbolTable(SymbolTable? parent = null)
    {
        _parent = parent;
    }

    public void Define(Symbol symbol)
    {
        _symbols[symbol.Name] = symbol;
    }

    public Symbol? Lookup(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
            return symbol;
        return _parent?.Lookup(name);
    }

    public SymbolTable EnterScope()
    {
        return new SymbolTable(this);
    }
}
