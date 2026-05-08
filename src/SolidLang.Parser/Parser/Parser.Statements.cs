using System.Text;
using SolidLang.Parser.Nodes;
using SolidLang.Parser.Nodes.Declarations;
using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Statements;

namespace SolidLang.Parser.Parser;

/// <summary>
/// Statement parsing methods for the parser.
/// </summary>
public sealed partial class Parser
{
    // ========================================
    // Statement Parsing
    // ========================================

    /// <summary>
    /// Parses a statement.
    /// </summary>
    private StmtNode ParseStatement()
    {
        SkipWhitespaceAndComments();
        var start = _position;

        // Empty statement
        if (Current == ';')
        {
            Advance();
            return new EmptyStmtNode(GetSpanFrom(start));
        }

        // Body statement (block)
        if (Current == '{')
        {
            return ParseBodyStmt();
        }

        // Statement keywords
        if (LookAheadKeyword("if"))
            return ParseIfStmt(start);

        if (LookAheadKeyword("for"))
            return ParseForStmt(start);

        if (LookAheadKeyword("switch"))
            return ParseSwitchStmt(start);

        if (LookAheadKeyword("break"))
            return ParseBreakStmt(start);

        if (LookAheadKeyword("continue"))
            return ParseContinueStmt(start);

        if (LookAheadKeyword("return"))
            return ParseReturnStmt(start);

        if (LookAheadKeyword("defer"))
            return ParseDeferStmt(start);

        // Declaration statements
        if (LookAheadKeyword("var"))
        {
            var annotations = ParseAnnotations();
            return ParseVarDecl(annotations, start);
        }

        if (LookAheadKeyword("const"))
        {
            var annotations = ParseAnnotations();
            return ParseConstDecl(annotations, start);
        }

        if (LookAheadKeyword("static"))
        {
            var annotations = ParseAnnotations();
            return ParseStaticDecl(annotations, start);
        }

        // Expression statement or assignment
        return ParseExprOrAssignStmt(start);
    }

    private BodyStmtNode ParseBodyStmt()
    {
        var start = _position;
        Expect('{');
        SkipWhitespaceAndComments();

        var statements = new List<StmtNode>();

        while (Current != '}' && Current != '\0')
        {
            var stmt = ParseStatement();
            if (stmt is not BadStmtNode)
                statements.Add(stmt);
            SkipWhitespaceAndComments();
        }

        Expect('}');

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new BodyStmtNode(statements, span, text);
    }

    // ========================================
    // Control Flow Statements
    // ========================================

    private IfStmtNode ParseIfStmt(int start)
    {
        Match("if");
        SkipWhitespaceAndComments();

        var condition = ParseExpression();
        SkipWhitespaceAndComments();

        var thenBody = ParseBodyStmt();
        SkipWhitespaceAndComments();

        StmtNode? elseBody = null;
        if (LookAheadKeyword("else"))
        {
            Match("else");
            SkipWhitespaceAndComments();

            if (LookAheadKeyword("if"))
            {
                elseBody = ParseIfStmt(_position);
            }
            else
            {
                elseBody = ParseBodyStmt();
            }
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new IfStmtNode(condition, thenBody, elseBody, span, text);
    }

    private ForStmtNode ParseForStmt(int start)
    {
        Match("for");
        SkipWhitespaceAndComments();

        // Determine for loop type
        if (Current == '{')
        {
            // Infinite loop: for { ... }
            var body = ParseBodyStmt();
            var span = GetSpanFrom(start);
            var text = _source.GetText(span);
            return new ForStmtNode(new ForInfiniteNode(body, span, text), span, text);
        }

        // Check if it's a C-style for loop (starts with ; or has ; later)
        // or a conditional loop (expression followed by {)

        // Save position for lookahead
        var savedPos = _position;

        // Try to detect C-style by looking for semicolons before {
        bool isCStyle = false;
        int depth = 0;
        while (_position < _source.Length)
        {
            var c = Current;
            if (c == ';' && depth == 0)
            {
                isCStyle = true;
                break;
            }
            if (c == '{')
            {
                break;
            }
            if (c == '(' || c == '[')
                depth++;
            else if (c == ')' || c == ']')
                depth--;

            Advance();
        }

        // Restore position
        _position = savedPos;

        if (isCStyle)
        {
            return ParseForCStyle(start);
        }
        else
        {
            return ParseForCond(start);
        }
    }

    private ForStmtNode ParseForCond(int start)
    {
        var condition = ParseExpression();
        SkipWhitespaceAndComments();

        var body = ParseBodyStmt();

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new ForStmtNode(new ForCondNode(condition, body, span, text), span, text);
    }

    private ForStmtNode ParseForCStyle(int start)
    {
        // init; cond; update { body }
        ForInitNode? init = null;

        if (Current != ';')
        {
            init = ParseForInit();
        }
        SkipWhitespaceAndComments();

        Expect(';');
        SkipWhitespaceAndComments();

        ExprNode? condition = null;
        if (Current != ';')
        {
            condition = ParseExpression();
        }
        SkipWhitespaceAndComments();

        Expect(';');
        SkipWhitespaceAndComments();

        ExprNode? update = null;
        if (Current != '{')
        {
            var updateExpr = ParseExpression();
            SkipWhitespaceAndComments();
            var assignOp = ParseAssignmentOperator();
            if (assignOp != null)
            {
                SkipWhitespaceAndComments();
                var value = ParseExpression();
                var updateSpan = TextSpan.FromBounds(updateExpr.Span.Start, _position);
                var updateText = _source.GetText(updateSpan);
                update = new BinaryExprNode(updateExpr, assignOp.Value, value, updateSpan, updateText);
            }
            else
            {
                update = updateExpr;
            }
        }
        SkipWhitespaceAndComments();

        var body = ParseBodyStmt();

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new ForStmtNode(new ForCStyleNode(init, condition, update, body, span, text), span, text);
    }

    private ForInitNode ParseForInit()
    {
        SkipWhitespaceAndComments();
        var start = _position;

        // Check for var declaration
        if (LookAheadKeyword("var"))
        {
            var annotations = ParseAnnotations();
            Match("var");
            SkipWhitespaceAndComments();

            var name = ScanIdentifier();
            SkipWhitespaceAndComments();

            Expect('=');
            SkipWhitespaceAndComments();

            var initializer = ParseExpression();

            var span = GetSpanFrom(start);
            var text = _source.GetText(span);
            return new ForVarDeclNode(annotations, name, initializer, span, text);
        }

        // Assignment expression
        var target = ParseExpression();
        SkipWhitespaceAndComments();

        var op = ParseAssignmentOperator();
        if (op == null)
        {
            _diagnostics.ExpectedToken(SyntaxKind.EqualsToken, SyntaxKind.BadToken, GetCurrentSpan());
            op = SyntaxKind.EqualsToken;
        }

        SkipWhitespaceAndComments();
        var value = ParseExpression();

        var span2 = GetSpanFrom(start);
        var text2 = _source.GetText(span2);
        return new ForAssignNode(target, op.Value, value, span2, text2);
    }

    private SyntaxKind? ParseAssignmentOperator()
    {
        if (Current == '=' && Peek() != '=')
        {
            Advance();
            return SyntaxKind.EqualsToken;
        }
        if (Current == '+' && Peek() == '=')
        {
            Advance(); Advance();
            return SyntaxKind.PlusEqualsToken;
        }
        if (Current == '-' && Peek() == '=')
        {
            Advance(); Advance();
            return SyntaxKind.MinusEqualsToken;
        }
        if (Current == '*' && Peek() == '=')
        {
            Advance(); Advance();
            return SyntaxKind.StarEqualsToken;
        }
        if (Current == '/' && Peek() == '=')
        {
            Advance(); Advance();
            return SyntaxKind.SlashEqualsToken;
        }
        if (Current == '%' && Peek() == '=')
        {
            Advance(); Advance();
            return SyntaxKind.PercentEqualsToken;
        }
        if (Current == '&' && Peek() == '=')
        {
            Advance(); Advance();
            return SyntaxKind.AmpersandEqualsToken;
        }
        if (Current == '|' && Peek() == '=')
        {
            Advance(); Advance();
            return SyntaxKind.PipeEqualsToken;
        }
        if (Current == '^' && Peek() == '=')
        {
            Advance(); Advance();
            return SyntaxKind.CaretEqualsToken;
        }
        if (Current == '<' && Peek() == '<' && Peek(2) == '=')
        {
            Advance(); Advance(); Advance();
            return SyntaxKind.LessLessEqualsToken;
        }
        if (Current == '>' && Peek() == '>' && Peek(2) == '=')
        {
            Advance(); Advance(); Advance();
            return SyntaxKind.GreaterGreaterEqualsToken;
        }

        return null;
    }

    private SwitchStmtNode ParseSwitchStmt(int start)
    {
        Match("switch");
        SkipWhitespaceAndComments();

        var expr = ParseExpression();
        SkipWhitespaceAndComments();

        Expect('{');
        SkipWhitespaceAndComments();

        var arms = new List<SwitchArmNode>();

        while (Current != '}' && Current != '\0')
        {
            arms.Add(ParseSwitchArm());
            SkipWhitespaceAndComments();
        }

        Expect('}');

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new SwitchStmtNode(expr, arms, span, text);
    }

    private SwitchArmNode ParseSwitchArm()
    {
        var start = _position;
        bool isElse = false;
        var patterns = new List<SwitchPatternNode>();

        if (LookAheadKeyword("else"))
        {
            Match("else");
            isElse = true;
        }
        else
        {
            // Parse first pattern
            var firstPattern = ParseSwitchPattern();
            if (firstPattern != null)
                patterns.Add(firstPattern);

            SkipWhitespaceAndComments();

            // Handle comma-separated patterns: pattern1, pattern2, pattern3 => ...
            while (Current == ',')
            {
                Advance();
                SkipWhitespaceAndComments();

                var nextPattern = ParseSwitchPattern();
                if (nextPattern != null)
                    patterns.Add(nextPattern);

                SkipWhitespaceAndComments();
            }
        }

        SkipWhitespaceAndComments();

        // =>
        if (Current == '=' && Peek() == '>')
        {
            Advance(); Advance();
        }
        else
        {
            // Error recovery: if no patterns were parsed and => is missing, skip to safe point
            if (patterns.Count == 0 && !isElse)
            {
                // Skip to next => or } or end of line to avoid infinite loop
                while (Current != '\0' && Current != '}' && !(Current == '=' && Peek() == '>'))
                    Advance();
                if (Current == '=' && Peek() == '>')
                {
                    Advance(); Advance();
                }
            }
            else
            {
                _diagnostics.ExpectedToken(SyntaxKind.EqualsArrowToken, SyntaxKind.BadToken, GetCurrentSpan());
            }
        }

        SkipWhitespaceAndComments();

        var stmt = ParseStatement();

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new SwitchArmNode(isElse, patterns, stmt, span, text);
    }

    private SwitchPatternNode? ParseSwitchPattern()
    {
        var start = _position;

        // Try to parse as literal first
        var literal = TryParseLiteral();
        if (literal != null)
        {
            var span = GetSpanFrom(start);
            var text = _source.GetText(span);
            return new SwitchPatternNode(SwitchPatternKind.Literal, literal, null, null, null, null, span, text);
        }

        // Try to parse as named type with member.
        // Use ParseSimpleNamedType because in this context :: separates type from member.
        if (char.IsLetter(Current) || Current == '_')
        {
            var namedType = ParseSimpleNamedType();
            SkipWhitespaceAndComments();

            if (Current == ':' && Peek() == ':')
            {
                // NamedType::member or NamedType::member(expr)
                Advance(); Advance();
                SkipWhitespaceAndComments();

                var memberName = ScanIdentifier();
                SkipWhitespaceAndComments();

                if (Current == '(')
                {
                    // Pattern with binding
                    Advance();
                    SkipWhitespaceAndComments();

                    ExprNode? binding = null;
                    if (Current != ')')
                    {
                        binding = ParseExpression();
                        SkipWhitespaceAndComments();
                    }

                    Expect(')');

                    var span = GetSpanFrom(start);
                    var text = _source.GetText(span);
                    return new SwitchPatternNode(SwitchPatternKind.NamedTypeMemberBinding, null, namedType, memberName, binding, null, span, text);
                }
                else
                {
                    var span = GetSpanFrom(start);
                    var text = _source.GetText(span);
                    return new SwitchPatternNode(SwitchPatternKind.NamedTypeMember, null, namedType, memberName, null, null, span, text);
                }
            }
            else
            {
                // Just an identifier — backtrack to let the identifier pattern handle it
                _position = namedType.Span.Start;
            }
        }

        // Identifier pattern
        if (char.IsLetter(Current) || Current == '_')
        {
            var identifier = ScanIdentifier();
            var span2 = GetSpanFrom(start);
            var text2 = _source.GetText(span2);
            return new SwitchPatternNode(SwitchPatternKind.Identifier, null, null, null, null, identifier, span2, text2);
        }

        return null;
    }

    private BreakStmtNode ParseBreakStmt(int start)
    {
        Match("break");
        SkipWhitespaceAndComments();
        Expect(';');

        return new BreakStmtNode(GetSpanFrom(start));
    }

    private ContinueStmtNode ParseContinueStmt(int start)
    {
        Match("continue");
        SkipWhitespaceAndComments();
        Expect(';');

        return new ContinueStmtNode(GetSpanFrom(start));
    }

    private ReturnStmtNode ParseReturnStmt(int start)
    {
        Match("return");
        SkipWhitespaceAndComments();

        ExprNode? expr = null;
        if (Current != ';')
        {
            expr = ParseExpression();
            SkipWhitespaceAndComments();
        }

        Expect(';');

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new ReturnStmtNode(expr, span, text);
    }

    private DeferStmtNode ParseDeferStmt(int start)
    {
        Match("defer");
        SkipWhitespaceAndComments();

        // Allowed statements in defer:
        // empty_stmt, body_stmt, assign_stmt, expr_stmt, if_stmt, for_stmt, switch_stmt
        StmtNode deferredStmt;

        if (Current == ';')
        {
            deferredStmt = new EmptyStmtNode(GetCurrentSpan());
            Advance();
        }
        else if (Current == '{')
        {
            deferredStmt = ParseBodyStmt();
        }
        else if (LookAheadKeyword("if"))
        {
            deferredStmt = ParseIfStmt(_position);
        }
        else if (LookAheadKeyword("for"))
        {
            deferredStmt = ParseForStmt(_position);
        }
        else if (LookAheadKeyword("switch"))
        {
            deferredStmt = ParseSwitchStmt(_position);
        }
        else
        {
            // Assignment or expression statement
            deferredStmt = ParseExprOrAssignStmt(_position);
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new DeferStmtNode(deferredStmt, span, text);
    }

    // ========================================
    // Expression or Assignment Statement
    // ========================================

    private StmtNode ParseExprOrAssignStmt(int start)
    {
        var expr = ParseExpression();
        SkipWhitespaceAndComments();

        // Check for assignment operator
        var op = ParseAssignmentOperator();

        if (op != null)
        {
            // Assignment statement
            SkipWhitespaceAndComments();
            var value = ParseExpression();
            SkipWhitespaceAndComments();
            Expect(';');

            var span = GetSpanFrom(start);
            var text = _source.GetText(span);
            return new AssignStmtNode(expr, op.Value, value, span, text);
        }
        else
        {
            // Expression statement
            Expect(';');

            var span = GetSpanFrom(start);
            var text = _source.GetText(span);
            return new ExprStmtNode(expr, span, text);
        }
    }
}
