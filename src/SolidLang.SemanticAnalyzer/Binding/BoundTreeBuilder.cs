using SolidLang.Parser;
using SolidLang.Parser.Nodes;
using SolidLang.Parser.Nodes.Declarations;
using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Literals;
using SolidLang.Parser.Nodes.Statements;
using SolidLang.Parser.Nodes.Types;
using SolidLang.Parser.Parser;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Pass 2: Walks all AST ProgramNodes and builds the corresponding BoundNode tree.
/// All names are resolved via the scope system. All types are resolved via TypeResolver.
/// </summary>
internal sealed class BoundTreeBuilder
{
    private readonly Scope _globalScope;
    private readonly DiagnosticBag _diagnostics;
    private readonly Dictionary<string, NamespaceSymbol> _namespaces;
    private Scope _currentScope;

    public BoundTreeBuilder(Scope globalScope, Dictionary<string, NamespaceSymbol> namespaces,
        DiagnosticBag diagnostics)
    {
        _globalScope = globalScope;
        _namespaces = namespaces;
        _diagnostics = diagnostics;
        _currentScope = globalScope;
    }

    /// <summary>
    /// Run Pass 2 over all program nodes. Returns the root BoundProgram.
    /// </summary>
    public BoundProgram Build(IReadOnlyList<ProgramNode> programs)
    {
        var declarations = new List<BoundDeclaration>();

        foreach (var program in programs)
        {
            // Enter namespace scope if applicable
            var savedScope = _currentScope;
            if (program.Namespace != null)
            {
                var fullPath = string.Join("::", program.Namespace.Path.Segments);
                if (_namespaces.TryGetValue(fullPath, out var nsSymbol))
                    _currentScope = nsSymbol.NamespaceScope;
            }

            foreach (var decl in program.Declarations)
            {
                var boundDecl = BuildDeclaration(decl);
                if (boundDecl != null)
                    declarations.Add(boundDecl);
            }

            _currentScope = savedScope;
        }

        return new BoundProgram(declarations, _globalScope);
    }

    // ========================================
    // Declaration builders
    // ========================================

    private BoundDeclaration? BuildDeclaration(DeclNode decl)
    {
        return decl switch
        {
            FunctionDeclNode f => BuildFunction(f),
            StructDeclNode s => BuildStruct(s),
            EnumDeclNode e => BuildEnum(e),
            UnionDeclNode u => BuildUnion(u),
            VariantDeclNode v => BuildVariant(v),
            InterfaceDeclNode i => BuildInterface(i),
            VarDeclNode vd => BuildVariable(vd),
            ConstDeclNode cd => BuildConstOrStatic(cd),
            StaticDeclNode sd => BuildConstOrStatic(sd),
            _ => null,
        };
    }

    private BoundFunctionDecl BuildFunction(FunctionDeclNode node)
    {
        // Resolve function symbol
        var symbol = _currentScope.LookupRecursive(node.Name) as FunctionSymbol;
        if (symbol == null) return null!;

        // Determine the correct scope for the function (handle out-of-line)
        var funcScope = symbol.BodyScope ?? _currentScope;
        var savedScope = _currentScope;
        _currentScope = funcScope ?? _currentScope;

        // Resolve return type
        var typeResolver = new TypeResolver(_currentScope, _diagnostics);
        SolidType? returnType = null;
        if (node.ReturnType != null)
            returnType = typeResolver.ResolveType(node.ReturnType);

        // Build parameter decls
        var parameters = new List<BoundVariableDecl>();
        if (node.Parameters != null)
        {
            foreach (var param in node.Parameters.Parameters)
            {
                var paramSymbol = _currentScope.Lookup(param.Name) as VariableSymbol;
                var paramType = typeResolver.ResolveType(param.Type);
                parameters.Add(new BoundVariableDecl(paramSymbol!, paramType, null));
            }
        }

        // Build body
        BoundBlock? body = null;
        if (node.Body != null)
            body = BuildBody(node.Body);

        var result = new BoundFunctionDecl(symbol, returnType, parameters, body,
            node.CallingConvention?.GetFullText());

        _currentScope = savedScope;
        return result;
    }

    private BoundDeclaration BuildStruct(StructDeclNode node)
    {
        return BuildTypeDeclaration(node.Name, node, node.GenericParams, node.Fields,
            (symbol, fields, typeScope) => new BoundStructDecl(symbol, fields, typeScope));
    }

    private BoundDeclaration BuildEnum(EnumDeclNode node)
    {
        return BuildTypeDeclaration(node.Name, node, null, node.Fields,
            (symbol, fields, typeScope) =>
            {
                var typeResolver = new TypeResolver(_currentScope, _diagnostics);
                var underlyingType = node.UnderlyingType != null
                    ? typeResolver.ResolveType(node.UnderlyingType) : null;
                return new BoundEnumDecl(symbol, fields, underlyingType, typeScope);
            });
    }

    private BoundDeclaration BuildUnion(UnionDeclNode node)
    {
        return BuildTypeDeclaration(node.Name, node, node.GenericParams, node.Fields,
            (symbol, fields, typeScope) => new BoundUnionDecl(symbol, fields, typeScope));
    }

    private BoundDeclaration BuildVariant(VariantDeclNode node)
    {
        return BuildTypeDeclaration(node.Name, node, node.GenericParams, node.Fields,
            (symbol, fields, typeScope) =>
            {
                var typeResolver = new TypeResolver(_currentScope, _diagnostics);
                var tagType = node.TagType != null ? typeResolver.ResolveType(node.TagType) : null;
                return new BoundVariantDecl(symbol, fields, tagType, typeScope);
            });
    }

    private BoundDeclaration BuildInterface(InterfaceDeclNode node)
    {
        var typeSymbol = _currentScope.LookupRecursive(node.Name) as TypeSymbol;
        if (typeSymbol?.TypeScope == null) return null!;

        var savedScope = _currentScope;
        _currentScope = typeSymbol.TypeScope;

        var typeResolver = new TypeResolver(_currentScope, _diagnostics);
        var methods = new List<BoundFieldDecl>();

        if (node.Fields != null)
        {
            foreach (var method in node.Fields.Fields)
            {
                var methodSymbol = _currentScope.Lookup(method.Name) as MemberSymbol;
                var returnType = method.ReturnType != null
                    ? typeResolver.ResolveType(method.ReturnType) : null;
                methods.Add(new BoundFieldDecl(methodSymbol!, returnType));
            }
        }

        _currentScope = savedScope;
        return new BoundInterfaceDecl(typeSymbol, methods, typeSymbol.TypeScope);
    }

    private BoundDeclaration BuildTypeDeclaration<TFields>(
        string name, SyntaxNode declNode,
        GenericParamsNode? genericParams,
        TFields? fieldsContainer,
        Func<TypeSymbol, IReadOnlyList<BoundFieldDecl>, Scope, BoundDeclaration> factory)
    {
        var typeSymbol = _currentScope.LookupRecursive(name) as TypeSymbol;
        if (typeSymbol?.TypeScope == null) return null!;

        var savedScope = _currentScope;
        _currentScope = typeSymbol.TypeScope;

        var typeResolver = new TypeResolver(_currentScope, _diagnostics);
        var boundFields = new List<BoundFieldDecl>();

        if (fieldsContainer != null)
        {
            // Use reflection-like approach to get Fields collection
            IReadOnlyList<object> fieldList = fieldsContainer switch
            {
                StructFieldsNode s => new List<object>(s.Fields.Cast<object>()),
                EnumFieldsNode e => new List<object>(e.Fields.Cast<object>()),
                UnionFieldsNode u => new List<object>(u.Fields.Cast<object>()),
                VariantFieldsNode v => new List<object>(v.Fields.Cast<object>()),
                _ => new List<object>(),
            };

            foreach (var field in fieldList)
            {
                BoundFieldDecl? boundField = field switch
                {
                    StructFieldNode sf => BuildField(sf, typeResolver),
                    EnumFieldNode ef => BuildEnumField(ef, typeResolver),
                    UnionFieldNode uf => BuildUnionField(uf, typeResolver),
                    VariantFieldNode vf => BuildVariantField(vf, typeResolver),
                    _ => null,
                };
                if (boundField != null) boundFields.Add(boundField);
            }
        }

        _currentScope = savedScope;
        return factory(typeSymbol, boundFields, typeSymbol.TypeScope);
    }

    private BoundFieldDecl? BuildField(StructFieldNode field, TypeResolver typeResolver)
    {
        var symbol = _currentScope.Lookup(field.Name) as MemberSymbol;
        if (symbol == null) return null;
        var fieldType = typeResolver.ResolveType(field.Type);
        return new BoundFieldDecl(symbol, fieldType);
    }

    private BoundFieldDecl? BuildEnumField(EnumFieldNode field, TypeResolver typeResolver)
    {
        var symbol = _currentScope.Lookup(field.Name) as MemberSymbol;
        if (symbol == null) return null;
        BoundExpression? value = null;
        if (field.Value != null)
            value = BuildExpression(field.Value);
        return new BoundFieldDecl(symbol, PrimitiveType.I32, value); // default enum type
    }

    private BoundFieldDecl? BuildUnionField(UnionFieldNode field, TypeResolver typeResolver)
    {
        var symbol = _currentScope.Lookup(field.Name) as MemberSymbol;
        if (symbol == null) return null;
        var fieldType = typeResolver.ResolveType(field.Type);
        return new BoundFieldDecl(symbol, fieldType);
    }

    private BoundFieldDecl? BuildVariantField(VariantFieldNode field, TypeResolver typeResolver)
    {
        var symbol = _currentScope.Lookup(field.Name) as MemberSymbol;
        if (symbol == null) return null;
        SolidType? fieldType = null;
        if (field.Type != null)
            fieldType = typeResolver.ResolveType(field.Type);
        return new BoundFieldDecl(symbol, fieldType);
    }

    private BoundVariableDecl BuildVariable(VarDeclNode node)
    {
        var symbol = _currentScope.LookupRecursive(node.Name) as VariableSymbol;
        if (symbol == null) return null!;

        var typeResolver = new TypeResolver(_currentScope, _diagnostics);
        SolidType? declaredType = null;
        if (node.Type != null)
            declaredType = typeResolver.ResolveType(node.Type);

        var initializer = BuildExpression(node.Initializer);
        return new BoundVariableDecl(symbol, declaredType, initializer);
    }

    private BoundVariableDecl? BuildConstOrStatic(DeclNode node)
    {
        string name;
        SyntaxNode declNode;
        ExprNode? initializer;

        switch (node)
        {
            case ConstDeclNode c:
                name = c.Name; declNode = c; initializer = c.Initializer; break;
            case StaticDeclNode s:
                name = s.Name; declNode = s; initializer = s.Initializer; break;
            default: return null;
        }

        var symbol = _currentScope.LookupRecursive(name) as VariableSymbol;
        if (symbol == null) return null;

        var typeResolver = new TypeResolver(_currentScope, _diagnostics);
        SolidType? declaredType = null;

        // Get type annotation from const/static declaration
        if (node is ConstDeclNode cd && cd.Type != null)
            declaredType = typeResolver.ResolveType(cd.Type);
        else if (node is StaticDeclNode sd && sd.Type != null)
            declaredType = typeResolver.ResolveType(sd.Type);

        var init = BuildExpression(initializer);
        return new BoundVariableDecl(symbol, declaredType, init);
    }

    private BoundVariableStmt? BuildConstOrStaticStmt(DeclNode node)
    {
        var decl = BuildConstOrStatic(node);
        return decl != null ? new BoundVariableStmt(decl) : null;
    }

    // ========================================
    // Body & Statement builders
    // ========================================

    private BoundBlock BuildBody(BodyStmtNode body)
    {
        // Find the scope that corresponds to this body
        var bodyScope = FindChildScope(_currentScope, body);

        var savedScope = _currentScope;
        _currentScope = bodyScope ?? _currentScope;

        var statements = new List<BoundStatement>();
        foreach (var stmt in body.Statements)
        {
            var boundStmt = BuildStatement(stmt);
            if (boundStmt != null)
                statements.Add(boundStmt);
        }

        _currentScope = savedScope;
        return new BoundBlock(bodyScope ?? new Scope(ScopeKind.Block, _currentScope, body), statements);
    }

    private BoundStatement? BuildStatement(StmtNode stmt)
    {
        return stmt switch
        {
            BodyStmtNode body => BuildBody(body),
            VarDeclNode vd => new BoundVariableStmt(BuildVariable(vd)),
            ConstDeclNode cd => BuildConstOrStaticStmt(cd),
            StaticDeclNode sd => BuildConstOrStaticStmt(sd),
            ExprStmtNode es => new BoundExprStmt(BuildExpression(es.Expression)),
            AssignStmtNode assn => new BoundAssignStmt(BuildExpression(assn.Target), assn.Operator, BuildExpression(assn.Value)),
            IfStmtNode ifStmt => BuildIf(ifStmt),
            ForStmtNode forStmt => BuildFor(forStmt),
            SwitchStmtNode switchStmt => BuildSwitch(switchStmt),
            ReturnStmtNode ret => new BoundReturnStmt(ret.Expression != null ? BuildExpression(ret.Expression) : null),
            BreakStmtNode => new BoundBreakStmt(),
            ContinueStmtNode => new BoundContinueStmt(),
            DeferStmtNode defer => new BoundDeferStmt(BuildStatement(defer.Statement)!),
            EmptyStmtNode => null,
            _ => null,
        };
    }

    private BoundIfStmt BuildIf(IfStmtNode ifStmt)
    {
        var condition = BuildExpression(ifStmt.Condition);
        var thenBody = BuildBody(ifStmt.ThenBody);
        BoundStatement? elseBody = null;

        if (ifStmt.ElseBody is BodyStmtNode elseB)
            elseBody = BuildBody(elseB);
        else if (ifStmt.ElseBody is IfStmtNode elseIf)
            elseBody = BuildIf(elseIf);

        return new BoundIfStmt(condition, thenBody, elseBody);
    }

    private BoundStatement BuildFor(ForStmtNode forStmt)
    {
        return forStmt.KindNode switch
        {
            ForInfiniteNode inf => BuildWhile(inf),
            ForCondNode cond => BuildWhileCond(cond),
            ForCStyleNode cs => BuildForCStyle(cs),
            _ => null!,
        };
    }

    private BoundWhileStmt BuildWhile(ForInfiniteNode node)
    {
        var body = BuildBody(node.Body);
        return new BoundWhileStmt(null, body); // null condition = infinite
    }

    private BoundWhileStmt BuildWhileCond(ForCondNode node)
    {
        var condition = BuildExpression(node.Condition);
        var body = BuildBody(node.Body);
        return new BoundWhileStmt(condition, body);
    }

    private BoundForCStyleStmt BuildForCStyle(ForCStyleNode node)
    {
        // Enter loop scope
        var loopScope = FindChildScope(_currentScope, node.Body);
        var savedScope = _currentScope;
        _currentScope = loopScope ?? _currentScope;

        BoundVariableDecl? initVar = null;
        BoundExpression? initExpr = null;

        if (node.Init is ForVarDeclNode varDecl)
        {
            var symbol = _currentScope.Lookup(varDecl.Name) as VariableSymbol;
            var initializer = BuildExpression(varDecl.Initializer);
            initVar = new BoundVariableDecl(symbol!, null, initializer);
        }
        else if (node.Init is ForAssignNode assign)
        {
            initExpr = BuildExpression(assign.Target);
            // wrap assignment
        }

        BoundExpression? condition = null;
        if (node.Condition != null)
            condition = BuildExpression(node.Condition);

        BoundExpression? update = null;
        if (node.Update != null)
            update = BuildExpression(node.Update);

        var body = BuildBody(node.Body);

        _currentScope = savedScope;
        return new BoundForCStyleStmt(initVar, initExpr, condition, update, body);
    }

    private BoundSwitchStmt BuildSwitch(SwitchStmtNode switchStmt)
    {
        var expression = BuildExpression(switchStmt.Expression);
        var arms = new List<BoundSwitchArm>();

        foreach (var arm in switchStmt.Arms)
        {
            var patterns = new List<BoundSwitchPattern>();
            foreach (var astPattern in arm.Patterns)
            {
                var boundPattern = BuildSwitchPattern(astPattern);
                if (boundPattern != null)
                    patterns.Add(boundPattern);
            }

            var armScope = FindChildScope(_currentScope, arm);
            var savedScope = _currentScope;
            _currentScope = armScope ?? _currentScope;

            var body = BuildStatement(arm.Statement);

            _currentScope = savedScope;
            arms.Add(new BoundSwitchArm(arm.IsElse, patterns, body!,
                armScope ?? new Scope(ScopeKind.SwitchArm, _currentScope)));
        }

        return new BoundSwitchStmt(expression, arms);
    }

    private BoundSwitchPattern? BuildSwitchPattern(SolidLang.Parser.Nodes.Statements.SwitchPatternNode astPattern)
    {
        var typeResolver = new TypeResolver(_currentScope, _diagnostics);

        switch (astPattern.PatternKind)
        {
            case SolidLang.Parser.Nodes.Statements.SwitchPatternKind.Literal:
                if (astPattern.Literal != null)
                {
                    var value = BuildLiteral(astPattern.Literal) as BoundLiteralExpr;
                    return new BoundSwitchPattern(SwitchPatternKind.Literal, literal: value);
                }
                break;

            case SolidLang.Parser.Nodes.Statements.SwitchPatternKind.NamedTypeMember:
                if (astPattern.NamedType != null && astPattern.MemberName != null)
                {
                    var typeSymbol = ResolveTypeSymbol(astPattern.NamedType);
                    var memberSymbol = typeSymbol?.TypeScope?.Lookup(astPattern.MemberName) as MemberSymbol;
                    if (memberSymbol != null)
                        return new BoundSwitchPattern(SwitchPatternKind.NamedTypeMember,
                            namedTypeSymbol: typeSymbol, memberSymbol: memberSymbol);
                }
                break;

            case SolidLang.Parser.Nodes.Statements.SwitchPatternKind.NamedTypeMemberBinding:
                if (astPattern.NamedType != null && astPattern.MemberName != null)
                {
                    var typeSymbol = ResolveTypeSymbol(astPattern.NamedType);
                    var memberSymbol = typeSymbol?.TypeScope?.Lookup(astPattern.MemberName) as MemberSymbol;
                    var binding = astPattern.Binding != null ? BuildExpression(astPattern.Binding) : null;
                    return new BoundSwitchPattern(SwitchPatternKind.NamedTypeMemberBinding,
                        namedTypeSymbol: typeSymbol, memberSymbol: memberSymbol, binding: binding);
                }
                break;

            case SolidLang.Parser.Nodes.Statements.SwitchPatternKind.Identifier:
                if (astPattern.Identifier != null)
                {
                    var captureVar = _currentScope.Lookup(astPattern.Identifier) as VariableSymbol;
                    return new BoundSwitchPattern(SwitchPatternKind.Identifier,
                        captureVariable: captureVar);
                }
                break;
        }

        return null;
    }

    private TypeSymbol? ResolveTypeSymbol(NamedTypeNode namedType)
    {
        var symbol = _currentScope.LookupRecursive(namedType.Name);
        return symbol as TypeSymbol;
    }

    // ========================================
    // Expression builders
    // ========================================

    private BoundExpression BuildExpression(ExprNode expr)
    {
        return expr switch
        {
            PrimaryExprNode p => BuildPrimaryExpr(p),
            UnaryExprNode u => new BoundUnaryExpr(u.Operator, BuildExpression(u.Operand)),
            BinaryExprNode b => new BoundBinaryExpr(BuildExpression(b.Left), b.Operator, BuildExpression(b.Right)),
            ConditionalExprNode c => new BoundConditionalExpr(BuildExpression(c.Condition),
                BuildExpression(c.ThenExpr), BuildExpression(c.ElseExpr)),
            PostfixExprNode pf => BuildPostfixExpr(pf),
            ScopedAccessExprNode sa => BuildScopedAccess(sa),
            _ => null!,
        };
    }

    private BoundExpression BuildPrimaryExpr(PrimaryExprNode primary)
    {
        switch (primary.PrimaryKind)
        {
            case PrimaryExprKind.Literal:
                if (primary.Literal != null)
                    return BuildLiteral(primary.Literal);
                break;

            case PrimaryExprKind.Identifier:
                if (primary.Identifier != null)
                {
                    var symbol = _currentScope.LookupRecursive(primary.Identifier);
                    if (symbol is VariableSymbol vs)
                        return new BoundVarExpr(vs);
                    // Function references: not yet supported as expressions
                    if (symbol == null)
                        _diagnostics.UndefinedName(primary.Identifier, primary.Span);
                }
                break;

            case PrimaryExprKind.Parenthesized:
                if (primary.ParenthesizedExpr != null)
                    return BuildExpression(primary.ParenthesizedExpr);
                break;

            case PrimaryExprKind.CtOperator:
                // Compile-time operators: skip for now, handled in Phase 2
                break;
        }

        return null!;
    }

    private BoundExpression BuildLiteral(LiteralNode literal)
    {
        return literal switch
        {
            IntegerLiteralNode i => new BoundLiteralExpr(PrimitiveType.I32, i.Value),
            FloatLiteralNode f => new BoundLiteralExpr(PrimitiveType.F64, f.Value),
            StringLiteralNode s => new BoundLiteralExpr(null, s.Value),        // string type TBD
            CharLiteralNode c => new BoundLiteralExpr(null, c.Value),          // char type TBD
            BoolLiteralNode b => new BoundLiteralExpr(PrimitiveType.Bool, b.Value),
            NullLiteralNode => new BoundLiteralExpr(NullType.Instance, null!),
            StructLiteralNode sl => BuildStructLiteral(sl),
            UnionLiteralNode ul => BuildUnionLiteral(ul),
            EnumLiteralNode el => BuildEnumLiteral(el),
            VariantLiteralNode vl => BuildVariantLiteral(vl),
            _ => new BoundLiteralExpr(ErrorType.Instance, null!),
        };
    }

    private BoundExpression BuildPostfixExpr(PostfixExprNode postfix)
    {
        var result = BuildExpression(postfix.Primary);

        foreach (var suffix in postfix.Suffixes)
        {
            result = suffix switch
            {
                CallExprNode call => BuildCall(result, call),
                DotAccessNode dot => BuildDotAccess(result, dot),
                IndexAccessNode index => new BoundIndexAccessExpr(result, BuildExpression(index.Index)),
                _ => result,
            };
        }

        return result;
    }

    private BoundCallExpr BuildCall(BoundExpression receiver, CallExprNode call)
    {
        // Resolve the function name from the receiver's symbol name
        string? funcName = null;
        if (receiver is BoundVarExpr varExpr)
            funcName = varExpr.Symbol.Name;

        if (funcName != null)
        {
            var symbol = _currentScope.LookupRecursive(funcName);
            if (symbol is FunctionSymbol fs)
            {
                var args = new List<BoundExpression>();
                if (call.Arguments != null)
                {
                    foreach (var arg in call.Arguments.Arguments)
                        args.Add(BuildExpression(arg.Expression));
                }
                return new BoundCallExpr(fs, args);
            }
        }

        // For scope access calls (already resolved)
        return new BoundCallExpr(null!, Array.Empty<BoundExpression>());
    }

    private BoundExpression BuildDotAccess(BoundExpression receiver, DotAccessNode dot)
    {
        // Simple case: receiver is a variable with a known type
        return new BoundMemberAccessExpr(receiver, null!); // Member resolution deferred to Phase 2
    }

    private BoundExpression BuildScopedAccess(ScopedAccessExprNode node)
    {
        // Simple case: NS::Type::member
        if (node.Prefix != null)
        {
            var namedType = node.Prefix.NamedType;
            var typeSymbol = _currentScope.LookupRecursive(namedType.Name) as TypeSymbol;
            if (typeSymbol?.TypeScope != null)
            {
                var memberSymbol = typeSymbol.TypeScope.Lookup(node.Name) as MemberSymbol;
                if (memberSymbol != null)
                {
                    // If this has call args, it's a method call
                    return new BoundVarExpr(null!); // Scoped access TBD
                }
            }
        }

        return null!;
    }

    private BoundExpression BuildStructLiteral(StructLiteralNode literal)
    {
        var typeSymbol = _currentScope.LookupRecursive(literal.StructType.Name) as TypeSymbol;
        var fields = new List<(MemberSymbol, BoundExpression)>();

        foreach (var field in literal.Fields)
        {
            var memberSymbol = typeSymbol?.TypeScope?.Lookup(field.Name) as MemberSymbol;
            var value = BuildExpression(field.Value);
            fields.Add((memberSymbol!, value));
        }

        return new BoundStructLiteralExpr(typeSymbol!, fields);
    }

    private BoundExpression BuildUnionLiteral(UnionLiteralNode literal)
    {
        var typeSymbol = _currentScope.LookupRecursive(literal.UnionType.Name) as TypeSymbol;
        var memberSymbol = typeSymbol?.TypeScope?.Lookup(literal.MemberName) as MemberSymbol;
        BoundExpression? value = null;
        if (literal.Value != null)
            value = BuildExpression(literal.Value);
        return new BoundUnionLiteralExpr(typeSymbol!, memberSymbol!, value);
    }

    private BoundExpression BuildEnumLiteral(EnumLiteralNode literal)
    {
        var typeSymbol = _currentScope.LookupRecursive(literal.EnumType.Name) as TypeSymbol;
        var memberSymbol = typeSymbol?.TypeScope?.Lookup(literal.MemberName) as MemberSymbol;
        return new BoundEnumLiteralExpr(typeSymbol!, memberSymbol!);
    }

    private BoundExpression BuildVariantLiteral(VariantLiteralNode literal)
    {
        var typeSymbol = _currentScope.LookupRecursive(literal.VariantType.Name) as TypeSymbol;
        var memberSymbol = typeSymbol?.TypeScope?.Lookup(literal.MemberName) as MemberSymbol;
        BoundExpression? value = null;
        if (literal.Value != null)
            value = BuildExpression(literal.Value);
        return new BoundVariantLiteralExpr(typeSymbol!, memberSymbol!, value);
    }

    // ========================================
    // Helpers
    // ========================================

    /// <summary>
    /// Finds the child scope that corresponds to a given AST node by comparing owning nodes.
    /// </summary>
    private static Scope? FindChildScope(Scope parent, SyntaxNode owningNode)
    {
        return null; // Scope traversal TBD — use dictionary from symbol builder
    }
}
