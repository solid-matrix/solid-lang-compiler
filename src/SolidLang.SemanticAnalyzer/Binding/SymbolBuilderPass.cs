using SolidLang.Parser;
using SolidLang.Parser.Nodes;
using SolidLang.Parser.Nodes.Declarations;
using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Statements;
using SolidLang.Parser.Nodes.Types;
using SolidLang.Parser.Parser;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Pass 1: Walks all AST ProgramNodes top-down and builds the full symbol table.
/// Registers every name-introducing node as a Symbol in the appropriate Scope.
/// Handles forward declarations, out-of-line members, using imports, and namespaces.
/// </summary>
internal sealed class SymbolBuilderPass
{
    private readonly DiagnosticBag _diagnostics;
    private Scope _currentScope;
    private Scope _globalScope = null!;

    // Forward declaration tracking: name → symbol, for merging definitions
    private readonly Dictionary<string, Symbol> _forwardDecls = new();

    // Registered namespaces: full path → NamespaceSymbol
    private readonly Dictionary<string, NamespaceSymbol> _namespaces = new();

    // Maps each scope-creating AST node to its Scope, for Pass 2 to find child scopes
    private readonly Dictionary<SyntaxNode, Scope> _scopeMap = new();

    public SymbolBuilderPass(DiagnosticBag diagnostics)
    {
        _diagnostics = diagnostics;
        _currentScope = null!;
    }

    /// <summary>
    /// Run Pass 1 over all program nodes. Returns the global scope.
    /// </summary>
    public (Scope GlobalScope, Dictionary<string, NamespaceSymbol> Namespaces, Dictionary<SyntaxNode, Scope> ScopeMap) Run(
        IReadOnlyList<ProgramNode> programs)
    {
        // Create global scope and register primitives
        _globalScope = new Scope(ScopeKind.Global);
        _currentScope = _globalScope;
        TypeFactory.RegisterPrimitives(_globalScope);

        // Walk all programs
        foreach (var program in programs)
            WalkProgram(program);

        // Implicit using std::predeclare — available in all scopes
        if (_namespaces.TryGetValue("std::predeclare", out var predeclareNs))
            _globalScope.AddImport(predeclareNs.NamespaceScope);

        return (_globalScope, _namespaces, _scopeMap);
    }

    // ========================================
    // Top-level walk
    // ========================================

    private void WalkProgram(ProgramNode program)
    {
        // Determine which scope top-level declarations go into
        if (program.Namespace != null)
        {
            var nsSymbol = GetOrCreateNamespace(program.Namespace.Path);
            // Enter the namespace scope for this file's declarations
            var savedScope = _currentScope;
            _currentScope = nsSymbol.NamespaceScope;

            // Process usings inside the namespace
            foreach (var usingDecl in program.Usings)
                ProcessUsing(usingDecl);

            // Walk top-level declarations into namespace scope
            foreach (var decl in program.Declarations)
                WalkDeclaration(decl);

            _currentScope = savedScope;
        }
        else
        {
            // Process usings at global level
            foreach (var usingDecl in program.Usings)
                ProcessUsing(usingDecl);

            // Walk top-level declarations into global scope
            foreach (var decl in program.Declarations)
                WalkDeclaration(decl);
        }
    }

    // ========================================
    // Namespace and Using
    // ========================================

    private NamespaceSymbol GetOrCreateNamespace(NamespacePathNode path)
    {
        var fullPath = string.Join("::", path.Segments);
        if (_namespaces.TryGetValue(fullPath, out var existing))
            return existing;

        // Create namespace hierarchy (a::b::c → scopes a → b → c)
        var parentScope = _globalScope;
        NamespaceSymbol? lastSymbol = null;

        foreach (var segment in path.Segments)
        {
            var existingNsSymbol = parentScope.Lookup(segment) as NamespaceSymbol;
            if (existingNsSymbol != null)
            {
                parentScope = existingNsSymbol.NamespaceScope;
                lastSymbol = existingNsSymbol;
                continue;
            }

            var nsScope = new Scope(ScopeKind.Namespace, parentScope);
            var nsSymbol = new NamespaceSymbol(segment, path, nsScope, fullPath);
            parentScope.Declare(nsSymbol);
            parentScope = nsScope;
            lastSymbol = nsSymbol;
        }

        if (lastSymbol != null)
            _namespaces[fullPath] = lastSymbol;

        return lastSymbol!;
    }

    private void ProcessUsing(UsingDeclNode usingDecl)
    {
        var nsSymbol = GetOrCreateNamespace(usingDecl.Path);
        _currentScope.AddImport(nsSymbol.NamespaceScope);
    }

    // ========================================
    // Declaration walk — main dispatch
    // ========================================

    private void WalkDeclaration(DeclNode decl)
    {
        // Compile-time platform filtering
        if (ShouldSkipPlatform(decl)) return;

        switch (decl)
        {
            case FunctionDeclNode func: WalkFunction(func); break;
            case StructDeclNode st: WalkStruct(st); break;
            case EnumDeclNode en: WalkEnum(en); break;
            case UnionDeclNode un: WalkUnion(un); break;
            case VariantDeclNode vr: WalkVariant(vr); break;
            case InterfaceDeclNode iface: WalkInterface(iface); break;
            case VariableDeclNode vd: WalkVariable(vd); break;
            case BadDeclNode: break; // Skip error recovery nodes
        }
    }

    // ========================================
    // Function declarations
    // ========================================

    private void WalkFunction(FunctionDeclNode node)
    {
        // Handle out-of-line member: Struct<T>::method() or NS::function()
        Scope? targetScope = null;
        if (node.NamedTypePrefix != null)
        {
            var namedType = node.NamedTypePrefix;

            // First, try as type (for Struct::method out-of-line members)
            var typeSymbol = _currentScope.LookupRecursive(namedType.Name) as TypeSymbol;
            if (typeSymbol?.TypeScope != null)
            {
                targetScope = typeSymbol.TypeScope;
            }
            else
            {
                // Not a type — try as namespace (NS::function or NS::NS::function)
                var nsSegments = namedType.NamespacePrefix != null
                    ? namedType.NamespacePrefix.Path.Segments.ToList()
                    : new List<string>();
                nsSegments.Add(namedType.Name);

                var nsPath = new NamespacePathNode(nsSegments, namedType.Span, string.Join("::", nsSegments));
                var nsSymbol = GetOrCreateNamespace(nsPath);
                targetScope = nsSymbol.NamespaceScope;
            }
        }

        var actualScope = targetScope ?? _currentScope;
        var isIntrinsic = IsIntrinsicFunction(node.Annotations);

        // Create or merge forward declaration
        var existing = actualScope.Lookup(node.Name);
        FunctionSymbol funcSymbol;
        var skipRegistration = false;

        if (existing is FunctionSymbol fs && ((FunctionDeclNode)fs.Declaration).Body == null)
        {
            fs.SetDeclaration(node);
            fs.ImportName ??= ExtractImportName(node.Annotations);
            fs.IsIntrinsic = isIntrinsic || fs.IsIntrinsic;
            funcSymbol = fs;
        }
        else if (existing != null)
        {
            // Allow duplicate names for @intrinsic functions (overloading by param type)
            if (isIntrinsic && existing is FunctionSymbol existingFs && existingFs.IsIntrinsic)
            {
                funcSymbol = new FunctionSymbol(node.Name, node, isIntrinsic: true);
                skipRegistration = true;
            }
            else
            {
                _diagnostics.DuplicateName(node.Name, "function", node.Span, existing.Declaration?.Span ?? node.Span);
                return;
            }
        }
        else
        {
            funcSymbol = new FunctionSymbol(node.Name, node, isIntrinsic: isIntrinsic);
            actualScope.Declare(funcSymbol);
        }

        // For intrinsic overloads (duplicate name), only set import name and return
        if (skipRegistration)
        {
            funcSymbol.ImportName = ExtractImportName(node.Annotations);
            return;
        }
        funcSymbol.ImportName = ExtractImportName(node.Annotations);

        // Enter function scope
        var funcScope = new Scope(ScopeKind.Function, _currentScope, node);
        _scopeMap[node] = funcScope;
        var savedScope = _currentScope;
        _currentScope = funcScope;

        // Register generic params in function scope
        foreach (var gp in node.GenericParams)
        {
            var gpSymbol = new GenericParamSymbol(gp.Name, gp);
            _currentScope.Declare(gpSymbol);
        }

        // Register parameters in function scope
        var paramSymbols = new List<VariableSymbol>();
        foreach (var param in node.Parameters)
        {
            var paramSymbol = new VariableSymbol(SymbolKind.Parameter, param.Name, param);
            _currentScope.Declare(paramSymbol);
            paramSymbols.Add(paramSymbol);
        }
        funcSymbol.Parameters = paramSymbols;

        // Walk body (which may contain nested scopes)
        if (node.Body != null)
            WalkBody(node.Body);

        funcSymbol.BodyScope = funcScope;

        _currentScope = savedScope;
    }

    // ========================================
    // Struct declarations
    // ========================================

    private void WalkStruct(StructDeclNode node)
    {
        var typeSymbol = RegisterTypeDeclaration(node.Name, node, SymbolKind.Struct, node.Fields.Count == 0);
        if (typeSymbol == null) return;

        if (node.Fields.Count > 0)
        {
            var typeScope = new Scope(ScopeKind.Type, _currentScope, node);
            _scopeMap[node] = typeScope;
            typeSymbol.TypeScope = typeScope;

            var savedScope = _currentScope;
            _currentScope = typeScope;

            // Register generic params
            if (node.GenericParams != null)
            {
                foreach (var gp in node.GenericParams)
                    _currentScope.Declare(new GenericParamSymbol(gp.Name, gp));
            }

            // Register fields
            foreach (var field in node.Fields)
            {
                var memberSymbol = new MemberSymbol(SymbolKind.StructField, field.Name, field);
                _currentScope.Declare(memberSymbol);
            }

            _currentScope = savedScope;
        }
    }

    // ========================================
    // Enum declarations
    // ========================================

    private void WalkEnum(EnumDeclNode node)
    {
        var typeSymbol = RegisterTypeDeclaration(node.Name, node, SymbolKind.Enum, node.Fields.Count == 0);
        if (typeSymbol == null) return;

        if (node.Fields.Count > 0)
        {
            var typeScope = new Scope(ScopeKind.Type, _currentScope, node);
            _scopeMap[node] = typeScope;
            typeSymbol.TypeScope = typeScope;

            var savedScope = _currentScope;
            _currentScope = typeScope;

            foreach (var field in node.Fields)
            {
                var memberSymbol = new MemberSymbol(SymbolKind.EnumField, field.Name, field);
                _currentScope.Declare(memberSymbol);
            }

            _currentScope = savedScope;
        }
    }

    // ========================================
    // Union declarations
    // ========================================

    private void WalkUnion(UnionDeclNode node)
    {
        var typeSymbol = RegisterTypeDeclaration(node.Name, node, SymbolKind.Union, node.Fields.Count == 0);
        if (typeSymbol == null) return;

        if (node.Fields.Count > 0)
        {
            var typeScope = new Scope(ScopeKind.Type, _currentScope, node);
            _scopeMap[node] = typeScope;
            typeSymbol.TypeScope = typeScope;

            var savedScope = _currentScope;
            _currentScope = typeScope;

            if (node.GenericParams != null)
            {
                foreach (var gp in node.GenericParams)
                    _currentScope.Declare(new GenericParamSymbol(gp.Name, gp));
            }

            foreach (var field in node.Fields)
            {
                var memberSymbol = new MemberSymbol(SymbolKind.UnionField, field.Name, field);
                _currentScope.Declare(memberSymbol);
            }

            _currentScope = savedScope;
        }
    }

    // ========================================
    // Variant declarations
    // ========================================

    private void WalkVariant(VariantDeclNode node)
    {
        var typeSymbol = RegisterTypeDeclaration(node.Name, node, SymbolKind.Variant, node.Fields.Count == 0);
        if (typeSymbol == null) return;

        if (node.Fields.Count > 0)
        {
            var typeScope = new Scope(ScopeKind.Type, _currentScope, node);
            _scopeMap[node] = typeScope;
            typeSymbol.TypeScope = typeScope;

            var savedScope = _currentScope;
            _currentScope = typeScope;

            if (node.GenericParams != null)
            {
                foreach (var gp in node.GenericParams)
                    _currentScope.Declare(new GenericParamSymbol(gp.Name, gp));
            }

            foreach (var field in node.Fields)
            {
                var memberSymbol = new MemberSymbol(SymbolKind.VariantField, field.Name, field);
                _currentScope.Declare(memberSymbol);
            }

            _currentScope = savedScope;
        }
    }

    // ========================================
    // Interface declarations
    // ========================================

    private void WalkInterface(InterfaceDeclNode node)
    {
        var typeSymbol = RegisterTypeDeclaration(node.Name, node, SymbolKind.Interface, false);
        if (typeSymbol == null) return;

        var typeScope = new Scope(ScopeKind.Type, _currentScope, node);
        _scopeMap[node] = typeScope;
        typeSymbol.TypeScope = typeScope;

        var savedScope = _currentScope;
        _currentScope = typeScope;

        foreach (var gp in node.GenericParams)
            _currentScope.Declare(new GenericParamSymbol(gp.Name, gp));

        foreach (var method in node.Fields)
        {
            var memberSymbol = new MemberSymbol(SymbolKind.InterfaceMethod, method.Name, method);
            _currentScope.Declare(memberSymbol);
        }

        _currentScope = savedScope;
    }

    // ========================================
    // Variable declarations
    // ========================================

    private void WalkVariable(VariableDeclNode node)
    {
        var kind = node.Keyword switch
        {
            SyntaxKind.VarKeyword => SymbolKind.VarVariable,
            SyntaxKind.ConstKeyword => SymbolKind.ConstVariable,
            SyntaxKind.StaticKeyword => SymbolKind.StaticVariable,
            _ => SymbolKind.VarVariable,
        };

        // Handle out-of-line member (const/static)
        var targetScope = _currentScope;
        if (node.NamedTypePrefix is { } prefix)
        {
            var typeName = prefix.Name;
            var typeSymbol = targetScope.LookupRecursive(typeName) as TypeSymbol;
            if (typeSymbol?.TypeScope == null)
            {
                _diagnostics.UndefinedName(typeName, prefix.Span);
                return;
            }
            targetScope = typeSymbol.TypeScope;
        }

        var symbol = new VariableSymbol(kind, node.Name, node);
        if (kind != SymbolKind.VarVariable)
            symbol.ImportName = ExtractImportName(node.Annotations);
        if (!targetScope.TryDeclare(symbol))
        {
            var existing = targetScope.Lookup(node.Name)!;
            _diagnostics.DuplicateName(node.Name, kind.ToString(), node.Span, existing.Declaration?.Span ?? node.Span);
        }
    }

    // ========================================
    // Statement and body walk
    // ========================================

    private void WalkBody(BodyStmtNode body)
    {
        var scope = new Scope(ScopeKind.Block, _currentScope, body);
        _scopeMap[body] = scope;
        var savedScope = _currentScope;
        _currentScope = scope;

        foreach (var stmt in body.Statements)
            WalkStatement(stmt);

        _currentScope = savedScope;
    }

    private void WalkStatement(StmtNode stmt)
    {
        switch (stmt)
        {
            case BodyStmtNode body: WalkBody(body); break;
            case VariableDeclNode vd: WalkVariable(vd); break;
            case IfStmtNode ifStmt: WalkIf(ifStmt); break;
            case ForStmtNode forStmt: WalkFor(forStmt); break;
            case SwitchStmtNode switchStmt: WalkSwitch(switchStmt); break;
            case DeferStmtNode deferStmt: WalkStatement(deferStmt.Statement); break;
            // Other statements (ExprStmt, AssignStmt, Return, Break, Continue) don't introduce scopes
        }
    }

    private void WalkIf(IfStmtNode ifStmt)
    {
        WalkBody(ifStmt.ThenBody);
        if (ifStmt.ElseBody is BodyStmtNode elseBody)
            WalkBody(elseBody);
        else if (ifStmt.ElseBody is IfStmtNode elseIf)
            WalkIf(elseIf);
        else if (ifStmt.ElseBody != null)
            WalkStatement(ifStmt.ElseBody);
    }

    private void WalkFor(ForStmtNode forStmt)
    {
        // Enter loop scope
        var loopScope = new Scope(ScopeKind.Block, _currentScope, forStmt);
        _scopeMap[forStmt] = loopScope;
        var savedScope = _currentScope;
        _currentScope = loopScope;

        // Register for-var-decl variable if present
        if (forStmt.KindNode is ForCStyleNode cStyle && cStyle.Init is ForVarDeclNode varDecl)
        {
            var symbol = new VariableSymbol(SymbolKind.ForLoopVariable, varDecl.Name, varDecl);
            _currentScope.Declare(symbol);
        }

        // Walk body
        switch (forStmt.KindNode)
        {
            case ForInfiniteNode inf: WalkBody(inf.Body); break;
            case ForCondNode cond: WalkBody(cond.Body); break;
            case ForCStyleNode cs: WalkBody(cs.Body); break;
        }

        _currentScope = savedScope;
    }

    private void WalkSwitch(SwitchStmtNode switchStmt)
    {
        foreach (var arm in switchStmt.Arms)
        {
            // Each arm gets its own scope for pattern-capture bindings
            var armScope = new Scope(ScopeKind.SwitchArm, _currentScope, arm);
            _scopeMap[arm] = armScope;
            var savedScope = _currentScope;
            _currentScope = armScope;

            // Register identifier capture patterns as variables
            foreach (var pattern in arm.Patterns)
            {
                if (pattern.PatternKind == SolidLang.Parser.Nodes.Statements.SwitchPatternKind.Identifier
                    && pattern.Identifier != null)
                {
                    var captureSymbol = new VariableSymbol(
                        SymbolKind.VarVariable, pattern.Identifier, pattern);
                    _currentScope.Declare(captureSymbol);
                }
            }

            WalkStatement(arm.Statement);
            _currentScope = savedScope;
        }
    }

    // ========================================
    // Helpers
    // ========================================

    /// <summary>
    /// Returns true if the function has the @intrinsic annotation.
    /// </summary>
    private static bool IsIntrinsicFunction(IReadOnlyList<CtAnnotateNode> annotations)
    {
        return annotations.Any(a => a.Name == "intrinsic");
    }

    /// <summary>
    /// Extracts the linker symbol name from @import(name) annotation.
    /// Returns null if @import has no argument (use the declared name).
    /// </summary>
    private static string? ExtractImportName(IReadOnlyList<CtAnnotateNode> annotations)
    {
        var importAnnot = annotations.FirstOrDefault(a => a.Name == "import");
        var firstArg = importAnnot?.Arguments.FirstOrDefault();
        return firstArg?.GetFullText();
    }

    /// <summary>
    /// Returns true if the declaration should be skipped due to platform annotations.
    /// Supported: @os(windows) / @windows (skip on non-Windows), @os(unix) / @unix (skip on Windows),
    /// @if_msvc / @if_not_msvc (legacy aliases).
    /// </summary>
    private static bool ShouldSkipPlatform(DeclNode decl)
    {
        var annotations = GetDeclAnnotations(decl);
        var isWindows = OperatingSystem.IsWindows();
        foreach (var a in annotations)
        {
            var name = a.Name == "os"
                ? a.Arguments.FirstOrDefault()?.GetFullText()
                : a.Name;
            if ((name == "windows" || a.Name == "if_msvc") && !isWindows) return true;
            if ((name == "unix" || a.Name == "if_not_msvc") && isWindows) return true;
        }
        return false;
    }

    private static IReadOnlyList<CtAnnotateNode> GetDeclAnnotations(DeclNode decl) => decl switch
    {
        FunctionDeclNode f => f.Annotations,
        StructDeclNode s => s.Annotations,
        EnumDeclNode e => e.Annotations,
        UnionDeclNode u => u.Annotations,
        VariantDeclNode v => v.Annotations,
        InterfaceDeclNode i => i.Annotations,
        VariableDeclNode vd => vd.Annotations,
        _ => Array.Empty<CtAnnotateNode>(),
    };

    /// <summary>
    /// Creates or merges a TypeSymbol for a type declaration.
    /// Returns null on error.
    /// </summary>
    private TypeSymbol? RegisterTypeDeclaration(string name, SyntaxNode node, SymbolKind kind, bool isForward)
    {
        var existing = _currentScope.Lookup(name);

        if (existing is TypeSymbol ts)
        {
            var existingIsForward = IsForwardTypeDecl(ts);
            // Forward decl + definition = merge
            if (existingIsForward && !isForward)
            {
                ts.SetDeclaration(node);
                if (ts.Kind != kind)
                {
                    _diagnostics.KindMismatch(name, kind.ToString(), ts.Kind.ToString(), node.Span);
                    return null;
                }
                return ts;
            }
            // Forward decl + forward decl = OK (same kind)
            if (existingIsForward && isForward && ts.Kind == kind)
                return ts;

            // Definition already exists = error
            _diagnostics.DuplicateName(name, "type", node.Span, existing.Declaration?.Span ?? node.Span);
            return null;
        }

        var typeSymbol = new TypeSymbol(kind, name, node);
        _currentScope.Declare(typeSymbol);
        return typeSymbol;
    }

    private static bool IsForwardTypeDecl(TypeSymbol ts) => ts.Declaration switch
    {
        StructDeclNode s => s.Fields.Count == 0,
        EnumDeclNode e => e.Fields.Count == 0,
        UnionDeclNode u => u.Fields.Count == 0,
        VariantDeclNode v => v.Fields.Count == 0,
        _ => true,
    };
}
