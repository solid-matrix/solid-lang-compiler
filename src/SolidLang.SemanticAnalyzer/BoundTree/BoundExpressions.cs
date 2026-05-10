using SolidLang.Parser;

namespace SolidLang.SemanticAnalyzer;

// ========================================
// Bound Expression nodes
// ========================================

/// <summary>
/// Abstract base for all bound expressions.
/// </summary>
public abstract class BoundExpression : BoundNode
{
}

/// <summary>
/// A literal value: integer, float, string, char, bool, null.
/// </summary>
public sealed class BoundLiteralExpr : BoundExpression
{
    public override BoundKind Kind => BoundKind.LiteralExpr;
    public override SolidType? Type { get; }
    public object Value { get; }

    public BoundLiteralExpr(SolidType? type, object value)
    {
        Type = type;
        Value = value;
    }
}

/// <summary>
/// A reference to a variable or parameter.
/// The Symbol's DeclaredType provides the type.
/// </summary>
public sealed class BoundVarExpr : BoundExpression
{
    public override BoundKind Kind => BoundKind.VarExpr;
    public VariableSymbol Symbol { get; }

    public BoundVarExpr(VariableSymbol symbol) { Symbol = symbol; }
}

/// <summary>
/// A function call: func(args).
/// </summary>
public sealed class BoundCallExpr : BoundExpression
{
    public override BoundKind Kind => BoundKind.CallExpr;
    public FunctionSymbol Function { get; }
    public IReadOnlyList<BoundExpression> Arguments { get; }

    public BoundCallExpr(FunctionSymbol function, IReadOnlyList<BoundExpression> arguments)
    {
        Function = function;
        Arguments = arguments;
    }
}

/// <summary>
/// A binary expression: left op right.
/// </summary>
public sealed class BoundBinaryExpr : BoundExpression
{
    public override BoundKind Kind => BoundKind.BinaryExpr;
    public BoundExpression Left { get; }
    public SyntaxKind Operator { get; }
    public BoundExpression Right { get; }

    public BoundBinaryExpr(BoundExpression left, SyntaxKind op, BoundExpression right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }
}

/// <summary>
/// A unary expression: op operand.
/// </summary>
public sealed class BoundUnaryExpr : BoundExpression
{
    public override BoundKind Kind => BoundKind.UnaryExpr;
    public SyntaxKind Operator { get; }
    public BoundExpression Operand { get; }

    public BoundUnaryExpr(SyntaxKind op, BoundExpression operand)
    {
        Operator = op;
        Operand = operand;
    }
}

/// <summary>
/// A ternary conditional expression: cond ? then : else.
/// </summary>
public sealed class BoundConditionalExpr : BoundExpression
{
    public override BoundKind Kind => BoundKind.ConditionalExpr;
    public BoundExpression Condition { get; }
    public BoundExpression ThenExpr { get; }
    public BoundExpression ElseExpr { get; }

    public BoundConditionalExpr(BoundExpression condition, BoundExpression thenExpr, BoundExpression elseExpr)
    {
        Condition = condition;
        ThenExpr = thenExpr;
        ElseExpr = elseExpr;
    }
}

/// <summary>
/// A member access: receiver.member. The member is resolved to a MemberSymbol.
/// </summary>
public sealed class BoundMemberAccessExpr : BoundExpression
{
    public override BoundKind Kind => BoundKind.MemberAccessExpr;
    public BoundExpression Receiver { get; }
    public MemberSymbol Member { get; }

    public BoundMemberAccessExpr(BoundExpression receiver, MemberSymbol member)
    {
        Receiver = receiver;
        Member = member;
    }
}

/// <summary>
/// An index access: receiver[index].
/// </summary>
public sealed class BoundIndexAccessExpr : BoundExpression
{
    public override BoundKind Kind => BoundKind.IndexAccessExpr;
    public BoundExpression Receiver { get; }
    public BoundExpression Index { get; }

    public BoundIndexAccessExpr(BoundExpression receiver, BoundExpression index)
    {
        Receiver = receiver;
        Index = index;
    }
}

/// <summary>
/// A struct literal: StructType { field1: val1, field2: val2 }.
/// </summary>
public sealed class BoundStructLiteralExpr : BoundExpression
{
    public override BoundKind Kind => BoundKind.StructLiteralExpr;
    public TypeSymbol StructType { get; }
    public IReadOnlyList<(MemberSymbol Field, BoundExpression Value)> Fields { get; }

    public BoundStructLiteralExpr(TypeSymbol structType,
        IReadOnlyList<(MemberSymbol Field, BoundExpression Value)> fields)
    {
        StructType = structType;
        Fields = fields;
    }
}

/// <summary>
/// A union literal: UnionType.member(expr).
/// </summary>
public sealed class BoundUnionLiteralExpr : BoundExpression
{
    public override BoundKind Kind => BoundKind.UnionLiteralExpr;
    public TypeSymbol UnionType { get; }
    public MemberSymbol Member { get; }
    public BoundExpression? Value { get; }

    public BoundUnionLiteralExpr(TypeSymbol unionType, MemberSymbol member, BoundExpression? value)
    {
        UnionType = unionType;
        Member = member;
        Value = value;
    }
}

/// <summary>
/// An enum literal: EnumType.member.
/// </summary>
public sealed class BoundEnumLiteralExpr : BoundExpression
{
    public override BoundKind Kind => BoundKind.EnumLiteralExpr;
    public TypeSymbol EnumType { get; }
    public MemberSymbol Member { get; }

    public BoundEnumLiteralExpr(TypeSymbol enumType, MemberSymbol member)
    {
        EnumType = enumType;
        Member = member;
    }
}

/// <summary>
/// A variant literal: VariantType.member(expr).
/// </summary>
public sealed class BoundVariantLiteralExpr : BoundExpression
{
    public override BoundKind Kind => BoundKind.VariantLiteralExpr;
    public TypeSymbol VariantType { get; }
    public MemberSymbol Member { get; }
    public BoundExpression? Value { get; }

    public BoundVariantLiteralExpr(TypeSymbol variantType, MemberSymbol member, BoundExpression? value)
    {
        VariantType = variantType;
        Member = member;
        Value = value;
    }
}

/// <summary>
/// A switch pattern: literal, named-type member, named-type member with binding, or identifier capture.
/// </summary>
public sealed class BoundSwitchPattern : BoundNode
{
    public override BoundKind Kind => BoundKind.SwitchPattern;

    public SwitchPatternKind PatternKind { get; }

    // Literal pattern
    public BoundLiteralExpr? Literal { get; }

    // Named-type member pattern (enum/variant member)
    public TypeSymbol? NamedTypeSymbol { get; }
    public MemberSymbol? MemberSymbol { get; }

    // Binding for NamedTypeMemberBinding
    public BoundExpression? Binding { get; }

    // Identifier capture pattern
    public VariableSymbol? CaptureVariable { get; }

    public BoundSwitchPattern(SwitchPatternKind patternKind,
        BoundLiteralExpr? literal = null,
        TypeSymbol? namedTypeSymbol = null,
        MemberSymbol? memberSymbol = null,
        BoundExpression? binding = null,
        VariableSymbol? captureVariable = null)
    {
        PatternKind = patternKind;
        Literal = literal;
        NamedTypeSymbol = namedTypeSymbol;
        MemberSymbol = memberSymbol;
        Binding = binding;
        CaptureVariable = captureVariable;
    }
}

/// <summary>
/// Kinds of switch patterns.
/// </summary>
public enum SwitchPatternKind
{
    Literal,
    NamedTypeMember,
    NamedTypeMemberBinding,
    Identifier,
}
