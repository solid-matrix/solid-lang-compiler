using System.Text;

namespace SolidLang.Parser.Parser;

/// <summary>
/// Literal scanning methods for the parser.
/// </summary>
public sealed partial class Parser
{
    // ========================================
    // Integer Literals
    // ========================================

    /// <summary>
    /// Scans an integer literal.
    /// </summary>
    private (string Text, object Value, SyntaxKind? TypeSuffix) ScanIntegerLiteral()
    {
        var start = _position;
        var spanStart = start;

        // Check for prefix (hex, octal, binary)
        if (Current == '0')
        {
            var prefix = Peek();
            if (prefix == 'x' || prefix == 'X')
            {
                return ScanHexLiteral(spanStart);
            }
            else if (prefix == 'o' || prefix == 'O')
            {
                return ScanOctalLiteral(spanStart);
            }
            else if (prefix == 'b' || prefix == 'B')
            {
                return ScanBinaryLiteral(spanStart);
            }
        }

        // Decimal literal
        return ScanDecimalLiteral(spanStart);
    }

    private (string Text, object Value, SyntaxKind? TypeSuffix) ScanDecimalLiteral(int spanStart)
    {
        var sb = new StringBuilder();

        // First digit
        if (Current >= '1' && Current <= '9')
        {
            sb.Append(AdvanceAndReturn());
        }
        else if (Current == '0')
        {
            sb.Append(AdvanceAndReturn());
        }

        // Remaining digits (with optional underscores)
        while (char.IsDigit(Current) || Current == '_')
        {
            if (Current == '_')
            {
                Advance();
                continue;
            }
            sb.Append(AdvanceAndReturn());
        }

        // Check for type suffix
        var typeSuffix = TryScanIntegerTypeSuffix();
        var text = _source.GetText(spanStart, _position - spanStart);

        // Parse value
        var valueStr = sb.ToString();
        if (!ulong.TryParse(valueStr, out var value))
        {
            _diagnostics.InvalidNumericFormat(text, new TextSpan(spanStart, _position - spanStart));
            return (text, 0UL, typeSuffix);
        }

        return (text, value, typeSuffix);
    }

    private (string Text, object Value, SyntaxKind? TypeSuffix) ScanHexLiteral(int spanStart)
    {
        Advance(); Advance(); // Skip 0x

        var sb = new StringBuilder();
        while (IsHexDigit(Current) || Current == '_')
        {
            if (Current == '_')
            {
                Advance();
                continue;
            }
            sb.Append(AdvanceAndReturn());
        }

        var typeSuffix = TryScanIntegerTypeSuffix();
        var text = _source.GetText(spanStart, _position - spanStart);

        var valueStr = sb.ToString();
        if (!ulong.TryParse(valueStr, System.Globalization.NumberStyles.HexNumber, null, out var value))
        {
            _diagnostics.InvalidNumericFormat(text, new TextSpan(spanStart, _position - spanStart));
            return (text, 0UL, typeSuffix);
        }

        return (text, value, typeSuffix);
    }

    private (string Text, object Value, SyntaxKind? TypeSuffix) ScanOctalLiteral(int spanStart)
    {
        Advance(); Advance(); // Skip 0o

        var sb = new StringBuilder();
        while (IsOctalDigit(Current) || Current == '_')
        {
            if (Current == '_')
            {
                Advance();
                continue;
            }
            sb.Append(AdvanceAndReturn());
        }

        var typeSuffix = TryScanIntegerTypeSuffix();
        var text = _source.GetText(spanStart, _position - spanStart);

        var valueStr = sb.ToString();
        try
        {
            var value = Convert.ToUInt64(valueStr, 8);
            return (text, value, typeSuffix);
        }
        catch
        {
            _diagnostics.InvalidNumericFormat(text, new TextSpan(spanStart, _position - spanStart));
            return (text, 0UL, typeSuffix);
        }
    }

    private (string Text, object Value, SyntaxKind? TypeSuffix) ScanBinaryLiteral(int spanStart)
    {
        Advance(); Advance(); // Skip 0b

        var sb = new StringBuilder();
        while ((Current == '0' || Current == '1' || Current == '_'))
        {
            if (Current == '_')
            {
                Advance();
                continue;
            }
            sb.Append(AdvanceAndReturn());
        }

        var typeSuffix = TryScanIntegerTypeSuffix();
        var text = _source.GetText(spanStart, _position - spanStart);

        var valueStr = sb.ToString();
        try
        {
            var value = Convert.ToUInt64(valueStr, 2);
            return (text, value, typeSuffix);
        }
        catch
        {
            _diagnostics.InvalidNumericFormat(text, new TextSpan(spanStart, _position - spanStart));
            return (text, 0UL, typeSuffix);
        }
    }

    private SyntaxKind? TryScanIntegerTypeSuffix()
    {
        var suffixStart = _position;

        // Check for usize/u or isize/i shorthand
        if (Current == 'u' && !char.IsLetterOrDigit(Peek()) && Peek() != '_')
        {
            Advance();
            return SyntaxKind.USizeKeyword;
        }
        if (Current == 'i' && !char.IsLetterOrDigit(Peek()) && Peek() != '_')
        {
            Advance();
            return SyntaxKind.ISizeKeyword;
        }

        // Check for longer suffixes
        var suffixText = ScanIdentifier();
        if (string.IsNullOrEmpty(suffixText))
            return null;

        return SyntaxFacts.GetIntegerTypeSuffix(suffixText);
    }

    private static bool IsHexDigit(char c) =>
        char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    private static bool IsOctalDigit(char c) => c >= '0' && c <= '7';

    // ========================================
    // Float Literals
    // ========================================

    /// <summary>
    /// Scans a float literal.
    /// </summary>
    private (string Text, object Value, SyntaxKind? TypeSuffix) ScanFloatLiteral(int intPartStart, string intPart)
    {
        var sb = new StringBuilder(intPart);

        // Fractional part
        if (Current == '.')
        {
            sb.Append(AdvanceAndReturn());

            while (char.IsDigit(Current) || Current == '_')
            {
                if (Current == '_')
                {
                    Advance();
                    continue;
                }
                sb.Append(AdvanceAndReturn());
            }
        }

        // Exponent
        if (Current == 'e' || Current == 'E')
        {
            sb.Append(AdvanceAndReturn());

            if (Current == '+' || Current == '-')
                sb.Append(AdvanceAndReturn());

            while (char.IsDigit(Current) || Current == '_')
            {
                if (Current == '_')
                {
                    Advance();
                    continue;
                }
                sb.Append(AdvanceAndReturn());
            }
        }

        // Type suffix
        var typeSuffix = TryScanFloatTypeSuffix();
        var text = _source.GetText(intPartStart, _position - intPartStart);

        var valueStr = sb.ToString().Replace("_", "");
        if (!double.TryParse(valueStr, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            _diagnostics.InvalidNumericFormat(text, new TextSpan(intPartStart, _position - intPartStart));
            return (text, 0.0, typeSuffix);
        }

        return (text, value, typeSuffix);
    }

    private SyntaxKind? TryScanFloatTypeSuffix()
    {
        var suffixText = ScanIdentifier();
        if (string.IsNullOrEmpty(suffixText))
            return null;

        return SyntaxFacts.GetFloatTypeSuffix(suffixText);
    }

    // ========================================
    // String Literals
    // ========================================

    /// <summary>
    /// Scans a string literal.
    /// </summary>
    private (string Text, string Value) ScanStringLiteral()
    {
        var start = _position;
        Advance(); // Skip opening quote

        var sb = new StringBuilder();

        while (Current != '"' && Current != '\0')
        {
            if (Current == '\\')
            {
                sb.Append(ScanEscapeSequence());
            }
            else
            {
                sb.Append(AdvanceAndReturn());
            }
        }

        if (Current == '\0')
        {
            _diagnostics.UnterminatedString(new TextSpan(start, _position - start));
        }
        else
        {
            Advance(); // Skip closing quote
        }

        var text = _source.GetText(start, _position - start);
        return (text, sb.ToString());
    }

    // ========================================
    // Char Literals
    // ========================================

    /// <summary>
    /// Scans a character literal.
    /// </summary>
    private (string Text, string Value) ScanCharLiteral()
    {
        var start = _position;
        Advance(); // Skip opening quote

        if (Current == '\'')
        {
            _diagnostics.EmptyCharLiteral(new TextSpan(start, 1));
            Advance(); // Skip closing quote
            var emptyText = _source.GetText(start, _position - start);
            return (emptyText, "");
        }

        string value;
        if (Current == '\\')
        {
            value = ScanEscapeSequence();
        }
        else
        {
            value = AdvanceAndReturn().ToString();
        }

        if (Current != '\'')
        {
            _diagnostics.UnterminatedChar(new TextSpan(start, _position - start));
            // Try to recover
            while (Current != '\'' && Current != '\0' && Current != '\n')
                Advance();
        }

        if (Current == '\'')
            Advance(); // Skip closing quote

        var text = _source.GetText(start, _position - start);
        return (text, value);
    }

    /// <summary>
    /// Scans an escape sequence and returns the escaped character.
    /// </summary>
    private string ScanEscapeSequence()
    {
        Advance(); // Skip backslash

        var c = Current;
        Advance();

        return c switch
        {
            'n' => "\n",
            'r' => "\r",
            't' => "\t",
            '"' => "\"",
            '\'' => "'",
            '\\' => "\\",
            '0' => "\0",
            'x' => ScanHexEscape(),
            'u' => ScanUnicodeEscape(),
            _ => ScanInvalidEscape(c),
        };
    }

    private string ScanHexEscape()
    {
        if (!IsHexDigit(Current) || !IsHexDigit(Peek()))
        {
            _diagnostics.InvalidEscapeSequence("\\x", GetCurrentSpan());
            return "?";
        }

        var hex = $"{AdvanceAndReturn()}{AdvanceAndReturn()}";
        var value = Convert.ToByte(hex, 16);
        return ((char)value).ToString();
    }

    private string ScanUnicodeEscape()
    {
        var hex = "";
        for (int i = 0; i < 4; i++)
        {
            if (!IsHexDigit(Current))
            {
                _diagnostics.InvalidEscapeSequence("\\u", GetCurrentSpan());
                return "?";
            }
            hex += AdvanceAndReturn();
        }

        var codePoint = Convert.ToUInt16(hex, 16);
        return ((char)codePoint).ToString();
    }

    private string ScanInvalidEscape(char c)
    {
        _diagnostics.InvalidEscapeSequence($"\\{c}", GetCurrentSpan());
        return c.ToString();
    }
}
