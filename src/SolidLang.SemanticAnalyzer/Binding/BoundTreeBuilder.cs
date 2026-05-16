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
    private readonly Dictionary<SyntaxNode, Scope> _scopeMap;
    private Scope _currentScope;

    public BoundTreeBuilder(Scope globalScope, Dictionary<string, NamespaceSymbol> namespaces,
        Dictionary<SyntaxNode, Scope> scopeMap, DiagnosticBag diagnostics)
    {
        _globalScope = globalScope;
        _namespaces = namespaces;
        _scopeMap = scopeMap;
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
        if (ShouldSkipPlatform(decl)) return null;

        return decl switch
        {
            FunctionDeclNode f => BuildFunction(f),
            StructDeclNode s => BuildStruct(s),
            EnumDeclNode e => BuildEnum(e),
            UnionDeclNode u => BuildUnion(u),
            VariantDeclNode v => BuildVariant(v),
            InterfaceDeclNode i => BuildInterface(i),
            VariableDeclNode vd => BuildVariable(vd),
            _ => null,
        };
    }

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

    private BoundFunctionDecl BuildFunction(FunctionDeclNode node)
    {
        // Determine lookup scope: for NS::func, look in namespace scope
        var lookupScope = _currentScope;
        if (node.NamedTypePrefix != null)
        {
            var namedType = node.NamedTypePrefix;
            var nsSegments = namedType.NamespacePrefix != null
                ? namedType.NamespacePrefix.Path.Segments.ToList()
                : new List<string>();
            nsSegments.Add(namedType.Name);
            var fullPath = string.Join("::", nsSegments);
            if (_namespaces.TryGetValue(fullPath, out var nsSymbol))
                lookupScope = nsSymbol.NamespaceScope;
        }

        // Resolve function symbol
        var symbol = lookupScope.LookupRecursive(node.Name) as FunctionSymbol;
        if (symbol == null) return null!;

        // Determine the correct scope for the function (handle out-of-line)
        var funcScope = symbol.BodyScope ?? lookupScope;
        var savedScope = _currentScope;
        _currentScope = funcScope ?? _currentScope;

        // Resolve return type
        var typeResolver = new TypeResolver(_currentScope, _diagnostics);
        SolidType? returnType = null;
        if (node.ReturnType != null)
            returnType = typeResolver.ResolveType(node.ReturnType);
        else if (node.Name == "main")
            returnType = PrimitiveType.I32;  // main implicitly returns i32

        // Build parameter decls
        var parameters = new List<BoundVariableDecl>();
        foreach (var param in node.Parameters)
        {
            var paramSymbol = _currentScope.Lookup(param.Name) as VariableSymbol;
            var paramType = typeResolver.ResolveType(param.Type);
            if (paramSymbol != null)
                paramSymbol.DeclaredType = paramType;
            parameters.Add(new BoundVariableDecl(paramSymbol!, paramType, null));
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
        return BuildTypeDeclaration(node.Name, node, Array.Empty<GenericParamNode>(), node.Fields,
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

        foreach (var method in node.Fields)
        {
            var methodSymbol = _currentScope.Lookup(method.Name) as MemberSymbol;
            var returnType = method.ReturnType != null
                ? typeResolver.ResolveType(method.ReturnType) : null;
            methods.Add(new BoundFieldDecl(methodSymbol!, returnType));
        }

        _currentScope = savedScope;
        return new BoundInterfaceDecl(typeSymbol, methods, typeSymbol.TypeScope);
    }

    private BoundDeclaration BuildTypeDeclaration(
        string name, SyntaxNode declNode,
        IReadOnlyList<GenericParamNode> genericParams,
        IEnumerable<SyntaxNode> fieldNodes,
        Func<TypeSymbol, IReadOnlyList<BoundFieldDecl>, Scope, BoundDeclaration> factory)
    {
        var typeSymbol = _currentScope.LookupRecursive(name) as TypeSymbol;
        if (typeSymbol?.TypeScope == null) return null!;

        var savedScope = _currentScope;
        _currentScope = typeSymbol.TypeScope;

        var typeResolver = new TypeResolver(_currentScope, _diagnostics);
        var boundFields = new List<BoundFieldDecl>();

        foreach (var field in fieldNodes)
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

    private BoundVariableDecl BuildVariable(VariableDeclNode node)
    {
        var symbol = _currentScope.LookupRecursive(node.Name) as VariableSymbol;
        if (symbol == null) return null!;

        var typeResolver = new TypeResolver(_currentScope, _diagnostics);
        SolidType? declaredType = null;
        if (node.Type != null)
            declaredType = typeResolver.ResolveType(node.Type);

        BoundExpression? initializer = null;
        if (node.Initializer != null)
            initializer = BuildExpression(node.Initializer);

        // Type inference: if no explicit type, infer from initializer
        if (declaredType == null && initializer != null)
            declaredType = initializer.Type;

        symbol.DeclaredType = declaredType;

        return new BoundVariableDecl(symbol, declaredType, initializer);
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
            VariableDeclNode vd => new BoundVariableStmt(BuildVariable(vd)),
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
            ForCStyleNode cs => BuildForCStyle(cs, forStmt),
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

    private BoundForCStyleStmt BuildForCStyle(ForCStyleNode node, ForStmtNode forStmt)
    {
        // Enter loop scope
        var loopScope = FindChildScope(_currentScope, forStmt);
        var savedScope = _currentScope;
        _currentScope = loopScope ?? _currentScope;

        BoundVariableDecl? initVar = null;
        BoundExpression? initExpr = null;

        if (node.Init is ForVarDeclNode varDecl)
        {
            var symbol = _currentScope.Lookup(varDecl.Name) as VariableSymbol;
            var initializer = BuildExpression(varDecl.Initializer);
            var inferredType = initializer?.Type;
            if (symbol != null) symbol.DeclaredType = inferredType;
            initVar = new BoundVariableDecl(symbol!, inferredType, initializer);
        }
        else if (node.Init is ForAssignNode assign)
        {
            var target = BuildExpression(assign.Target);
            var value = BuildExpression(assign.Value);
            initExpr = new BoundBinaryExpr(target, assign.Operator, value);
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
            UnaryExprNode u => BuildUnaryExpr(u),
            BinaryExprNode b => new BoundBinaryExpr(BuildExpression(b.Left), b.Operator, BuildExpression(b.Right)),
            ConditionalExprNode c => new BoundConditionalExpr(BuildExpression(c.Condition),
                BuildExpression(c.ThenExpr), BuildExpression(c.ElseExpr)),
            PostfixExprNode pf => BuildPostfixExpr(pf),
            ScopedAccessExprNode sa => BuildScopedAccess(sa),
            _ => null!,
        };
    }

    private BoundExpression BuildUnaryExpr(UnaryExprNode u)
    {
        var operand = BuildExpression(u.Operand);

        // When & is applied to an expression that has already been sugared
        // with &. (e.g. &pt&.y is parsed as &(pt&.y)), the outer & is
        // redundant and would cause codegen to crash because the inner &
        // produces a temporary GEP value with no memory address.
        if (u.Operator == SyntaxKind.AmpersandToken &&
            operand is BoundUnaryExpr innerUn &&
            innerUn.Operator == SyntaxKind.AmpersandToken &&
            innerUn.Operand is BoundMemberAccessExpr)
        {
            return operand;
        }

        return new BoundUnaryExpr(u.Operator, operand);
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
                    if (symbol is FunctionSymbol fs)
                        return new BoundVarExpr(fs);
                    if (symbol == null)
                        _diagnostics.UndefinedName(primary.Identifier, primary.Span);
                }
                break;

            case PrimaryExprKind.Parenthesized:
                if (primary.ParenthesizedExpr != null)
                    return BuildExpression(primary.ParenthesizedExpr);
                break;

            case PrimaryExprKind.CtOperator:
                if (primary.CtOperator != null)
                    return BuildCtOperator(primary.CtOperator);
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
            StringLiteralNode s => BuildStringLiteral(s),
            CharLiteralNode c => new BoundLiteralExpr(null, c.Value),          // char type TBD
            BoolLiteralNode b => new BoundLiteralExpr(PrimitiveType.Bool, b.Value),
            NullLiteralNode => new BoundLiteralExpr(null, null!),          // null has no inherent type — target-typed
            ArrayLiteralNode al => BuildArrayLiteral(al),
            StructLiteralNode sl => BuildStructLiteral(sl),
            UnionLiteralNode ul => BuildUnionLiteral(ul),
            EnumLiteralNode el => BuildEnumLiteral(el),
            VariantLiteralNode vl => BuildVariantLiteral(vl),
            _ => new BoundLiteralExpr(null, null!),
        };
    }

    private BoundExpression BuildStringLiteral(StringLiteralNode s)
    {
        var stringTypeSym = _currentScope.LookupRecursive("String") as TypeSymbol;
        if (stringTypeSym != null)
            return new BoundLiteralExpr(new NamedType(stringTypeSym), s.Value);
        return new BoundLiteralExpr(new PointerType(PrimitiveType.U8), s.Value);
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
                PointerAccessNode pa => BuildPointerAccess(result, pa),
                AddressAccessNode aa => BuildAddressAccess(result, aa),
                IndexAccessNode index => new BoundIndexAccessExpr(result, BuildExpression(index.Index)),
                _ => result,
            };
        }

        return result;
    }

    private BoundExpression BuildCall(BoundExpression receiver, CallExprNode call)
    {
        // Pass through already-resolved built-in calls
        if (receiver is BoundBuiltinCallExpr)
            return receiver;

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
                foreach (var arg in call.Arguments)
                    args.Add(BuildExpression(arg.Expression));

                // Target-type null literals from function parameters.
                // Resolve parameter types if not already set (may happen when the called
                // function is declared in a file processed after the caller).
                for (int i = 0; i < args.Count && i < fs.Parameters.Count; i++)
                {
                    if (args[i] is BoundLiteralExpr { Type: null, Value: null })
                    {
                        var paramType = fs.Parameters[i].DeclaredType;
                        if (paramType == null && fs.Parameters[i].Declaration is FuncParameterNode fp)
                            paramType = new TypeResolver(_currentScope, _diagnostics).ResolveType(fp.Type);
                        args[i] = new BoundLiteralExpr(paramType, null!);
                    }
                }

                return new BoundCallExpr(fs, args);
            }
        }

        // For scope access calls (already resolved)
        return new BoundCallExpr(null!, Array.Empty<BoundExpression>());
    }

    private BoundExpression BuildDotAccess(BoundExpression receiver, DotAccessNode dot)
    {
        // Check for compiler built-in methods
        var builtin = TryBuildBuiltinCall(receiver, dot);
        if (builtin != null) return builtin;

        if (receiver.Type is NamedType nt && nt.TypeSymbol.TypeScope != null)
        {
            var member = nt.TypeSymbol.TypeScope.Lookup(dot.Name) as MemberSymbol;
            if (member != null)
                return new BoundMemberAccessExpr(receiver, member);
        }
        return new BoundMemberAccessExpr(receiver, null!);
    }

    private BoundExpression BuildPointerAccess(BoundExpression receiver, PointerAccessNode pa)
    {
        // Desugar *.member: (*receiver).member for pointer receivers,
        // or receiver.member for struct receivers (chained *. after deref).
        TypeSymbol? typeSym = null;
        if (receiver.Type is NamedType nt)
            typeSym = nt.TypeSymbol;
        else if (receiver.Type is PointerType pt && pt.PointeeType is NamedType ptNt)
            typeSym = ptNt.TypeSymbol;

        if (typeSym?.TypeScope != null)
        {
            var member = typeSym.TypeScope.Lookup(pa.Name) as MemberSymbol;
            if (member != null)
            {
                var inner = receiver.Type is PointerType
                    ? new BoundUnaryExpr(SyntaxKind.StarToken, receiver)
                    : receiver;
                return new BoundMemberAccessExpr(inner, member);
            }
        }

        var inner2 = receiver.Type is PointerType
            ? new BoundUnaryExpr(SyntaxKind.StarToken, receiver)
            : receiver;
        return new BoundMemberAccessExpr(inner2, null!);
    }

    private BoundExpression? TryBuildBuiltinCall(BoundExpression receiver, DotAccessNode dot)
    {
        var typeResolver = new TypeResolver(_currentScope, _diagnostics);

        // array.to_pointer()
        if (receiver.Type is ArrayType at && dot.Name == "to_pointer")
        {
            var ptrType = new PointerType(at.ElementType, true);
            return new BoundBuiltinCallExpr(BuiltinMethodKind.ToPointer, receiver, null, ptrType);
        }

        // array.to_slice()
        if (receiver.Type is ArrayType at2 && dot.Name == "to_slice")
        {
            var sliceTypeSym = _currentScope.LookupRecursive("Slice") as TypeSymbol;
            if (sliceTypeSym != null)
            {
                var elementTypeArg = at2.ElementType;
                var sliceType = new NamedType(sliceTypeSym, new List<SolidType> { elementTypeArg });
                return new BoundBuiltinCallExpr(BuiltinMethodKind.ToSlice, receiver, elementTypeArg, sliceType);
            }
        }

        // value.into<T>()
        if (dot.Name == "into" && dot.TypeArguments.Count > 0)
        {
            var targetType = typeResolver.ResolveType(dot.TypeArguments[0]);
            return new BoundBuiltinCallExpr(BuiltinMethodKind.TypeCast, receiver, targetType, targetType);
        }

        // value.into_TARGET() for primitive types
        if (dot.Name.StartsWith("into_") && receiver.Type is PrimitiveType)
        {
            var targetName = dot.Name.Substring("into_".Length);
            var targetType = ResolveIntrinsicTypeName(targetName);
            if (targetType != null)
                return new BoundBuiltinCallExpr(BuiltinMethodKind.TypeCast, receiver, targetType, targetType);
        }

        return null;
    }

    private BoundExpression BuildArrayLiteral(ArrayLiteralNode node)
    {
        var typeResolver = new TypeResolver(_currentScope, _diagnostics);
        var arrayType = typeResolver.ResolveType(node.ArrayType);
        return new BoundLiteralExpr(arrayType, null!);
    }

    private BoundExpression BuildAddressAccess(BoundExpression receiver, AddressAccessNode aa)
    {
        // &.member desugars to (&receiver).member per spec.
        // Resolve the member in the receiver's struct scope.
        MemberSymbol? member = null;
        if (receiver.Type is NamedType nt && nt.TypeSymbol.TypeScope != null)
            member = nt.TypeSymbol.TypeScope.Lookup(aa.Name) as MemberSymbol;
        else if (receiver.Type is PointerType pt && pt.PointeeType is NamedType ptNt && ptNt.TypeSymbol.TypeScope != null)
            member = ptNt.TypeSymbol.TypeScope.Lookup(aa.Name) as MemberSymbol;

        var addrOf = new BoundUnaryExpr(SyntaxKind.AmpersandToken, receiver);
        return new BoundMemberAccessExpr(addrOf, member!);
    }

    private BoundExpression BuildScopedAccess(ScopedAccessExprNode node)
    {
        if (node.Prefix != null)
        {
            var namedType = node.Prefix;

            // Build full namespace path from prefix segments + type name
            var nsSegments = namedType.NamespacePrefix != null
                ? namedType.NamespacePrefix.Path.Segments.ToList()
                : new List<string>();
            nsSegments.Add(namedType.Name);
            var fullPath = string.Join("::", nsSegments);

            // Case 1: Namespace-qualified access: NS::name or NS::NS::name
            if (_namespaces.TryGetValue(fullPath, out var nsSymbol))
            {
                var symbol = nsSymbol.NamespaceScope.Lookup(node.Name);
                if (symbol is FunctionSymbol funcSym)
                {
                    var args = node.Arguments.Select(a => BuildExpression(a.Expression)).ToList();

                    // Target-type null literals from function parameters.
                    // Resolve parameter types if not already set (may happen when the called
                    // function is declared in a file processed after the caller).
                    for (int i = 0; i < args.Count && i < funcSym.Parameters.Count; i++)
                    {
                        if (args[i] is BoundLiteralExpr { Type: null, Value: null })
                        {
                            var paramType = funcSym.Parameters[i].DeclaredType;
                            if (paramType == null && funcSym.Parameters[i].Declaration is FuncParameterNode fp)
                                paramType = new TypeResolver(_currentScope, _diagnostics).ResolveType(fp.Type);
                            args[i] = new BoundLiteralExpr(paramType, null!);
                        }
                    }

                    // @intrinsic functions: generate builtin call (e.g., i8::from(value))
                    if (funcSym.IsIntrinsic)
                    {
                        var targetType = ResolveIntrinsicTypeName(fullPath);
                        if (targetType != null && args.Count >= 1)
                        {
                            var srcExpr = args[0];
                            return new BoundBuiltinCallExpr(BuiltinMethodKind.TypeCast, srcExpr, targetType, targetType);
                        }
                    }

                    return new BoundCallExpr(funcSym, args);
                }
                if (symbol is VariableSymbol varSym)
                    return new BoundVarExpr(varSym);
            }

            // Case 2: Type::member — variant literal, enum literal, or struct member
            var typeSymbol = _currentScope.LookupRecursive(namedType.Name) as TypeSymbol;
            if (typeSymbol?.TypeScope != null)
            {
                var memberSymbol = typeSymbol.TypeScope.Lookup(node.Name) as MemberSymbol;
                if (memberSymbol != null)
                {
                    if (typeSymbol.Kind == SymbolKind.Variant)
                    {
                        // VariantType::Member(args) parsed as scoped access
                        BoundExpression? value = null;
                        if (node.Arguments.Count > 0)
                            value = BuildExpression(node.Arguments[0].Expression);
                        return new BoundVariantLiteralExpr(typeSymbol, memberSymbol, value);
                    }
                    if (typeSymbol.Kind == SymbolKind.Enum)
                        return new BoundEnumLiteralExpr(typeSymbol, memberSymbol);
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

        // If this is actually a variant type, treat it as a variant literal
        if (typeSymbol?.Kind == SymbolKind.Variant)
            return new BoundVariantLiteralExpr(typeSymbol!, memberSymbol!, null);

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

    private BoundExpression BuildCtOperator(CtOperatorExprNode ctOperator)
    {
        var typeResolver = new TypeResolver(_currentScope, _diagnostics);
        var resultType = PrimitiveType.USize;

        var opKind = ctOperator.Name switch
        {
            "sizeof" => CtOperatorKind.Sizeof,
            "alignof" => CtOperatorKind.Alignof,
            "offsetof" => CtOperatorKind.Offsetof,
            _ => CtOperatorKind.Sizeof,
        };

        SolidType? typeArg = null;
        string? memberName = null;

        if (ctOperator.Arguments != null)
        {
            for (int i = 0; i < ctOperator.Arguments.Count; i++)
            {
                var arg = ctOperator.Arguments[i];
                if (arg.Type != null)
                    typeArg = typeResolver.ResolveType(arg.Type);
                else if (arg.Expression is PrimaryExprNode p && p.Identifier != null)
                    memberName = p.Identifier;
            }
        }

        return new BoundCtOperatorExpr(opKind, typeArg, memberName, resultType);
    }

    // ========================================
    // Helpers
    // ========================================

    /// <summary>
    /// Finds the child scope that corresponds to a given AST node by looking up the scope map.
    /// </summary>
    private Scope? FindChildScope(Scope parent, SyntaxNode owningNode)
    {
        _scopeMap.TryGetValue(owningNode, out var scope);
        return scope;
    }

    /// <summary>
    /// Maps a primitive type name (e.g., "i8", "f32") to its PrimitiveType singleton.
    /// Used for resolving @intrinsic function target types from namespace/method names.
    /// </summary>
    private static PrimitiveType? ResolveIntrinsicTypeName(string name) => name switch
    {
        "i8" => PrimitiveType.I8,
        "i16" => PrimitiveType.I16,
        "i32" => PrimitiveType.I32,
        "i64" => PrimitiveType.I64,
        "isize" => PrimitiveType.ISize,
        "u8" => PrimitiveType.U8,
        "u16" => PrimitiveType.U16,
        "u32" => PrimitiveType.U32,
        "u64" => PrimitiveType.U64,
        "usize" => PrimitiveType.USize,
        "f32" => PrimitiveType.F32,
        "f64" => PrimitiveType.F64,
        "bool" => PrimitiveType.Bool,
        _ => null,
    };
}
