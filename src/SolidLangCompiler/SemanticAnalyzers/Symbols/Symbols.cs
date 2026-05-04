using SolidLangCompiler.AST.Types;

using SolidLangCompiler.AST;
using SolidLangCompiler.AST.Types;

namespace SolidLangCompiler.SemanticAnalyzers.Symbols;

/// <summary>
/// Base class for all symbols.
/// </summary>
public abstract record Symbol
{
    public string Name { get; init; }
    public SourceLocation Location { get; init; }
    public bool IsPublic { get; init; }

    protected Symbol(string name, SourceLocation location, bool isPublic = false)
    {
        Name = name;
        Location = location;
        IsPublic = isPublic;
    }
}

/// <summary>
/// Represents a type symbol (struct, union, enum, variant, interface).
/// </summary>
public record TypeSymbol : Symbol
{
    public TypeNode? Definition { get; init; }
    public IReadOnlyList<Symbol>? Members { get; init; }
    public IReadOnlyList<string>? GenericParameters { get; init; }

    public TypeSymbol(string name, SourceLocation location, TypeNode? definition = null, bool isPublic = false)
        : base(name, location, isPublic)
    {
        Definition = definition;
    }
}

/// <summary>
/// Represents a function symbol.
/// </summary>
public record FuncSymbol : Symbol
{
    public TypeNode? ReturnType { get; init; }
    public IReadOnlyList<ParameterSymbol>? Parameters { get; init; }
    public CallingConvention? CallingConvention { get; init; }
    public IReadOnlyList<string>? GenericParameters { get; init; }

    public FuncSymbol(string name, SourceLocation location, bool isPublic = false)
        : base(name, location, isPublic)
    {
    }
}

/// <summary>
/// Represents a parameter symbol.
/// </summary>
public record ParameterSymbol : Symbol
{
    public TypeNode Type { get; init; }
    public int Index { get; init; }

    public ParameterSymbol(string name, SourceLocation location, TypeNode type, int index)
        : base(name, location)
    {
        Type = type;
        Index = index;
    }
}

/// <summary>
/// Represents a variable symbol.
/// </summary>
public record VariableSymbol : Symbol
{
    public TypeNode Type { get; init; }
    public bool IsMutable { get; init; }

    public VariableSymbol(string name, SourceLocation location, TypeNode type, bool isMutable = true)
        : base(name, location)
    {
        Type = type;
        IsMutable = isMutable;
    }
}

/// <summary>
/// Represents a constant symbol.
/// </summary>
public record ConstSymbol : Symbol
{
    public TypeNode Type { get; init; }
    public object? ConstantValue { get; init; }

    public ConstSymbol(string name, SourceLocation location, TypeNode type, bool isPublic = false)
        : base(name, location, isPublic)
    {
        Type = type;
    }
}

/// <summary>
/// Represents a static variable symbol.
/// </summary>
public record StaticSymbol : Symbol
{
    public TypeNode Type { get; init; }

    public StaticSymbol(string name, SourceLocation location, TypeNode type, bool isPublic = false)
        : base(name, location, isPublic)
    {
        Type = type;
    }
}

/// <summary>
/// Represents a field symbol in a struct/union/variant.
/// </summary>
public record FieldSymbol : Symbol
{
    public TypeNode Type { get; init; }
    public int Offset { get; init; }

    public FieldSymbol(string name, SourceLocation location, TypeNode type, int offset = 0)
        : base(name, location)
    {
        Type = type;
        Offset = offset;
    }
}

/// <summary>
/// Represents an enum member symbol.
/// </summary>
public record EnumMemberSymbol : Symbol
{
    public ulong Value { get; init; }

    public EnumMemberSymbol(string name, SourceLocation location, ulong value)
        : base(name, location)
    {
        Value = value;
    }
}
