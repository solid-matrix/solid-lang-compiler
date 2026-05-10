namespace SolidLang.SemanticAnalyzer;

// ========================================
// Bound Declaration nodes
// ========================================

/// <summary>
/// Abstract base for all bound declarations.
/// </summary>
public abstract class BoundDeclaration : BoundNode { }

/// <summary>
/// A function declaration in the bound tree.
/// </summary>
public sealed class BoundFunctionDecl : BoundDeclaration
{
    public override BoundKind Kind => BoundKind.FunctionDecl;
    public FunctionSymbol Symbol { get; }
    public SolidType? ReturnType { get; }
    public IReadOnlyList<BoundVariableDecl> Parameters { get; }
    public BoundBlock? Body { get; }
    public string? CallingConvention { get; }

    public BoundFunctionDecl(FunctionSymbol symbol, SolidType? returnType,
        IReadOnlyList<BoundVariableDecl> parameters, BoundBlock? body, string? callingConvention = null)
    {
        Symbol = symbol;
        ReturnType = returnType;
        Parameters = parameters;
        Body = body;
        CallingConvention = callingConvention;
    }
}

/// <summary>
/// A struct declaration in the bound tree.
/// </summary>
public sealed class BoundStructDecl : BoundDeclaration
{
    public override BoundKind Kind => BoundKind.StructDecl;
    public TypeSymbol Symbol { get; }
    public IReadOnlyList<BoundFieldDecl> Fields { get; }
    public Scope TypeScope { get; }

    public BoundStructDecl(TypeSymbol symbol, IReadOnlyList<BoundFieldDecl> fields, Scope typeScope)
    {
        Symbol = symbol;
        Fields = fields;
        TypeScope = typeScope;
    }
}

/// <summary>
/// An enum declaration in the bound tree.
/// </summary>
public sealed class BoundEnumDecl : BoundDeclaration
{
    public override BoundKind Kind => BoundKind.EnumDecl;
    public TypeSymbol Symbol { get; }
    public IReadOnlyList<BoundFieldDecl> Fields { get; }
    public SolidType? UnderlyingType { get; }
    public Scope TypeScope { get; }

    public BoundEnumDecl(TypeSymbol symbol, IReadOnlyList<BoundFieldDecl> fields,
        SolidType? underlyingType, Scope typeScope)
    {
        Symbol = symbol;
        Fields = fields;
        UnderlyingType = underlyingType;
        TypeScope = typeScope;
    }
}

/// <summary>
/// A union declaration in the bound tree.
/// </summary>
public sealed class BoundUnionDecl : BoundDeclaration
{
    public override BoundKind Kind => BoundKind.UnionDecl;
    public TypeSymbol Symbol { get; }
    public IReadOnlyList<BoundFieldDecl> Fields { get; }
    public Scope TypeScope { get; }

    public BoundUnionDecl(TypeSymbol symbol, IReadOnlyList<BoundFieldDecl> fields, Scope typeScope)
    {
        Symbol = symbol;
        Fields = fields;
        TypeScope = typeScope;
    }
}

/// <summary>
/// A variant declaration in the bound tree.
/// </summary>
public sealed class BoundVariantDecl : BoundDeclaration
{
    public override BoundKind Kind => BoundKind.VariantDecl;
    public TypeSymbol Symbol { get; }
    public IReadOnlyList<BoundFieldDecl> Fields { get; }
    public SolidType? TagType { get; }
    public Scope TypeScope { get; }

    public BoundVariantDecl(TypeSymbol symbol, IReadOnlyList<BoundFieldDecl> fields,
        SolidType? tagType, Scope typeScope)
    {
        Symbol = symbol;
        Fields = fields;
        TagType = tagType;
        TypeScope = typeScope;
    }
}

/// <summary>
/// An interface declaration in the bound tree.
/// </summary>
public sealed class BoundInterfaceDecl : BoundDeclaration
{
    public override BoundKind Kind => BoundKind.InterfaceDecl;
    public TypeSymbol Symbol { get; }
    public IReadOnlyList<BoundFieldDecl> Methods { get; }
    public Scope TypeScope { get; }

    public BoundInterfaceDecl(TypeSymbol symbol, IReadOnlyList<BoundFieldDecl> methods, Scope typeScope)
    {
        Symbol = symbol;
        Methods = methods;
        TypeScope = typeScope;
    }
}

/// <summary>
/// A variable/const/static declaration in the bound tree.
/// Covers: var, const, static, function parameter, for-loop variable.
/// </summary>
public sealed class BoundVariableDecl : BoundDeclaration
{
    public override BoundKind Kind => BoundKind.VariableDecl;
    public VariableSymbol Symbol { get; }
    public SolidType? DeclaredType { get; }
    public BoundExpression? Initializer { get; }

    public BoundVariableDecl(VariableSymbol symbol, SolidType? declaredType, BoundExpression? initializer)
    {
        Symbol = symbol;
        DeclaredType = declaredType;
        Initializer = initializer;
    }
}

/// <summary>
/// A field/enum value/variant arm/interface method in the bound tree.
/// </summary>
public sealed class BoundFieldDecl : BoundDeclaration
{
    public override BoundKind Kind => BoundKind.FieldDecl;
    public MemberSymbol Symbol { get; }
    public SolidType? FieldType { get; }    // null for variant fields with no type, methods with no return
    public BoundExpression? Value { get; }  // For enum discriminants

    public BoundFieldDecl(MemberSymbol symbol, SolidType? fieldType, BoundExpression? value = null)
    {
        Symbol = symbol;
        FieldType = fieldType;
        Value = value;
    }
}
