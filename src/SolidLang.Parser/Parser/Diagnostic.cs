namespace SolidLang.Parser.Parser;

/// <summary>
/// Represents a diagnostic message (error, warning, or info).
/// </summary>
public sealed class Diagnostic
{
    public TextSpan Span { get; }
    public string Message { get; }
    public DiagnosticSeverity Severity { get; }
    public string? Code { get; }

    public Diagnostic(TextSpan span, string message, DiagnosticSeverity severity, string? code = null)
    {
        Span = span;
        Message = message;
        Severity = severity;
        Code = code;
    }

    public override string ToString()
    {
        var prefix = Severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Info => "info",
            _ => "unknown"
        };

        var codePart = Code != null ? $" {Code}" : "";
        return $"{prefix}{codePart}: {Message} at {Span}";
    }
}

public enum DiagnosticSeverity
{
    Error,
    Warning,
    Info,
}
