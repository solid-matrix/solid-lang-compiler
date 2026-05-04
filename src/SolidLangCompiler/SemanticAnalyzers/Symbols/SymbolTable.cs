namespace SolidLangCompiler.SemanticAnalyzers.Symbols;

/// <summary>
/// Represents a scope in the symbol table.
/// </summary>
public class Scope
{
    private readonly Dictionary<string, Symbol> _symbols = new();
    private readonly List<Scope> _children = new();

    public Scope? Parent { get; }
    public string Name { get; }
    public IReadOnlyList<Scope> Children => _children;
    public IReadOnlyDictionary<string, Symbol> Symbols => _symbols;

    public Scope(string name, Scope? parent = null)
    {
        Name = name;
        Parent = parent;
        parent?._children.Add(this);
    }

    /// <summary>
    /// Tries to add a symbol to this scope.
    /// Returns false if a symbol with the same name already exists.
    /// </summary>
    public bool TryAddSymbol(Symbol symbol)
    {
        return _symbols.TryAdd(symbol.Name, symbol);
    }

    /// <summary>
    /// Adds a symbol to this scope, throwing if it already exists.
    /// </summary>
    public void AddSymbol(Symbol symbol)
    {
        if (!_symbols.TryAdd(symbol.Name, symbol))
        {
            throw new SemanticException($"Symbol '{symbol.Name}' is already defined in this scope", symbol.Location);
        }
    }

    /// <summary>
    /// Looks up a symbol in this scope and all parent scopes.
    /// </summary>
    public Symbol? Lookup(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
            return symbol;

        return Parent?.Lookup(name);
    }

    /// <summary>
    /// Looks up a symbol only in this scope.
    /// </summary>
    public Symbol? LookupLocal(string name)
    {
        return _symbols.TryGetValue(name, out var symbol) ? symbol : null;
    }
}

/// <summary>
/// Represents the global symbol table.
/// </summary>
public class SymbolTable
{
    private readonly Dictionary<string, Scope> _namespaces = new();

    public Scope GlobalScope { get; }
    public Scope CurrentScope { get; private set; }

    public SymbolTable()
    {
        GlobalScope = new Scope("<global>");
        CurrentScope = GlobalScope;
    }

    /// <summary>
    /// Enters a new scope.
    /// </summary>
    public Scope EnterScope(string name)
    {
        var scope = new Scope(name, CurrentScope);
        CurrentScope = scope;
        return scope;
    }

    /// <summary>
    /// Exits the current scope.
    /// </summary>
    public void ExitScope()
    {
        if (CurrentScope.Parent == null)
            throw new InvalidOperationException("Cannot exit the global scope");

        CurrentScope = CurrentScope.Parent;
    }

    /// <summary>
    /// Gets or creates a namespace scope.
    /// </summary>
    public Scope GetOrCreateNamespace(string namespacePath)
    {
        if (_namespaces.TryGetValue(namespacePath, out var scope))
            return scope;

        scope = new Scope(namespacePath, GlobalScope);
        _namespaces[namespacePath] = scope;
        return scope;
    }

    /// <summary>
    /// Looks up a symbol starting from the current scope.
    /// </summary>
    public Symbol? Lookup(string name)
    {
        return CurrentScope.Lookup(name);
    }

    /// <summary>
    /// Adds a symbol to the current scope.
    /// </summary>
    public void AddSymbol(Symbol symbol)
    {
        CurrentScope.AddSymbol(symbol);
    }

    /// <summary>
    /// Tries to add a symbol to the current scope.
    /// </summary>
    public bool TryAddSymbol(Symbol symbol)
    {
        return CurrentScope.TryAddSymbol(symbol);
    }
}
