namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Sentinel type returned when type resolution fails.
/// Downstream passes can continue without crashing.
/// </summary>
public sealed class ErrorType : SolidType
{
    public static readonly ErrorType Instance = new();

    public override SolidTypeKind Kind => SolidTypeKind.Error;
    public override string DisplayName => "?error?";

    private ErrorType() { }

    public override bool Equals(object? obj) => obj is ErrorType;
    public override int GetHashCode() => 2;
}
