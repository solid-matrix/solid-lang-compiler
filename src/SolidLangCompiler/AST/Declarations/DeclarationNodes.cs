using SolidLangCompiler.AST.Expressions;
using SolidLangCompiler.AST.Statements;
using SolidLangCompiler.AST.Types;

namespace SolidLangCompiler.AST.Declarations;

/// <summary>
/// Represents a variable declaration (var name: type = value;).
/// </summary>
public record VarDeclarationNode(
    IReadOnlyList<AnnotationNode>? Annotations,
    string Name,
    TypeNode? Type,
    ExpressionNode? Initializer
) : DeclarationNode
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        var type = Type != null ? $": {Type}" : "";
        var init = Initializer != null ? $" = {Initializer}" : "";
        return $"{annots}var {Name}{type}{init};";
    }
}

/// <summary>
/// Represents a constant declaration (const name: type = value;).
/// </summary>
public record ConstDeclarationNode(
    IReadOnlyList<AnnotationNode>? Annotations,
    string Name,
    TypeNode Type,
    ExpressionNode Initializer
) : DeclarationNode
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        return $"{annots}const {Name}: {Type} = {Initializer};";
    }
}

/// <summary>
/// Represents a static variable declaration (static name: type = value;).
/// </summary>
public record StaticDeclarationNode(
    IReadOnlyList<AnnotationNode>? Annotations,
    string Name,
    TypeNode Type,
    ExpressionNode Initializer
) : DeclarationNode
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        return $"{annots}static {Name}: {Type} = {Initializer};";
    }
}

/// <summary>
/// Represents a struct declaration.
/// </summary>
public record StructDeclarationNode(
    IReadOnlyList<AnnotationNode>? Annotations,
    string Name,
    IReadOnlyList<string>? GenericParameters,
    IReadOnlyList<WhereClauseNode>? WhereClauses,
    IReadOnlyList<StructFieldNode>? Fields
) : DeclarationNode
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        var genParams = GenericParameters != null ? $"<{string.Join(", ", GenericParameters)}>" : "";
        var whereClauses = WhereClauses != null ? " " + string.Join(" ", WhereClauses) : "";
        if (Fields == null || Fields.Count == 0)
            return $"{annots}struct {Name}{genParams}{whereClauses};";

        var fields = string.Join(",\n  ", Fields);
        return $"{annots}struct {Name}{genParams}{whereClauses} {{\n  {fields}\n}}";
    }
}

/// <summary>
/// Represents a field in a struct.
/// </summary>
public record StructFieldNode(IReadOnlyList<AnnotationNode>? Annotations, string Name, TypeNode Type)
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        return $"{annots}{Name}: {Type}";
    }
}

/// <summary>
/// Represents an enum declaration.
/// </summary>
public record EnumDeclarationNode(
    IReadOnlyList<AnnotationNode>? Annotations,
    string Name,
    TypeNode? UnderlyingType,
    IReadOnlyList<EnumFieldNode>? Fields
) : DeclarationNode
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        var underlying = UnderlyingType != null ? $": {UnderlyingType}" : "";
        if (Fields == null || Fields.Count == 0)
            return $"{annots}enum {Name}{underlying} {{}}";

        var fields = string.Join(",\n  ", Fields);
        return $"{annots}enum {Name}{underlying} {{\n  {fields}\n}}";
    }
}

/// <summary>
/// Represents a field in an enum.
/// </summary>
public record EnumFieldNode(IReadOnlyList<AnnotationNode>? Annotations, string Name, ExpressionNode? Value)
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        var value = Value != null ? $" = {Value}" : "";
        return $"{annots}{Name}{value}";
    }
}

/// <summary>
/// Represents a union declaration.
/// </summary>
public record UnionDeclarationNode(
    IReadOnlyList<AnnotationNode>? Annotations,
    string Name,
    IReadOnlyList<string>? GenericParameters,
    IReadOnlyList<WhereClauseNode>? WhereClauses,
    IReadOnlyList<UnionFieldNode>? Fields
) : DeclarationNode
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        var genParams = GenericParameters != null ? $"<{string.Join(", ", GenericParameters)}>" : "";
        var whereClauses = WhereClauses != null ? " " + string.Join(" ", WhereClauses) : "";
        if (Fields == null || Fields.Count == 0)
            return $"{annots}union {Name}{genParams}{whereClauses};";

        var fields = string.Join(",\n  ", Fields);
        return $"{annots}union {Name}{genParams}{whereClauses} {{\n  {fields}\n}}";
    }
}

/// <summary>
/// Represents a field in a union.
/// </summary>
public record UnionFieldNode(IReadOnlyList<AnnotationNode>? Annotations, string Name, TypeNode Type)
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        return $"{annots}{Name}: {Type}";
    }
}

/// <summary>
/// Represents a variant declaration.
/// </summary>
public record VariantDeclarationNode(
    IReadOnlyList<AnnotationNode>? Annotations,
    string Name,
    IReadOnlyList<string>? GenericParameters,
    TypeNode? TagType,
    IReadOnlyList<WhereClauseNode>? WhereClauses,
    IReadOnlyList<VariantFieldNode>? Fields
) : DeclarationNode
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        var genParams = GenericParameters != null ? $"<{string.Join(", ", GenericParameters)}>" : "";
        var tagType = TagType != null ? $": {TagType}" : "";
        var whereClauses = WhereClauses != null ? " " + string.Join(" ", WhereClauses) : "";
        if (Fields == null || Fields.Count == 0)
            return $"{annots}variant {Name}{genParams}{tagType}{whereClauses};";

        var fields = string.Join(",\n  ", Fields);
        return $"{annots}variant {Name}{genParams}{tagType}{whereClauses} {{\n  {fields}\n}}";
    }
}

/// <summary>
/// Represents a field in a variant.
/// </summary>
public record VariantFieldNode(IReadOnlyList<AnnotationNode>? Annotations, string Name, TypeNode? Type)
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        return Type != null ? $"{annots}{Name}: {Type}" : $"{annots}{Name}";
    }
}

/// <summary>
/// Represents an interface declaration.
/// </summary>
public record InterfaceDeclarationNode(
    IReadOnlyList<AnnotationNode>? Annotations,
    string Name,
    IReadOnlyList<string>? GenericParameters,
    IReadOnlyList<WhereClauseNode>? WhereClauses,
    IReadOnlyList<InterfaceFieldNode>? Fields
) : DeclarationNode
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        var genParams = GenericParameters != null ? $"<{string.Join(", ", GenericParameters)}>" : "";
        var whereClauses = WhereClauses != null ? " " + string.Join(" ", WhereClauses) : "";
        if (Fields == null || Fields.Count == 0)
            return $"{annots}interface {Name}{genParams}{whereClauses} {{}}";

        var fields = string.Join("\n  ", Fields);
        return $"{annots}interface {Name}{genParams}{whereClauses} {{\n  {fields}\n}}";
    }
}

/// <summary>
/// Represents a function signature in an interface.
/// </summary>
public record InterfaceFieldNode(
    IReadOnlyList<AnnotationNode>? Annotations,
    string Name,
    IReadOnlyList<FuncParameterNode>? Parameters,
    TypeNode? ReturnType
)
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        var params_ = Parameters != null ? string.Join(", ", Parameters) : "";
        var ret = ReturnType != null ? $": {ReturnType}" : "";
        return $"{annots}func {Name}({params_}){ret};";
    }
}

/// <summary>
/// Represents a function declaration.
/// </summary>
public record FuncDeclarationNode(
    IReadOnlyList<AnnotationNode>? Annotations,
    IReadOnlyList<string>? NamespacePrefix,
    string Name,
    IReadOnlyList<string>? GenericParameters,
    IReadOnlyList<FuncParameterNode>? Parameters,
    CallingConvention? CallingConvention,
    TypeNode? ReturnType,
    IReadOnlyList<WhereClauseNode>? WhereClauses,
    BlockStatementNode? Body
) : DeclarationNode
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        var ns = NamespacePrefix != null ? string.Join("::", NamespacePrefix) + "::" : "";
        var genParams = GenericParameters != null ? $"<{string.Join(", ", GenericParameters)}>" : "";
        var params_ = Parameters != null ? string.Join(", ", Parameters) : "";
        var conv = CallingConvention.HasValue ? $" {CallingConvention.Value.ToString().ToLower()}" : "";
        var ret = ReturnType != null ? $": {ReturnType}" : "";
        var whereClauses = WhereClauses != null ? " " + string.Join(" ", WhereClauses) : "";

        if (Body == null)
            return $"{annots}func {ns}{Name}{genParams}({params_}){conv}{ret}{whereClauses};";

        return $"{annots}func {ns}{Name}{genParams}({params_}){conv}{ret}{whereClauses} {Body}";
    }
}

/// <summary>
/// Represents a function parameter.
/// </summary>
public record FuncParameterNode(IReadOnlyList<AnnotationNode>? Annotations, string Name, TypeNode Type)
{
    public override string ToString()
    {
        var annots = Annotations != null ? string.Join(" ", Annotations) + " " : "";
        return $"{annots}{Name}: {Type}";
    }
}

/// <summary>
/// Represents a where clause (where T: ICloneable).
/// </summary>
public record WhereClauseNode(string TypeParameter, TypeNode ConstraintType)
{
    public override string ToString() => $"where {TypeParameter}: {ConstraintType}";
}

/// <summary>
/// Represents a namespace declaration.
/// </summary>
public record NamespaceDeclarationNode(IReadOnlyList<string> Path) : DeclarationNode
{
    public override string ToString() => $"namespace {string.Join("::", Path)};";
}

/// <summary>
/// Represents a using declaration.
/// </summary>
public record UsingDeclarationNode(IReadOnlyList<string> Path) : DeclarationNode
{
    public override string ToString() => $"using {string.Join("::", Path)};";
}
