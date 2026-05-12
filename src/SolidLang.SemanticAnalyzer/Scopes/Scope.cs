using SolidLang.Parser.Nodes;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// A lexical scope — a mapping from names to Symbols.
/// Scopes form a tree via Parent pointers. Name lookup walks the parent chain.
/// </summary>
public sealed class Scope
{
    private readonly Dictionary<string, Symbol> _members = new();
    private readonly List<Scope> _importedScopes = new();

    public ScopeKind Kind { get; }
    public Scope? Parent { get; }
    public SyntaxNode? OwningNode { get; }

    /// <summary>
    /// For Type scopes: the TypeSymbol that owns this scope.
    /// </summary>
    public TypeSymbol? OwningTypeSymbol { get; internal set; }
    public IReadOnlyDictionary<string, Symbol> Members => _members;
    public IReadOnlyList<Scope> ImportedScopes => _importedScopes;

    public Scope(ScopeKind kind, Scope? parent = null, SyntaxNode? owningNode = null)
    {
        Kind = kind;
        Parent = parent;
        OwningNode = owningNode;
    }

    /// <summary>
    /// Registers a symbol in this scope. Returns false if the name is already declared.
    /// </summary>
    public bool TryDeclare(Symbol symbol)
    {
        if (_members.ContainsKey(symbol.Name))
            return false;
        symbol.ContainingScope = this;
        _members[symbol.Name] = symbol;
        return true;
    }

    /// <summary>
    /// Registers a symbol, asserting no duplicate.
    /// </summary>
    public void Declare(Symbol symbol)
    {
        symbol.ContainingScope = this;
        _members[symbol.Name] = symbol;
    }

    /// <summary>
    /// Looks up a name in this scope only (no parent walk).
    /// </summary>
    public Symbol? Lookup(string name)
    {
        _members.TryGetValue(name, out var symbol);
        return symbol;
    }

    /// <summary>
    /// Adds an imported scope (from a using declaration or external library).
    /// </summary>
    public void AddImport(Scope importedScope)
    {
        _importedScopes.Add(importedScope);
    }

    /// <summary>
    /// Fully qualified lookup: walks the parent chain.
    /// At each level, checks Members first, then ImportedScopes.
    /// Local declarations shadow imported names.
    /// </summary>
    public Symbol? LookupRecursive(string name)
    {
        return LookupRecursiveImpl(name, new HashSet<Scope>());
    }

    private Symbol? LookupRecursiveImpl(string name, HashSet<Scope> visited)
    {
        var current = this;
        while (current != null)
        {
            if (!visited.Add(current))
                return null; // cycle detected

            if (current._members.TryGetValue(name, out var symbol))
                return symbol;

            // Check imports at this level
            foreach (var import in current._importedScopes)
            {
                var found = import.LookupRecursiveImpl(name, visited);
                if (found != null)
                    return found;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Checks if a name exists in this scope only.
    /// </summary>
    public bool ContainsLocally(string name) => _members.ContainsKey(name);
}
