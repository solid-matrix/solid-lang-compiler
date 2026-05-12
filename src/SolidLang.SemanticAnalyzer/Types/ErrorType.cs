namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Internal sentinel type returned when type resolution fails.
/// Downstream passes can continue without crashing. Identified via "is ErrorType",
/// never via Kind dispatch — the Kind property throws.
/// </summary>
public sealed class ErrorType : SolidType
{
    public static readonly ErrorType Instance = new();

    public override SolidTypeKind Kind =>
        throw new InvalidOperationException("ErrorType is a sentinel; check with 'is ErrorType' instead of Kind dispatch");
    public override string DisplayName => "?error?";

    private ErrorType() { }

    public override bool Equals(object? obj) => obj is ErrorType;
    public override int GetHashCode() => 2;
}
