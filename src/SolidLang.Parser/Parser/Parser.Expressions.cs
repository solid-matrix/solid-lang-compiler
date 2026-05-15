using System.Text;
using SolidLang.Parser.Nodes;
using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Literals;
using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Parser;

/// <summary>
/// Expression parsing methods for the parser.
/// </summary>
public sealed partial class Parser
{
    // ========================================
    // Expression Parsing (Operator Precedence)
    // ========================================

    /// <summary>
    /// Parses an expression (entry point).
    /// </summary>
    private ExprNode ParseExpression()
    {
        if (!EnterRecursion()) return new BadExprNode(GetCurrentSpan());
        try
        {
            return ParseConditionalExpr();
        }
        finally { ExitRecursion(); }
    }

    // Precedence 1: Conditional (?:) - Right associative
    private ExprNode ParseConditionalExpr()
    {
        var condition = ParseOrExpr();
        SkipWhitespaceAndComments();

        if (Current == '?')
        {
            var start = condition.Span.Start;
            Advance();
            SkipWhitespaceAndComments();

            var thenExpr = ParseExpression();
            SkipWhitespaceAndComments();

            Expect(':');
            SkipWhitespaceAndComments();

            var elseExpr = ParseConditionalExpr();

            var span = TextSpan.FromBounds(start, _position);
            var text = _source.GetText(span);
            return new ConditionalExprNode(condition, thenExpr, elseExpr, span, text);
        }

        return condition;
    }

    // Precedence 2: Logical OR (||) - Left associative
    private ExprNode ParseOrExpr()
    {
        var left = ParseAndExpr();

        while (true)
        {
            SkipWhitespaceAndComments();
            if (Current == '|' && Peek() == '|')
            {
                var start = left.Span.Start;
                Advance(); Advance();
                SkipWhitespaceAndComments();

                var right = ParseAndExpr();

                var span = TextSpan.FromBounds(start, _position);
                var text = _source.GetText(span);
                left = new BinaryExprNode(left, SyntaxKind.PipePipeToken, right, span, text);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    // Precedence 3: Logical AND (&&) - Left associative
    private ExprNode ParseAndExpr()
    {
        var left = ParseBitOrExpr();

        while (true)
        {
            SkipWhitespaceAndComments();
            if (Current == '&' && Peek() == '&')
            {
                var start = left.Span.Start;
                Advance(); Advance();
                SkipWhitespaceAndComments();

                var right = ParseBitOrExpr();

                var span = TextSpan.FromBounds(start, _position);
                var text = _source.GetText(span);
                left = new BinaryExprNode(left, SyntaxKind.AmpersandAmpersandToken, right, span, text);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    // Precedence 4: Bitwise OR (|) - Left associative
    private ExprNode ParseBitOrExpr()
    {
        var left = ParseBitXorExpr();

        while (true)
        {
            SkipWhitespaceAndComments();
            if (Current == '|' && Peek() != '|' && Peek() != '=')
            {
                var start = left.Span.Start;
                Advance();
                SkipWhitespaceAndComments();

                var right = ParseBitXorExpr();

                var span = TextSpan.FromBounds(start, _position);
                var text = _source.GetText(span);
                left = new BinaryExprNode(left, SyntaxKind.PipeToken, right, span, text);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    // Precedence 5: Bitwise XOR (^) - Left associative
    private ExprNode ParseBitXorExpr()
    {
        var left = ParseBitAndExpr();

        while (true)
        {
            SkipWhitespaceAndComments();
            if (Current == '^' && Peek() != '=')
            {
                var start = left.Span.Start;
                Advance();
                SkipWhitespaceAndComments();

                var right = ParseBitAndExpr();

                var span = TextSpan.FromBounds(start, _position);
                var text = _source.GetText(span);
                left = new BinaryExprNode(left, SyntaxKind.CaretToken, right, span, text);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    // Precedence 6: Bitwise AND (&) - Left associative
    private ExprNode ParseBitAndExpr()
    {
        var left = ParseEqualityExpr();

        while (true)
        {
            SkipWhitespaceAndComments();
            if (Current == '&' && Peek() != '&' && Peek() != '=')
            {
                var start = left.Span.Start;
                Advance();
                SkipWhitespaceAndComments();

                var right = ParseEqualityExpr();

                var span = TextSpan.FromBounds(start, _position);
                var text = _source.GetText(span);
                left = new BinaryExprNode(left, SyntaxKind.AmpersandToken, right, span, text);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    // Precedence 7: Equality (== !=) - Left associative
    private ExprNode ParseEqualityExpr()
    {
        var left = ParseComparisonExpr();

        while (true)
        {
            SkipWhitespaceAndComments();
            SyntaxKind? op = null;

            if (Current == '=' && Peek() == '=')
            {
                op = SyntaxKind.EqualsEqualsToken;
                Advance(); Advance();
            }
            else if (Current == '!' && Peek() == '=')
            {
                op = SyntaxKind.BangEqualsToken;
                Advance(); Advance();
            }

            if (op != null)
            {
                var start = left.Span.Start;
                SkipWhitespaceAndComments();

                var right = ParseComparisonExpr();

                var span = TextSpan.FromBounds(start, _position);
                var text = _source.GetText(span);
                left = new BinaryExprNode(left, op.Value, right, span, text);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    // Precedence 8: Comparison (< > <= >=) - Left associative
    private ExprNode ParseComparisonExpr()
    {
        var left = ParseShiftExpr();

        while (true)
        {
            SkipWhitespaceAndComments();
            SyntaxKind? op = null;

            if (Current == '<' && Peek() != '<' && Peek() != '=')
            {
                op = SyntaxKind.LessToken;
                Advance();
            }
            else if (Current == '>' && Peek() != '>' && Peek() != '=')
            {
                // IMPORTANT: In generic context, this could be closing a generic list
                // The caller should handle this
                op = SyntaxKind.GreaterToken;
                Advance();
            }
            else if (Current == '<' && Peek() == '=')
            {
                op = SyntaxKind.LessEqualsToken;
                Advance(); Advance();
            }
            else if (Current == '>' && Peek() == '=')
            {
                op = SyntaxKind.GreaterEqualsToken;
                Advance(); Advance();
            }

            if (op != null)
            {
                var start = left.Span.Start;
                SkipWhitespaceAndComments();

                var right = ParseShiftExpr();

                var span = TextSpan.FromBounds(start, _position);
                var text = _source.GetText(span);
                left = new BinaryExprNode(left, op.Value, right, span, text);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    // Precedence 9: Shift (<< >>) - Left associative
    private ExprNode ParseShiftExpr()
    {
        var left = ParseAddExpr();

        while (true)
        {
            SkipWhitespaceAndComments();
            SyntaxKind? op = null;

            if (Current == '<' && Peek() == '<')
            {
                // Check if it's <<= or just <<
                if (Peek(2) == '=')
                    break;

                op = SyntaxKind.LessLessToken;
                Advance(); Advance();
            }
            else if (Current == '>' && Peek() == '>')
            {
                // ========================================
                // KEY: >> AMBIGUITY RESOLUTION
                // Only parse >> as right shift when NOT in generic context
                // ========================================
                if (_genericDepth > 0)
                {
                    // In generic context, don't consume >> - it's two > tokens
                    break;
                }

                // Check if it's >>= or just >>
                if (Peek(2) == '=')
                    break;

                op = SyntaxKind.GreaterGreaterToken;
                Advance(); Advance();
            }

            if (op != null)
            {
                var start = left.Span.Start;
                SkipWhitespaceAndComments();

                var right = ParseAddExpr();

                var span = TextSpan.FromBounds(start, _position);
                var text = _source.GetText(span);
                left = new BinaryExprNode(left, op.Value, right, span, text);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    // Precedence 10: Additive (+ -) - Left associative
    private ExprNode ParseAddExpr()
    {
        var left = ParseMulExpr();

        while (true)
        {
            SkipWhitespaceAndComments();
            SyntaxKind? op = null;

            if (Current == '+' && Peek() != '=')
            {
                op = SyntaxKind.PlusToken;
                Advance();
            }
            else if (Current == '-' && Peek() != '=' && Peek() != '>')
            {
                op = SyntaxKind.MinusToken;
                Advance();
            }

            if (op != null)
            {
                var start = left.Span.Start;
                SkipWhitespaceAndComments();

                var right = ParseMulExpr();

                var span = TextSpan.FromBounds(start, _position);
                var text = _source.GetText(span);
                left = new BinaryExprNode(left, op.Value, right, span, text);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    // Precedence 11: Multiplicative (* / %) - Left associative
    private ExprNode ParseMulExpr()
    {
        var left = ParseUnaryExpr();

        while (true)
        {
            SkipWhitespaceAndComments();
            SyntaxKind? op = null;

            if (Current == '*' && Peek() != '=')
            {
                op = SyntaxKind.StarToken;
                Advance();
            }
            else if (Current == '/' && Peek() != '=')
            {
                op = SyntaxKind.SlashToken;
                Advance();
            }
            else if (Current == '%' && Peek() != '=')
            {
                op = SyntaxKind.PercentToken;
                Advance();
            }

            if (op != null)
            {
                var start = left.Span.Start;
                SkipWhitespaceAndComments();

                var right = ParseUnaryExpr();

                var span = TextSpan.FromBounds(start, _position);
                var text = _source.GetText(span);
                left = new BinaryExprNode(left, op.Value, right, span, text);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    // Precedence 12: Unary (- + ! ~ & *) - Right associative
    private ExprNode ParseUnaryExpr()
    {
        SkipWhitespaceAndComments();

        SyntaxKind? op = null;

        if (Current == '-')
        {
            if (Peek() == '>' || Peek() == '=')
            {
                // -=, not unary minus
            }
            else
            {
                op = SyntaxKind.MinusToken;
                Advance();
            }
        }
        else if (Current == '+')
        {
            if (Peek() == '=')
            {
                // +=, not unary plus
            }
            else
            {
                op = SyntaxKind.PlusToken;
                Advance();
            }
        }
        else if (Current == '!')
        {
            if (Peek() == '=')
            {
                // !=, not logical not
            }
            else
            {
                op = SyntaxKind.BangToken;
                Advance();
            }
        }
        else if (Current == '~')
        {
            op = SyntaxKind.TildeToken;
            Advance();
        }
        else if (Current == '&')
        {
            if (Peek() == '&' || Peek() == '=')
            {
                // && or &=, not address-of
            }
            else if (Peek() == '!')
            {
                // &! - mutable address-of
                op = SyntaxKind.AmpersandToken;
                Advance(); Advance();
            }
            else
            {
                op = SyntaxKind.AmpersandToken;
                Advance();
            }
        }
        else if (Current == '*')
        {
            if (Peek() == '=')
            {
                // *=, not dereference
            }
            else
            {
                // In expression context, *expr is dereference
                op = SyntaxKind.StarToken;
                Advance();
            }
        }

        if (op != null)
        {
            var start = _position - 1;
            if (op == SyntaxKind.AmpersandToken && Current == '!')
            {
                // Already consumed in &! case
            }

            SkipWhitespaceAndComments();
            var operand = ParseUnaryExpr();

            var span = TextSpan.FromBounds(start, _position);
            var text = _source.GetText(span);
            return new UnaryExprNode(op.Value, operand, span, text);
        }

        return ParsePostfixExpr();
    }

    // Postfix: . [] ()
    private ExprNode ParsePostfixExpr()
    {
        var primary = ParsePrimaryExpr();

        var suffixes = new List<PostfixSuffixNode>();

        while (true)
        {
            SkipWhitespaceAndComments();

            if (Current == '.')
            {
                var start = _position;
                Advance();
                SkipWhitespaceAndComments();

                var name = ScanIdentifier();
                SkipWhitespaceAndComments();

                // Check for generic type arguments: .method<T>
                TypeArgumentListNode? typeArgs = null;
                if (name.Length > 0 && Current == '<')
                {
                    var savedPos = _position;
                    var savedDiag = _diagnostics.Count;

                    typeArgs = ParseTypeArgumentList();
                    SkipWhitespaceAndComments();

                    // Only keep type args if ( follows (generic method call)
                    if (Current != '(')
                    {
                        _position = savedPos;
                        _diagnostics.TruncateTo(savedDiag);
                        typeArgs = null;
                    }
                }

                var span = GetSpanFrom(start);
                var text = _source.GetText(span);
                suffixes.Add(new DotAccessNode(name, typeArgs, span, text));
            }
            else if (Current == '[')
            {
                var start = _position;
                Advance();
                SkipWhitespaceAndComments();

                var index = ParseExpression();
                SkipWhitespaceAndComments();

                Expect(']');

                var span = GetSpanFrom(start);
                var text = _source.GetText(span);
                suffixes.Add(new IndexAccessNode(index, span, text));
            }
            else if (Current == '(')
            {
                var start = _position;
                Advance();
                SkipWhitespaceAndComments();

                CallArgsNode? args = null;
                if (Current != ')')
                {
                    args = ParseCallArgs();
                }

                Expect(')');

                var span = GetSpanFrom(start);
                var text = _source.GetText(span);
                suffixes.Add(new CallExprNode(args, span, text));
            }
            else if (Current == '*' && Peek() == '.')
            {
                // *.member: sugar for (*expr).member
                var start = _position;
                Advance(); // consume *
                Advance(); // consume .
                SkipWhitespaceAndComments();

                var name = ScanIdentifier();

                var span = GetSpanFrom(start);
                var text = _source.GetText(span);
                suffixes.Add(new PointerAccessNode(name, span, text));
            }
            else if (Current == '&' && Peek() == '.')
            {
                // &.member: sugar for (&expr).member
                var start = _position;
                Advance(); // consume &
                Advance(); // consume .
                SkipWhitespaceAndComments();

                var name = ScanIdentifier();

                var span = GetSpanFrom(start);
                var text = _source.GetText(span);
                suffixes.Add(new AddressAccessNode(name, span, text));
            }
            else
            {
                break;
            }
        }

        if (suffixes.Count == 0)
            return primary;

        var exprStart = primary.Span.Start;
        var exprSpan = TextSpan.FromBounds(exprStart, _position);
        var exprText = _source.GetText(exprSpan);
        return new PostfixExprNode(primary, suffixes, exprSpan, exprText);
    }

    private CallArgsNode ParseCallArgs()
    {
        var start = _position;
        var args = new List<CallArgNode>();

        args.Add(new CallArgNode(ParseExpression(), GetCurrentSpan(), GetTextFrom(start)));
        SkipWhitespaceAndComments();

        while (Current == ',')
        {
            Advance();
            SkipWhitespaceAndComments();

            var argStart = _position;
            args.Add(new CallArgNode(ParseExpression(), GetSpanFrom(argStart), GetTextFrom(argStart)));
            SkipWhitespaceAndComments();
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new CallArgsNode(args, span, text);
    }

    // Primary: literals, identifiers, parenthesized expressions
    private ExprNode ParsePrimaryExpr()
    {
        SkipWhitespaceAndComments();
        var start = _position;

        // Check for compile-time operator expression: @name(...)
        if (Current == '@')
        {
            var ctOp = ParseCtOperatorExpr();
            return new PrimaryExprNode(PrimaryExprKind.CtOperator, null, null, null, ctOp, ctOp.Span, ctOp.GetFullText());
        }

        // Parenthesized expression
        if (Current == '(')
        {
            Advance();
            SkipWhitespaceAndComments();

            var expr = ParseExpression();
            SkipWhitespaceAndComments();

            Expect(')');

            var span = GetSpanFrom(start);
            var text = _source.GetText(span);
            return new PrimaryExprNode(PrimaryExprKind.Parenthesized, null, null, expr, null, span, text);
        }

        // Literals
        var literal = TryParseLiteral();
        if (literal != null)
        {
            return new PrimaryExprNode(PrimaryExprKind.Literal, literal, null, null, null, literal.Span, literal.GetFullText());
        }

        // Identifier (possibly scoped: Name::Name::...::member)
        if (char.IsLetter(Current) || Current == '_')
        {
            return ParseIdentifierOrScopedAccess(start);
        }

        // Error
        _diagnostics.ExpectedExpression(GetCurrentSpan());
        return new BadExprNode(GetCurrentSpan());
    }

    /// <summary>
    /// Parses an identifier or a scoped access expression: [NS::][Type&lt;T&gt;::]member [ (args) ].
    /// Uses TryParseNamedTypePrefix to determine if :: follows the first identifier.
    /// </summary>
    private ExprNode ParseIdentifierOrScopedAccess(int start)
    {
        var namedTypePrefix = TryParseNamedTypePrefix();

        if (namedTypePrefix == null)
        {
            // Simple identifier
            var name = ScanIdentifier();
            SkipWhitespaceAndComments();

            // Check for generic type arguments: identity<T>(args)
            TypeArgumentListNode? typeArgs = null;
            if (name.Length > 0 && Current == '<')
            {
                var savedPos = _position;
                var savedDiag = _diagnostics.Count;

                typeArgs = ParseTypeArgumentList();
                SkipWhitespaceAndComments();

                // Only valid as generic call if ( follows
                if (Current == '(')
                {
                    Advance();
                    SkipWhitespaceAndComments();

                    CallArgsNode? callArgs = null;
                    if (Current != ')')
                        callArgs = ParseCallArgs();

                    SkipWhitespaceAndComments();
                    Expect(')');

                    var exprSpan = GetSpanFrom(start);
                    var exprText = _source.GetText(exprSpan);
                    return new ScopedAccessExprNode(null, name, callArgs, typeArgs, exprSpan, exprText);
                }

                // Not a generic call — backtrack, treat < as comparison operator
                _position = savedPos;
                _diagnostics.TruncateTo(savedDiag);
            }

            var simpleSpan = GetSpanFrom(start);
            var simpleText = _source.GetText(simpleSpan);
            return new PrimaryExprNode(PrimaryExprKind.Identifier, null, name, null, null, simpleSpan, simpleText);
        }

        // Scoped access: prefix::member or prefix::member(args)
        SkipWhitespaceAndComments();
        var memberName = ScanIdentifier();
        SkipWhitespaceAndComments();

        CallArgsNode? args = null;
        if (Current == '(')
        {
            Advance();
            SkipWhitespaceAndComments();

            if (Current != ')')
                args = ParseCallArgs();

            SkipWhitespaceAndComments();
            Expect(')');
        }

        var exprSpan2 = GetSpanFrom(start);
        var exprText2 = _source.GetText(exprSpan2);
        return new ScopedAccessExprNode(namedTypePrefix, memberName, args, null, exprSpan2, exprText2);
    }

    private CtOperatorExprNode ParseCtOperatorExpr()
    {
        var start = _position;
        Advance(); // Skip @

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        CtOperatorArgsNode? args = null;
        if (Current == '(')
        {
            Advance();
            SkipWhitespaceAndComments();

            if (Current != ')')
            {
                args = ParseCtOperatorArgs();
            }

            Expect(')');
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new CtOperatorExprNode(name, args, span, text);
    }

    private CtOperatorArgsNode ParseCtOperatorArgs()
    {
        var start = _position;
        var args = ParseCommaSeparatedList(ParseCtOperatorArg);

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new CtOperatorArgsNode(args, span, text);
    }

    private CtOperatorArgNode ParseCtOperatorArg()
    {
        var start = _position;

        // Try type first
        var type = TryParseType();
        if (type != null)
        {
            var span = GetSpanFrom(start);
            return new CtOperatorArgNode(type, null, span, _source.GetText(span));
        }

        // Parse expression
        var expr = ParseExpression();
        var span2 = GetSpanFrom(start);
        return new CtOperatorArgNode(null, expr, span2, _source.GetText(span2));
    }

    // ========================================
    // Literal Parsing
    // ========================================

    private LiteralNode? TryParseLiteral()
    {
        SkipWhitespaceAndComments();
        var start = _position;

        // Null
        if (LookAheadKeyword("null"))
        {
            Match("null");
            return new NullLiteralNode(GetSpanFrom(start));
        }

        // Boolean
        if (LookAheadKeyword("true"))
        {
            Match("true");
            return new BoolLiteralNode(true, GetSpanFrom(start));
        }
        if (LookAheadKeyword("false"))
        {
            Match("false");
            return new BoolLiteralNode(false, GetSpanFrom(start));
        }

        // Number (integer or float)
        if (char.IsDigit(Current) || (Current == '.' && char.IsDigit(Peek())))
        {
            return ParseNumericLiteral();
        }

        // String
        if (Current == '"')
        {
            var (text, value) = ScanStringLiteral();
            var span = GetSpanFrom(start);
            return new StringLiteralNode(text, value, span);
        }

        // Char
        if (Current == '\'')
        {
            var (text, value) = ScanCharLiteral();
            var span = GetSpanFrom(start);
            return new CharLiteralNode(text, value, span);
        }

        // Array literal: [size]type{ ... }
        if (Current == '[')
        {
            return TryParseArrayLiteral();
        }

        // Struct/Enum/Variant literal: Name{ ... } or Name::member
        if (char.IsLetter(Current) || Current == '_')
        {
            return TryParseCompositeLiteral();
        }

        return null;
    }

    private LiteralNode ParseNumericLiteral()
    {
        var start = _position;

        // Check for float starting with .
        if (Current == '.')
        {
            var (text, value, suffix) = ScanFloatLiteral(start, "");
            return new FloatLiteralNode(text, value, suffix, GetSpanFrom(start));
        }

        // Scan integer part
        var (intText, intValue, intSuffix) = ScanIntegerLiteral();

        // Check for float (has . or e)
        if (Current == '.' || Current == 'e' || Current == 'E')
        {
            var (text, value, suffix) = ScanFloatLiteral(start, intText);
            return new FloatLiteralNode(text, value, suffix, GetSpanFrom(start));
        }

        return new IntegerLiteralNode(intText, intValue, intSuffix, GetSpanFrom(start));
    }

    private ArrayLiteralNode? TryParseArrayLiteral()
    {
        // Array literal: [size]type{ elements... }
        // In expression context, [ at the start must be an array literal
        // (index access is handled in ParsePostfixExpr)
        var start = _position;
        var savedPos = _position;

        var arrayType = ParseArrayType();
        SkipWhitespaceAndComments();

        if (Current != '{')
        {
            // Not an array literal — backtrack
            _position = savedPos;
            return null;
        }

        Advance(); // Skip {
        SkipWhitespaceAndComments();

        var elements = ParseCommaSeparatedList(ParseExpression, '}');

        Expect('}');

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new ArrayLiteralNode(arrayType, elements, span, text);
    }

    private LiteralNode? TryParseCompositeLiteral()
    {
        var start = _position;
        var savedPos = _position;
        var savedDepth = _genericDepth;

        // Save diagnostic count to allow rollback on speculative failure
        var diagCount = _diagnostics.Count;

        // Try to parse as a simple named type (no namespace prefix).
        // We use ParseSimpleNamedType because in this context :: separates type from member,
        // not namespace from type.
        if (!char.IsLetter(Current) && Current != '_')
            return null;

        var namedType = ParseSimpleNamedType();
        SkipWhitespaceAndComments();

        // If type parsing produced errors, this is not a valid composite literal
        var hasTypeErrors = _diagnostics.Count > diagCount;

        // Check what follows
        if (!hasTypeErrors && Current == '{')
        {
            // Struct literal candidate: Name{ ... } or Name { ... }
            // Speculatively check that { contains field assignments (or is empty)
            var specPos = _position;
            Advance(); // skip {
            SkipWhitespaceAndComments();
            var isStructLiteral = Current == '}' || MatchStructLiteralFieldPattern();
            _position = specPos;
            if (isStructLiteral)
                return ParseStructLiteral(namedType, start);
        }

        if (!hasTypeErrors && Current == ':' && Peek() == ':')
        {
            // May be enum literal: Name::member (without args)
            // Name::member(args) could be a variant literal OR a scoped access call —
            // let ParseIdentifierOrScopedAccess handle the ambiguity.
            var specPos = _position;
            Advance(); Advance(); // skip ::
            SkipWhitespaceAndComments();
            if (char.IsLetter(Current) || Current == '_')
            {
                ScanIdentifier();
                SkipWhitespaceAndComments();
                // Only treat as enum literal if there's NO (args) and NO further :: (scoped access)
                if (Current != '(' && !(Current == ':' && Peek() == ':'))
                {
                    _position = specPos;
                    return ParseEnumOrVariantLiteral(namedType, start);
                }
            }
            // Backtrack — let scoped access or other handling take over
            _position = savedPos;
            _genericDepth = savedDepth;
            _diagnostics.TruncateTo(diagCount);
            return null;
        }

        // Not a literal, backtrack — restore position, generic depth, and diagnostics
        _position = savedPos;
        _genericDepth = savedDepth;
        _diagnostics.TruncateTo(diagCount);
        return null;
    }

    /// <summary>
    /// Speculatively checks if the current position starts with a struct literal field pattern
    /// (identifier = or @annotate identifier =). Does not consume if no match.
    /// Assumes whitespace has already been skipped.
    /// </summary>
    private bool MatchStructLiteralFieldPattern()
    {
        var savedPos = _position;
        var savedDiag = _diagnostics.Count;

        // Skip annotations
        while (Current == '@')
        {
            Advance();
            if (char.IsLetter(Current) || Current == '_')
                ScanIdentifier();
            if (Current == '(')
            {
                Advance();
                var depth = 1;
                while (depth > 0 && Current != '\0')
                {
                    if (Current == '(') depth++;
                    else if (Current == ')') depth--;
                    Advance();
                }
            }
            SkipWhitespaceAndComments();
        }

        if (char.IsLetter(Current) || Current == '_')
        {
            ScanIdentifier();
            SkipWhitespaceAndComments();
            var matches = Current == '=';
            _position = savedPos;
            _diagnostics.TruncateTo(savedDiag);
            return matches;
        }

        _position = savedPos;
        _diagnostics.TruncateTo(savedDiag);
        return false;
    }

    private StructLiteralNode ParseStructLiteral(NamedTypeNode type, int start)
    {
        Expect('{');
        SkipWhitespaceAndComments();

        var fields = ParseCommaSeparatedList(ParseStructLiteralField, '}');

        Expect('}');

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new StructLiteralNode(type, fields, span, text);
    }

    private StructLiteralFieldNode ParseStructLiteralField()
    {
        var start = _position;
        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        Expect('=');
        SkipWhitespaceAndComments();

        var value = ParseExpression();

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new StructLiteralFieldNode(name, value, span, text);
    }

    private LiteralNode ParseEnumOrVariantLiteral(NamedTypeNode type, int start)
    {
        Expect(':');
        Expect(':');
        SkipWhitespaceAndComments();

        var memberName = ScanIdentifier();
        SkipWhitespaceAndComments();

        if (Current == '(')
        {
            // Variant literal with value
            Advance();
            SkipWhitespaceAndComments();

            ExprNode? value = null;
            if (Current != ')')
            {
                value = ParseExpression();
                SkipWhitespaceAndComments();
            }

            Expect(')');

            var span = GetSpanFrom(start);
            var text = _source.GetText(span);
            return new VariantLiteralNode(type, memberName, value, span, text);
        }
        else
        {
            // Enum literal
            var span = GetSpanFrom(start);
            var text = _source.GetText(span);
            return new EnumLiteralNode(type, memberName, span, text);
        }
    }
}
