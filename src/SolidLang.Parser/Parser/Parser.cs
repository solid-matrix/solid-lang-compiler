using System.Text;
using SolidLang.Parser.Nodes;
using SolidLang.Parser.Nodes.Declarations;
using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Literals;
using SolidLang.Parser.Nodes.Statements;
using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Parser;

/// <summary>
/// A unified recursive descent parser that combines lexical and syntactic analysis.
/// Handles the >> ambiguity in generic types naturally through context awareness.
/// </summary>
public sealed partial class Parser
{
    private readonly SourceText _source;
    private int _position;
    private int _line = 1;
    private int _column = 1;

    /// <summary>
    /// Current depth of generic type argument lists.
    /// Used to resolve the >> ambiguity: inside generics, >> is treated as two > tokens.
    /// </summary>
    private int _genericDepth;

    private int _recursionDepth;
    private const int MaxRecursionDepth = 256;

    private readonly DiagnosticBag _diagnostics = new();

    public Parser(SourceText source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.Diagnostics;
    public bool HasErrors => _diagnostics.HasErrors;

    // ========================================
    // Character-Level Methods
    // ========================================

    /// <summary>
    /// Gets the current character, or '\0' at end of file.
    /// </summary>
    private char Current => _position < _source.Length ? _source[_position] : '\0';

    /// <summary>
    /// Peeks at a character at the specified offset from current position.
    /// </summary>
    private char Peek(int offset = 1)
    {
        var pos = _position + offset;
        return pos < _source.Length ? _source[pos] : '\0';
    }

    /// <summary>
    /// Advances to the next character.
    /// </summary>
    private void Advance()
    {
        if (Current == '\n')
        {
            _line++;
            _column = 1;
        }
        else if (char.IsHighSurrogate(Current) && char.IsLowSurrogate(Peek()))
        {
            // Supplementary-plane character (surrogate pair) — count as one column
            _position += 2;
            _column++;
            return;
        }
        else
        {
            _column++;
        }
        _position++;
    }

    /// <summary>
    /// Advances and returns the consumed character (or surrogate pair as a string).
    /// </summary>
    private string AdvanceAndReturn()
    {
        if (char.IsHighSurrogate(Current) && char.IsLowSurrogate(Peek()))
        {
            var pair = new string(new[] { Current, Peek() });
            Advance();
            return pair;
        }
        var c = Current;
        Advance();
        return c.ToString();
    }

    /// <summary>
    /// Matches the current character and advances if it matches.
    /// </summary>
    private bool Match(char expected)
    {
        if (Current == expected)
        {
            Advance();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Matches the current character sequence and advances if it matches.
    /// </summary>
    private bool Match(string expected)
    {
        if (_position + expected.Length > _source.Length)
            return false;

        for (int i = 0; i < expected.Length; i++)
        {
            if (_source[_position + i] != expected[i])
                return false;
        }

        for (int i = 0; i < expected.Length; i++)
            Advance();

        return true;
    }

    /// <summary>
    /// Expects a character; reports an error if not found.
    /// </summary>
    private void Expect(char expected, string? message = null)
    {
        if (!Match(expected))
        {
            var span = GetCurrentSpan();
            _diagnostics.ExpectedToken(
                GetTokenKindForChar(expected),
                SyntaxKind.BadToken,
                span);
        }
    }

    private static SyntaxKind GetTokenKindForChar(char c)
    {
        return c switch
        {
            '{' => SyntaxKind.OpenBraceToken,
            '}' => SyntaxKind.CloseBraceToken,
            '[' => SyntaxKind.OpenBracketToken,
            ']' => SyntaxKind.CloseBracketToken,
            '(' => SyntaxKind.OpenParenToken,
            ')' => SyntaxKind.CloseParenToken,
            ';' => SyntaxKind.SemicolonToken,
            ':' => SyntaxKind.ColonToken,
            ',' => SyntaxKind.CommaToken,
            '.' => SyntaxKind.DotToken,
            '?' => SyntaxKind.QuestionToken,
            '@' => SyntaxKind.AtToken,
            '=' => SyntaxKind.EqualsToken,
            '<' => SyntaxKind.LessToken,
            '>' => SyntaxKind.GreaterToken,
            _ => SyntaxKind.BadToken,
        };
    }

    private bool EnterRecursion()
    {
        if (_recursionDepth >= MaxRecursionDepth)
        {
            _diagnostics.ExcessiveRecursion(GetCurrentSpan());
            return false;
        }
        _recursionDepth++;
        return true;
    }

    private void ExitRecursion()
    {
        _recursionDepth--;
    }

    /// <summary>
    /// Gets the current text span.
    /// </summary>
    private TextSpan GetCurrentSpan()
    {
        var len = (char.IsHighSurrogate(Current) && char.IsLowSurrogate(Peek())) ? 2 : 1;
        return new TextSpan(_position, len);
    }

    /// <summary>
    /// Gets a span from a start position to current position.
    /// </summary>
    private TextSpan GetSpanFrom(int start)
    {
        return TextSpan.FromBounds(start, _position);
    }

    /// <summary>
    /// Gets text from a span.
    /// </summary>
    private string GetText(TextSpan span)
    {
        return _source.GetText(span);
    }

    /// <summary>
    /// Gets text from a start position to current position.
    /// </summary>
    private string GetTextFrom(int start)
    {
        return _source.GetText(start, _position - start);
    }

    // ========================================
    // Whitespace and Comments
    // ========================================

    /// <summary>
    /// Skips whitespace and comments.
    /// </summary>
    private void SkipWhitespaceAndComments()
    {
        while (true)
        {
            switch (Current)
            {
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    Advance();
                    break;

                case '/':
                    if (Peek() == '/')
                    {
                        // Single-line comment
                        Advance(); Advance();
                        while (Current != '\n' && Current != '\0')
                            Advance();
                    }
                    else if (Peek() == '*')
                    {
                        // Multi-line comment
                        Advance(); Advance();
                        while (!(Current == '*' && Peek() == '/') && Current != '\0')
                            Advance();
                        if (Current != '\0')
                        {
                            Advance(); Advance();
                        }
                    }
                    else
                    {
                        return;
                    }
                    break;

                case '\\':
                    // Line continuation: \<newline>
                    if (Peek() == '\r')
                    {
                        Advance(); Advance();
                        if (Current == '\n')
                            Advance();
                    }
                    else if (Peek() == '\n')
                    {
                        Advance(); Advance();
                    }
                    else
                    {
                        return;
                    }
                    break;

                default:
                    return;
            }
        }
    }

    // ========================================
    // Keyword Lookahead
    // ========================================

    /// <summary>
    /// Checks if the current position starts with the given keyword.
    /// A keyword must be followed by a non-identifier character.
    /// </summary>
    private bool LookAheadKeyword(string keyword, int offset = 0)
    {
        var start = _position + offset;
        if (start + keyword.Length > _source.Length)
            return false;

        for (int i = 0; i < keyword.Length; i++)
        {
            if (_source[start + i] != keyword[i])
                return false;
        }

        // Ensure the keyword is not part of a larger identifier
        var nextPos = start + keyword.Length;
        if (nextPos < _source.Length)
        {
            var next = _source[nextPos];
            if (char.IsLetterOrDigit(next) || next == '_')
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the current position starts with a primitive type keyword.
    /// </summary>
    private bool IsAtPrimitiveTypeKeyword()
    {
        return LookAheadKeyword("u8") || LookAheadKeyword("u16") ||
               LookAheadKeyword("u32") || LookAheadKeyword("u64") ||
               LookAheadKeyword("usize") ||
               LookAheadKeyword("i8") || LookAheadKeyword("i16") ||
               LookAheadKeyword("i32") || LookAheadKeyword("i64") ||
               LookAheadKeyword("isize") ||
               LookAheadKeyword("f32") || LookAheadKeyword("f64") ||
               LookAheadKeyword("bool");
    }

    /// <summary>
    /// Checks if the current position starts with a declaration keyword.
    /// </summary>
    private bool IsAtDeclarationKeyword()
    {
        return LookAheadKeyword("func") || LookAheadKeyword("struct") ||
               LookAheadKeyword("enum") || LookAheadKeyword("union") ||
               LookAheadKeyword("variant") || LookAheadKeyword("interface") ||
               LookAheadKeyword("const") || LookAheadKeyword("static") ||
               LookAheadKeyword("var");
    }

    /// <summary>
    /// Checks if the current position starts with a statement keyword.
    /// </summary>
    private bool IsAtStatementKeyword()
    {
        return LookAheadKeyword("if") || LookAheadKeyword("while") ||
               LookAheadKeyword("for") ||
               LookAheadKeyword("switch") || LookAheadKeyword("break") ||
               LookAheadKeyword("continue") || LookAheadKeyword("return") ||
               LookAheadKeyword("defer");
    }

    // ========================================
    // Identifier and Keyword Scanning
    // ========================================

    /// <summary>
    /// Scans an identifier or keyword.
    /// Returns the identifier text (caller should check if it's a keyword).
    /// </summary>
    private string ScanIdentifier()
    {
        var start = _position;

        while (char.IsLetterOrDigit(Current) || Current == '_')
            Advance();

        return GetTextFrom(start);
    }

    /// <summary>
    /// Scans a keyword and returns its kind, or None if not a keyword.
    /// Advances past the keyword if found.
    /// </summary>
    private SyntaxKind ScanKeyword()
    {
        // Check each keyword length (longer first to avoid prefix issues)
        var keywords = new[]
        {
            ("namespace", SyntaxKind.NamespaceKeyword),
            ("interface", SyntaxKind.InterfaceKeyword),
            ("stdcall", SyntaxKind.StdCallKeyword),
            ("struct", SyntaxKind.StructKeyword),
            ("variant", SyntaxKind.VariantKeyword),
            ("isize", SyntaxKind.ISizeKeyword),
            ("usize", SyntaxKind.USizeKeyword),
            ("using", SyntaxKind.UsingKeyword),
            ("union", SyntaxKind.UnionKeyword),
            ("cdecl", SyntaxKind.CDeclKeyword),
            ("const", SyntaxKind.ConstKeyword),
            ("defer", SyntaxKind.DeferKeyword),
            ("false", SyntaxKind.FalseKeyword),
            ("func", SyntaxKind.FuncKeyword),
            ("null", SyntaxKind.NullKeyword),
            ("enum", SyntaxKind.EnumKeyword),
            ("true", SyntaxKind.TrueKeyword),
            ("where", SyntaxKind.WhereKeyword),
            ("while", SyntaxKind.WhileKeyword),
            ("break", SyntaxKind.BreakKeyword),
            ("bool", SyntaxKind.BoolKeyword),
            ("else", SyntaxKind.ElseKeyword),
            ("for", SyntaxKind.ForKeyword),
            ("u64", SyntaxKind.U64Keyword),
            ("i64", SyntaxKind.I64Keyword),
            ("f64", SyntaxKind.F64Keyword),
            ("u32", SyntaxKind.U32Keyword),
            ("i32", SyntaxKind.I32Keyword),
            ("f32", SyntaxKind.F32Keyword),
            ("u16", SyntaxKind.U16Keyword),
            ("i16", SyntaxKind.I16Keyword),
            ("u8", SyntaxKind.U8Keyword),
            ("i8", SyntaxKind.I8Keyword),
            ("static", SyntaxKind.StaticKeyword),
            ("switch", SyntaxKind.SwitchKeyword),
            ("return", SyntaxKind.ReturnKeyword),
            ("var", SyntaxKind.VarKeyword),
            ("if", SyntaxKind.IfKeyword),
            ("continue", SyntaxKind.ContinueKeyword),
        };

        foreach (var (text, kind) in keywords)
        {
            if (LookAheadKeyword(text))
            {
                for (int i = 0; i < text.Length; i++)
                    Advance();
                return kind;
            }
        }

        return SyntaxKind.None;
    }

    /// <summary>
    /// Tries to parse a calling convention keyword (cdecl, stdcall).
    /// Returns a <see cref="CallConventionNode"/> or null.
    /// </summary>
    private CallConventionNode? TryParseCallConvention()
    {
        if (LookAheadKeyword("cdecl") || LookAheadKeyword("stdcall"))
        {
            var start = _position;
            var kind = ScanKeyword();
            var span = GetSpanFrom(start);
            var text = _source.GetText(span);
            SkipWhitespaceAndComments();
            return new CallConventionNode(kind, span, text);
        }
        return null;
    }

    /// <summary>
    /// Parses a comma-separated list of items.
    /// Pattern: item (, item)*
    /// </summary>
    private List<T> ParseCommaSeparatedList<T>(Func<T> parseItem)
    {
        var list = new List<T>();
        list.Add(parseItem());
        SkipWhitespaceAndComments();

        while (Current == ',')
        {
            Advance();
            SkipWhitespaceAndComments();
            list.Add(parseItem());
            SkipWhitespaceAndComments();
        }

        return list;
    }

    /// <summary>
    /// Parses a comma-separated list with a closing character, supporting trailing comma.
    /// Pattern: item (, item)* ,? closeChar
    /// </summary>
    private List<T> ParseCommaSeparatedList<T>(Func<T> parseItem, char closeChar)
    {
        var list = new List<T>();

        if (Current != closeChar)
        {
            list.Add(parseItem());
            SkipWhitespaceAndComments();

            while (Current == ',')
            {
                Advance();
                SkipWhitespaceAndComments();
                if (Current == closeChar)
                    break;
                list.Add(parseItem());
                SkipWhitespaceAndComments();
            }
        }

        return list;
    }
}
