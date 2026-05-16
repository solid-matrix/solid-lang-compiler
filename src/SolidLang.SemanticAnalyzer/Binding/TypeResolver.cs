using SolidLang.Parser;
using SolidLang.Parser.Nodes;
using SolidLang.Parser.Nodes.Types;
using SolidLang.Parser.Parser;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Converts syntactic TypeNode trees to resolved SolidType objects.
/// Called during Pass 2 (BoundTreeBuilder) whenever a type annotation is encountered.
/// </summary>
internal sealed class TypeResolver
{
    private readonly Scope _currentScope;
    private readonly DiagnosticBag _diagnostics;

    public TypeResolver(Scope currentScope, DiagnosticBag diagnostics)
    {
        _currentScope = currentScope;
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// Resolves any TypeNode to its corresponding SolidType.
    /// </summary>
    public SolidType ResolveType(TypeNode typeNode)
    {
        return typeNode switch
        {
            PrimitiveTypeNode p => ResolvePrimitive(p),
            IntegerTypeNode i => ResolveInteger(i),
            FloatTypeNode f => ResolveFloat(f),
            BoolTypeNode b => ResolveBool(b),
            NamedTypeNode n => ResolveNamed(n),
            PointerTypeNode p => ResolvePointer(p),
            ArrayTypeNode a => ResolveArray(a),
            FuncPointerTypeNode f => ResolveFuncPointer(f),
            BadTypeNode => ErrorType.Instance,
            _ => ErrorType.Instance,
        };
    }

    private SolidType ResolvePrimitive(PrimitiveTypeNode node)
    {
        return PrimitiveType.FromKeywordKind(node.PrimitiveKind) ?? (SolidType)ErrorType.Instance;
    }

    private SolidType ResolveInteger(IntegerTypeNode node)
    {
        return PrimitiveType.FromKeywordKind(node.IntegerKind) ?? (SolidType)ErrorType.Instance;
    }

    private SolidType ResolveFloat(FloatTypeNode node)
    {
        return PrimitiveType.FromKeywordKind(node.FloatKind) ?? (SolidType)ErrorType.Instance;
    }

    private static SolidType ResolveBool(BoolTypeNode node) => PrimitiveType.Bool;

    private SolidType ResolveNamed(NamedTypeNode node)
    {
        // Look up the type name in the current scope
        var symbol = _currentScope.LookupRecursive(node.Name);

        if (symbol is GenericParamSymbol)
            return new TypeParameterType(node.Name);

        if (symbol is not TypeSymbol typeSymbol)
        {
            if (symbol != null)
                _diagnostics.NotAType(node.Name, node.Span);
            else
                _diagnostics.UndefinedName(node.Name, node.Span);
            return ErrorType.Instance;
        }

        // Resolve type arguments
        var typeArgs = new List<SolidType>();
        foreach (var arg in node.TypeArguments)
            typeArgs.Add(ResolveType(arg));

        return new NamedType(typeSymbol, typeArgs);
    }

    private SolidType ResolvePointer(PointerTypeNode node)
    {
        var pointee = ResolveType(node.PointeeType);
        return new PointerType(pointee, node.HasWritePermission);
    }

    private SolidType ResolveArray(ArrayTypeNode node)
    {
        var elementType = ResolveType(node.ElementType);

        // Try to evaluate size as compile-time constant (Phase 1: only literal integers)
        int? size = TryEvaluateConstantSize(node.Size);

        return new ArrayType(elementType, size);
    }

    private SolidType ResolveFuncPointer(FuncPointerTypeNode node)
    {
        var paramTypes = node.ParameterTypes.Select(ResolveType).ToArray();
        var returnType = ResolveType(node.ReturnType);
        var conv = node.CallingConvention?.GetFullText();

        return new FuncPointerType(paramTypes, returnType, conv);
    }

    /// <summary>
    /// Tries to evaluate a constant integer size expression.
    /// </summary>
    private int? TryEvaluateConstantSize(SolidLang.Parser.Nodes.Expressions.ExprNode expr)
    {
        if (expr is SolidLang.Parser.Nodes.Expressions.PrimaryExprNode primary)
        {
            // Integer literal: 12, 0u, etc.
            if (primary.PrimaryKind == SolidLang.Parser.Nodes.Expressions.PrimaryExprKind.Literal
                && primary.Literal is SolidLang.Parser.Nodes.Literals.IntegerLiteralNode intLit)
            {
                return (int)Convert.ToInt64(intLit.Value);
            }

            // Identifier reference to const: N where const N: usize = 12u
            if (primary.PrimaryKind == SolidLang.Parser.Nodes.Expressions.PrimaryExprKind.Identifier
                && primary.Identifier != null)
            {
                var symbol = _currentScope.LookupRecursive(primary.Identifier);
                if (symbol is VariableSymbol vs
                    && vs.Kind == SymbolKind.ConstVariable
                    && vs.Declaration != null)
                {
                    // Try to evaluate the const initializer
                    if (vs.Declaration is SolidLang.Parser.Nodes.Declarations.VariableDeclNode constDecl
                        && constDecl.Keyword == SolidLang.Parser.SyntaxKind.ConstKeyword
                        && constDecl.Initializer != null)
                    {
                        return TryEvaluateConstantSize(constDecl.Initializer);
                    }
                }
            }
        }

        return null;
    }
}
