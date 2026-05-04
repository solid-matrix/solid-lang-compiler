using SolidLangCompiler.AST;

namespace SolidLangCompiler.AST.Types;

/// <summary>
/// Integer type kinds.
/// </summary>
public enum IntegerKind
{
    I8, I16, I32, I64, ISize,
    U8, U16, U32, U64, USize
}

/// <summary>
/// Floating-point type kinds.
/// </summary>
public enum FloatKind
{
    F32, F64
}

/// <summary>
/// Calling convention.
/// </summary>
public enum CallingConvention
{
    CDecl,
    StdCall
}

/// <summary>
/// Base class for all type nodes.
/// </summary>
public abstract record TypeNode : AstNode;

/// <summary>
/// Represents an integer type (i8, i16, i32, i64, isize, u8, u16, u32, u64, usize).
/// </summary>
public record IntegerTypeNode(IntegerKind Kind) : TypeNode
{
    public override string ToString() => Kind switch
    {
        IntegerKind.I8 => "i8",
        IntegerKind.I16 => "i16",
        IntegerKind.I32 => "i32",
        IntegerKind.I64 => "i64",
        IntegerKind.ISize => "isize",
        IntegerKind.U8 => "u8",
        IntegerKind.U16 => "u16",
        IntegerKind.U32 => "u32",
        IntegerKind.U64 => "u64",
        IntegerKind.USize => "usize",
        _ => throw new ArgumentOutOfRangeException()
    };
}

/// <summary>
/// Represents a floating-point type (f32, f64).
/// </summary>
public record FloatTypeNode(FloatKind Kind) : TypeNode
{
    public override string ToString() => Kind switch
    {
        FloatKind.F32 => "f32",
        FloatKind.F64 => "f64",
        _ => throw new ArgumentOutOfRangeException()
    };
}

/// <summary>
/// Represents the boolean type.
/// </summary>
public record BoolTypeNode() : TypeNode
{
    public override string ToString() => "bool";
}

/// <summary>
/// Represents an array type [N]T.
/// Note: Size expression is resolved during semantic analysis.
/// </summary>
public record ArrayTypeNode(TypeNode ElementType, ulong Size) : TypeNode
{
    public override string ToString() => $"[{Size}]{ElementType}";
}

/// <summary>
/// Represents a tuple type (T1, T2, ...).
/// </summary>
public record TupleTypeNode(IReadOnlyList<TypeNode> Elements) : TypeNode
{
    public override string ToString() => $"({string.Join(", ", Elements)})";
}

/// <summary>
/// Represents a reference type ^T or ^!T.
/// </summary>
public record RefTypeNode(TypeNode TargetType, bool IsMutable) : TypeNode
{
    public override string ToString() => IsMutable ? $"^!{TargetType}" : $"^{TargetType}";
}

/// <summary>
/// Represents a pointer type *T.
/// </summary>
public record PointerTypeNode(TypeNode TargetType) : TypeNode
{
    public override string ToString() => $"*{TargetType}";
}

/// <summary>
/// Represents a function type func(T1, T2, ...)calling_convention: R.
/// </summary>
public record FuncTypeNode(
    IReadOnlyList<TypeNode> ParameterTypes,
    TypeNode? ReturnType,
    CallingConvention? CallingConvention
) : TypeNode
{
    public override string ToString()
    {
        var paramsStr = string.Join(", ", ParameterTypes);
        var returnStr = ReturnType?.ToString() ?? "void";
        var convStr = CallingConvention.HasValue ? $" {CallingConvention.Value.ToString().ToLower()}" : "";
        return $"func({paramsStr}){convStr}: {returnStr}";
    }
}

/// <summary>
/// Represents a named type (struct, union, enum, variant) with optional namespace prefix and generic arguments.
/// </summary>
public record NamedTypeNode(
    IReadOnlyList<string>? NamespacePrefix,
    string Name,
    IReadOnlyList<TypeNode>? GenericArguments
) : TypeNode
{
    public override string ToString()
    {
        var ns = NamespacePrefix != null ? string.Join("::", NamespacePrefix) + "::" : "";
        var gen = GenericArguments != null ? $"<{string.Join(", ", GenericArguments)}>" : "";
        return $"{ns}{Name}{gen}";
    }

    public string FullyQualifiedName => NamespacePrefix != null
        ? $"{string.Join("::", NamespacePrefix)}::{Name}"
        : Name;
}
