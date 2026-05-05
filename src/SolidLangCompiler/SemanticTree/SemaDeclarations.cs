namespace SolidLangCompiler.SemanticTree;

/// <summary>
/// Represents a complete compilation unit (program).
/// </summary>
public class SemaProgram
{
    /// <summary>
    /// Namespace path (e.g., ["app", "module"]).
    /// </summary>
    public IReadOnlyList<string>? Namespace { get; init; }

    /// <summary>
    /// Using declarations.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>>? Usings { get; init; }

    /// <summary>
    /// All functions in the program.
    /// </summary>
    public IReadOnlyList<SemaFunction> Functions { get; init; } = [];

    /// <summary>
    /// All global variables/constants.
    /// </summary>
    public IReadOnlyList<SemaGlobal> Globals { get; init; } = [];

    /// <summary>
    /// All struct types.
    /// </summary>
    public IReadOnlyList<SemaStructType> Structs { get; init; } = [];

    /// <summary>
    /// All enum types.
    /// </summary>
    public IReadOnlyList<SemaEnumType> Enums { get; init; } = [];

    /// <summary>
    /// All union types.
    /// </summary>
    public IReadOnlyList<SemaUnionType> Unions { get; init; } = [];

    /// <summary>
    /// List of errors encountered during semantic analysis.
    /// </summary>
    public List<SemanticError> Errors { get; set; } = [];

    /// <summary>
    /// List of warnings.
    /// </summary>
    public List<SemanticError> Warnings { get; set; } = [];

    /// <summary>
    /// Whether semantic analysis was successful.
    /// </summary>
    public bool IsSuccessful => Errors.Count == 0;
}

/// <summary>
/// Represents a semantic error or warning.
/// </summary>
public record SemanticError(string Message, SourceLocation Location, bool IsWarning = false)
{
    public override string ToString()
    {
        var prefix = IsWarning ? "warning" : "error";
        return $"{Location}: {prefix}: {Message}";
    }
}

/// <summary>
/// Represents a function definition.
/// </summary>
public record SemaFunction : SemaNode
{
    /// <summary>
    /// Original function name (before mangling).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Mangled name for code generation.
    /// </summary>
    public required string MangledName { get; init; }

    /// <summary>
    /// Function parameters.
    /// </summary>
    public required IReadOnlyList<SemaParameter> Parameters { get; init; }

    /// <summary>
    /// Return type (null for void).
    /// </summary>
    public required SemaType? ReturnType { get; init; }

    /// <summary>
    /// Function body (null for external functions).
    /// </summary>
    public required SemaBlock? Body { get; init; }

    /// <summary>
    /// Whether this function is the entry point (main).
    /// </summary>
    public bool IsEntryPoint { get; init; }

    /// <summary>
    /// Number of local variables (for stack allocation).
    /// </summary>
    public int LocalCount { get; init; }

    public override string ToString()
    {
        var parms = string.Join(", ", Parameters);
        var ret = ReturnType?.ToString() ?? "void";
        return Body != null
            ? $"func {MangledName}({parms}) -> {ret} {Body}"
            : $"func {MangledName}({parms}) -> {ret};";
    }
}

/// <summary>
/// Represents a function parameter.
/// </summary>
public record SemaParameter : SemaNode
{
    public required string Name { get; init; }
    public required int Index { get; init; }
    public required SemaType Type { get; init; }
    public override string ToString() => $"{Name}: {Type}";
}

/// <summary>
/// Represents a global variable or constant.
/// </summary>
public record SemaGlobal : SemaNode
{
    public required string Name { get; init; }
    public required string MangledName { get; init; }
    public required SemaType Type { get; init; }
    public required SemaExpression? Initializer { get; init; }
    public required bool IsConstant { get; init; }
    public required bool IsPublic { get; init; }
    public override string ToString()
    {
        var keyword = IsConstant ? "const" : "static";
        var init = Initializer != null ? $" = {Initializer}" : "";
        return $"{keyword} {Name}: {Type}{init};";
    }
}

/// <summary>
/// Represents an enum type.
/// </summary>
public record SemaEnumType : SemaType
{
    public required string Name { get; init; }
    public required SemaType UnderlyingType { get; init; }
    public required IReadOnlyList<SemaEnumMember> Members { get; init; }

    public override int SizeBytes => UnderlyingType.SizeBytes;
    public override string ToString() => Name;
}

/// <summary>
/// Represents an enum member.
/// </summary>
public record SemaEnumMember(string Name, ulong Value);
