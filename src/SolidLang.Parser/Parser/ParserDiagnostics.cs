namespace SolidLang.Parser.Parser;

/// <summary>
/// Provides methods for reporting diagnostics during parsing.
/// </summary>
public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics = new();

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    public void Add(Diagnostic diagnostic)
    {
        _diagnostics.Add(diagnostic);
    }

    public void AddRange(IEnumerable<Diagnostic> diagnostics)
    {
        _diagnostics.AddRange(diagnostics);
    }

    public int Count => _diagnostics.Count;

    public void TruncateTo(int count)
    {
        if (count < _diagnostics.Count)
            _diagnostics.RemoveRange(count, _diagnostics.Count - count);
    }

    // ========================================
    // Lexical Errors
    // ========================================

    public void BadCharacter(char c, TextSpan span)
    {
        Report(span, $"Bad character: '{c}'", DiagnosticSeverity.Error, "SL001");
    }

    public void UnterminatedString(TextSpan span)
    {
        Report(span, "Unterminated string literal", DiagnosticSeverity.Error, "SL002");
    }

    public void UnterminatedChar(TextSpan span)
    {
        Report(span, "Unterminated character literal", DiagnosticSeverity.Error, "SL003");
    }

    public void EmptyCharLiteral(TextSpan span)
    {
        Report(span, "Empty character literal", DiagnosticSeverity.Error, "SL004");
    }

    public void MultiCharLiteral(TextSpan span)
    {
        Report(span, "Multi-character literal; use a string instead", DiagnosticSeverity.Error, "SL008");
    }

    public void InvalidEscapeSequence(string sequence, TextSpan span)
    {
        Report(span, $"Invalid escape sequence: '{sequence}'", DiagnosticSeverity.Error, "SL005");
    }

    public void InvalidNumericFormat(string text, TextSpan span)
    {
        Report(span, $"Invalid numeric format: '{text}'", DiagnosticSeverity.Error, "SL006");
    }

    public void InvalidUppercasePrefix(string prefix, TextSpan span)
    {
        Report(span, $"Invalid uppercase prefix '{prefix}'; use lowercase '{prefix.ToLowerInvariant()}' instead", DiagnosticSeverity.Error, "SL007");
    }

    // ========================================
    // Syntax Errors
    // ========================================

    public void ExpectedToken(SyntaxKind expected, SyntaxKind actual, TextSpan span)
    {
        var expectedText = SyntaxFacts.GetTokenText(expected) ?? SyntaxFacts.GetKeywordText(expected) ?? expected.ToString();
        Report(span, $"Expected '{expectedText}', but got '{actual}'", DiagnosticSeverity.Error, "SS001");
    }

    public void ExpectedIdentifier(TextSpan span)
    {
        Report(span, "Expected identifier", DiagnosticSeverity.Error, "SS002");
    }

    public void ExpectedType(TextSpan span)
    {
        Report(span, "Expected type", DiagnosticSeverity.Error, "SS003");
    }

    public void ExpectedExpression(TextSpan span)
    {
        Report(span, "Expected expression", DiagnosticSeverity.Error, "SS004");
    }

    public void ExpectedStatement(TextSpan span)
    {
        Report(span, "Expected statement", DiagnosticSeverity.Error, "SS005");
    }

    public void ExpectedDeclaration(TextSpan span)
    {
        Report(span, "Expected declaration", DiagnosticSeverity.Error, "SS006");
    }

    public void UnexpectedToken(SyntaxKind kind, TextSpan span)
    {
        var text = SyntaxFacts.GetTokenText(kind) ?? SyntaxFacts.GetKeywordText(kind) ?? kind.ToString();
        Report(span, $"Unexpected token: '{text}'", DiagnosticSeverity.Error, "SS007");
    }

    public void MissingSemicolon(TextSpan span)
    {
        Report(span, "Missing semicolon", DiagnosticSeverity.Error, "SS008");
    }

    public void MissingCloseBrace(TextSpan span)
    {
        Report(span, "Missing closing brace", DiagnosticSeverity.Error, "SS009");
    }

    public void MissingCloseBracket(TextSpan span)
    {
        Report(span, "Missing closing bracket", DiagnosticSeverity.Error, "SS010");
    }

    public void MissingCloseParen(TextSpan span)
    {
        Report(span, "Missing closing parenthesis", DiagnosticSeverity.Error, "SS011");
    }

    public void MissingGreaterThan(TextSpan span)
    {
        Report(span, "Missing '>' to close generic argument list", DiagnosticSeverity.Error, "SS012");
    }

    public void DuplicateModifier(string modifier, TextSpan span)
    {
        Report(span, $"Duplicate modifier: '{modifier}'", DiagnosticSeverity.Warning, "SS013");
    }

    public void ExcessiveRecursion(TextSpan span)
    {
        Report(span, "Excessive recursion depth; possible stack overflow prevented", DiagnosticSeverity.Error, "SS014");
    }

    // ========================================
    // Semantic Errors (placeholder for future)
    // ========================================

    public void UndefinedType(string name, TextSpan span)
    {
        Report(span, $"Undefined type: '{name}'", DiagnosticSeverity.Error, "SM001");
    }

    public void UndefinedIdentifier(string name, TextSpan span)
    {
        Report(span, $"Undefined identifier: '{name}'", DiagnosticSeverity.Error, "SM002");
    }

    public void TypeMismatch(string expected, string actual, TextSpan span)
    {
        Report(span, $"Type mismatch: expected '{expected}', but got '{actual}'", DiagnosticSeverity.Error, "SM003");
    }

    // ========================================
    // Private helper
    // ========================================

    private void Report(TextSpan span, string message, DiagnosticSeverity severity, string code)
    {
        _diagnostics.Add(new Diagnostic(span, message, severity, code));
    }
}
