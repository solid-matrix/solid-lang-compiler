using SolidLang.Parser;
using SolidLang.Parser.Parser;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Extension methods on DiagnosticBag for semantic analysis errors.
/// </summary>
public static class SemanticDiagnostics
{
    public static void UndefinedName(this DiagnosticBag bag, string name, TextSpan span)
    {
        bag.Report(span, $"Undefined name: '{name}'", DiagnosticSeverity.Error, "SM001");
    }

    public static void DuplicateName(this DiagnosticBag bag, string name, string kind, TextSpan span, TextSpan firstSpan)
    {
        bag.Report(span, $"Duplicate {kind} name: '{name}'", DiagnosticSeverity.Error, "SM002");
        bag.Report(firstSpan, $"First declared here", DiagnosticSeverity.Info, "SM002");
    }

    public static void AmbiguousName(this DiagnosticBag bag, string name, TextSpan span)
    {
        bag.Report(span, $"Ambiguous name: '{name}' found in multiple imports", DiagnosticSeverity.Error, "SM003");
    }

    public static void NotAType(this DiagnosticBag bag, string name, TextSpan span)
    {
        bag.Report(span, $"'{name}' is not a type", DiagnosticSeverity.Error, "SM004");
    }

    public static void KindMismatch(this DiagnosticBag bag, string name, string expectedKind, string actualKind, TextSpan span)
    {
        bag.Report(span, $"'{name}' is a {actualKind}, not a {expectedKind}", DiagnosticSeverity.Error, "SM005");
    }

    public static void ForwardDeclMismatch(this DiagnosticBag bag, string name, TextSpan span)
    {
        bag.Report(span, $"Forward declaration of '{name}' does not match the definition", DiagnosticSeverity.Error, "SM006");
    }

    /// <summary>
    /// Low-level report method — used by the extension methods above.
    /// Made internal so DiagnosticBag itself doesn't need to change.
    /// </summary>
    internal static void Report(this DiagnosticBag bag, TextSpan span, string message, DiagnosticSeverity severity, string code)
    {
        bag.Add(new Diagnostic(span, message, severity, code));
    }
}
