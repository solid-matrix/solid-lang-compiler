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
        if (!EnterRecursion()) return new BadStmtNode(GetCurrentSpan());
        try
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

        if (LookAheadKeyword("while"))
            return ParseWhileStmt(start);

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
        finally { ExitRecursion(); }
    }

    private BodyStmtNode ParseBodyStmt()
    {
        var start = _position;
        Expect('{');
        SkipWhitespaceAndComments();

        var statements = new List<StmtNode>();

        while (Current != '}' && Current != '\0')
        {
            var savedPos = _position;
            var stmt = ParseStatement();
            if (stmt is not BadStmtNode)
                statements.Add(stmt);
            SkipWhitespaceAndComments();

            // Safety: ensure progress to avoid infinite loop on stuck tokens
            if (_position == savedPos && Current != '}' && Current != '\0')
            {
                Advance();
                SkipWhitespaceAndComments();
            }
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

    private ForStmtNode ParseWhileStmt(int start)
    {
        Match("while");
        SkipWhitespaceAndComments();

        if (Current == '{')
        {
            // Infinite loop: while { ... }
            var body = ParseBodyStmt();
            var span = GetSpanFrom(start);
            var text = _source.GetText(span);
            return new ForStmtNode(new ForInfiniteNode(body, span, text), span, text);
        }

        // Conditional loop: while cond { ... }
        var condition = ParseExpression();
        SkipWhitespaceAndComments();

        var condBody = ParseBodyStmt();

        var condSpan = GetSpanFrom(start);
        var condText = _source.GetText(condSpan);
        return new ForStmtNode(new ForCondNode(condition, condBody, condSpan, condText), condSpan, condText);
    }

    private ForStmtNode ParseForStmt(int start)
    {
        Match("for");
        SkipWhitespaceAndComments();
        return ParseForCStyle(start);
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
            var savedPos = _position;
            arms.Add(ParseSwitchArm());
            SkipWhitespaceAndComments();
            if (_position == savedPos)
            {
                Advance();
                SkipWhitespaceAndComments();
            }
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
        else if (LookAheadKeyword("while"))
        {
            deferredStmt = ParseWhileStmt(_position);
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

        // BadExprNode means position didn't advance — skip to avoid infinite loop
        if (expr is BadExprNode)
        {
            // Advance past the problematic character so the caller can make progress
            if (Current != '\0' && Current != '}')
            {
                Advance();
                SkipWhitespaceAndComments();
            }
            return new BadStmtNode(GetSpanFrom(start));
        }

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
