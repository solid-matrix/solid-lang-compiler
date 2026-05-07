using System.Text;
using SolidLang.Parser.Nodes;
using SolidLang.Parser.Nodes.Declarations;
using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Literals;
using SolidLang.Parser.Nodes.Statements;
using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Parser;

/// <summary>
/// Declaration parsing methods for the parser.
/// </summary>
public sealed partial class Parser
{
    // ========================================
    // Entry Point
    // ========================================

    /// <summary>
    /// Parses the entire source file.
    /// </summary>
    public ProgramNode ParseProgram()
    {
        var start = _position;
        SkipWhitespaceAndComments();

        // namespace_decl?
        NamespaceDeclNode? ns = null;
        if (LookAheadKeyword("namespace"))
        {
            ns = ParseNamespaceDecl();
            SkipWhitespaceAndComments();
        }

        // using_decl*
        var usings = new List<UsingDeclNode>();
        while (LookAheadKeyword("using"))
        {
            usings.Add(ParseUsingDecl());
            SkipWhitespaceAndComments();
        }

        // declarations*
        var declarations = new List<DeclNode>();
        while (Current != '\0')
        {
            SkipWhitespaceAndComments();
            if (Current == '\0')
                break;

            var decl = ParseDeclaration();
            if (decl is not BadDeclNode)
                declarations.Add(decl);
            else if (Current != '\0')
            {
                // Error recovery: skip to next declaration
                SyncToNextDeclaration();
            }
        }

        var span = GetSpanFrom(start);
        return new ProgramNode(ns, usings, declarations, _source, span);
    }

    // ========================================
    // Namespace and Using
    // ========================================

    private NamespaceDeclNode ParseNamespaceDecl()
    {
        var start = _position;
        Match("namespace");
        SkipWhitespaceAndComments();

        var path = ParseNamespacePath();
        SkipWhitespaceAndComments();
        Expect(';');

        var span = GetSpanFrom(start);
        return new NamespaceDeclNode(path, span);
    }

    private UsingDeclNode ParseUsingDecl()
    {
        var start = _position;
        Match("using");
        SkipWhitespaceAndComments();

        var path = ParseNamespacePath();
        SkipWhitespaceAndComments();
        Expect(';');

        var span = GetSpanFrom(start);
        return new UsingDeclNode(path, span);
    }

    private NamespacePathNode ParseNamespacePath()
    {
        var start = _position;
        var segments = new List<string>();
        var sb = new StringBuilder();

        segments.Add(ScanIdentifier());
        sb.Append(segments[0]);

        while (true)
        {
            SkipWhitespaceAndComments();
            if (!Match("::"))
                break;

            SkipWhitespaceAndComments();
            var segment = ScanIdentifier();
            segments.Add(segment);
            sb.Append("::");
            sb.Append(segment);
        }

        var span = GetSpanFrom(start);
        return new NamespacePathNode(segments, span, sb.ToString());
    }

    // ========================================
    // Declarations
    // ========================================

    private DeclNode ParseDeclaration()
    {
        SkipWhitespaceAndComments();
        var start = _position;

        // Parse annotations
        var annotations = ParseAnnotations();

        SkipWhitespaceAndComments();

        // Check for declaration keywords
        if (LookAheadKeyword("func"))
            return ParseFunctionDecl(annotations, start);

        if (LookAheadKeyword("struct"))
            return ParseStructDecl(annotations, start);

        if (LookAheadKeyword("enum"))
            return ParseEnumDecl(annotations, start);

        if (LookAheadKeyword("union"))
            return ParseUnionDecl(annotations, start);

        if (LookAheadKeyword("variant"))
            return ParseVariantDecl(annotations, start);

        if (LookAheadKeyword("interface"))
            return ParseInterfaceDecl(annotations, start);

        if (LookAheadKeyword("const"))
            return ParseConstDecl(annotations, start);

        if (LookAheadKeyword("static"))
            return ParseStaticDecl(annotations, start);

        if (LookAheadKeyword("var"))
            return ParseVarDecl(annotations, start);

        // Check for function definition without 'func' keyword:
        // TypeName<generics>::method_name(params): return_type { body }
        if (char.IsLetter(Current) || Current == '_')
        {
            var decl = TryParseKeywordlessFunctionDecl(annotations, start);
            if (decl != null)
                return decl;
        }

        // Error
        _diagnostics.ExpectedDeclaration(GetCurrentSpan());
        return new BadDeclNode(GetCurrentSpan());
    }

    private CtAnnotatesNode? ParseAnnotations()
    {
        var annotations = new List<CtAnnotateNode>();

        while (Current == '@')
        {
            annotations.Add(ParseAnnotation());
            SkipWhitespaceAndComments();
        }

        if (annotations.Count == 0)
            return null;

        var start = annotations[0].Span.Start;
        var end = annotations[^1].Span.End;
        var span = TextSpan.FromBounds(start, end);
        var text = _source.GetText(span);

        return new CtAnnotatesNode(annotations, span, text);
    }

    private CtAnnotateNode ParseAnnotation()
    {
        var start = _position;
        Advance(); // Skip @

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        CtAnnotateArgsNode? args = null;
        if (Current == '(')
        {
            Advance();
            SkipWhitespaceAndComments();

            if (Current != ')')
            {
                var argList = new List<CtAnnotateArgNode>();
                argList.Add(ParseAnnotateArg());

                while (Current == ',')
                {
                    Advance();
                    SkipWhitespaceAndComments();
                    argList.Add(ParseAnnotateArg());
                }
            }

            SkipWhitespaceAndComments();
            Expect(')');
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new CtAnnotateNode(name, args, span, text);
    }

    private CtAnnotateArgNode ParseAnnotateArg()
    {
        var start = _position;

        // Try to parse as type first
        if (IsAtPrimitiveTypeKeyword() || char.IsLetter(Current) || Current == '[' || Current == '*')
        {
            // Ambiguous: could be type or expression
            // For simplicity, try type first
            var type = TryParseType();
            if (type != null)
            {
                var span = GetSpanFrom(start);
                return new CtAnnotateArgNode(type, null, span, _source.GetText(span));
            }
        }

        // Parse as expression
        var expr = ParseExpression();
        var span2 = GetSpanFrom(start);
        return new CtAnnotateArgNode(null, expr, span2, _source.GetText(span2));
    }

    // ========================================
    // Function Declaration
    // ========================================

    private FunctionDeclNode ParseFunctionDecl(CtAnnotatesNode? annotations, int start)
    {
        Match("func");
        SkipWhitespaceAndComments();

        // namespace_prefix?
        NamespacePrefixNode? nsPrefix = null;
        var savedPos = _position;
        var id1 = ScanIdentifier();
        SkipWhitespaceAndComments();

        if (Match("::"))
        {
            // It was a namespace prefix
            var pathStart = savedPos;
            var segments = new List<string> { id1 };
            var sb = new StringBuilder(id1);

            while (true)
            {
                SkipWhitespaceAndComments();
                var nextIdStart = _position;
                var segment = ScanIdentifier();
                SkipWhitespaceAndComments();

                // Only add as namespace segment if followed by ::
                if (Current == ':' && Peek() == ':')
                {
                    segments.Add(segment);
                    sb.Append("::");
                    sb.Append(segment);
                    Advance(); Advance(); // Skip ::
                }
                else
                {
                    // This segment is the actual name, backtrack
                    _position = nextIdStart;
                    break;
                }
            }

            var pathSpan = TextSpan.FromBounds(pathStart, _position);
            var path = new NamespacePathNode(segments, pathSpan, sb.ToString());
            nsPrefix = new NamespacePrefixNode(path, pathSpan, sb.ToString() + "::");
        }
        else
        {
            // Not a namespace prefix, backtrack
            _position = savedPos;
        }

        SkipWhitespaceAndComments();
        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        // generic_params?
        GenericParamsNode? genericParams = null;
        if (Current == '<')
        {
            genericParams = ParseGenericParams();
            SkipWhitespaceAndComments();
        }

        // Handle Type<T>::method_name pattern
        // This is used for member functions defined outside the type body
        if (genericParams != null && Current == ':' && Peek() == ':')
        {
            Advance(); Advance(); // Skip ::
            SkipWhitespaceAndComments();
            name = ScanIdentifier();
            SkipWhitespaceAndComments();
        }

        // ( func_parameters? )
        Expect('(');
        SkipWhitespaceAndComments();

        FuncParametersNode? parameters = null;
        if (Current != ')')
            parameters = ParseFuncParameters();

        Expect(')');
        SkipWhitespaceAndComments();

        // call_convention?
        CallConventionNode? callConv = null;
        if (LookAheadKeyword("cdecl") || LookAheadKeyword("stdcall"))
        {
            var convStart = _position;
            var convKind = ScanKeyword();
            var convSpan = GetSpanFrom(convStart);
            var convText = _source.GetText(convSpan);
            callConv = new CallConventionNode(convKind, convSpan, convText);
            SkipWhitespaceAndComments();
        }

        // : type?
        TypeNode? returnType = null;
        if (Current == ':')
        {
            Advance();
            SkipWhitespaceAndComments();
            returnType = ParseType();
            SkipWhitespaceAndComments();
        }

        // where_clauses?
        WhereClausesNode? whereClauses = null;
        if (LookAheadKeyword("where"))
            whereClauses = ParseWhereClauses();

        SkipWhitespaceAndComments();

        // body_stmt | ;
        BodyStmtNode? body = null;
        bool isForward = false;

        if (Current == '{')
        {
            body = ParseBodyStmt();
        }
        else if (Current == ';')
        {
            Advance();
            isForward = true;
        }
        else
        {
            _diagnostics.ExpectedToken(SyntaxKind.OpenBraceToken, SyntaxKind.BadToken, GetCurrentSpan());
        }

        var span = GetSpanFrom(start);
        var fullText = _source.GetText(span);
        return new FunctionDeclNode(annotations, nsPrefix, name, genericParams, parameters, callConv, returnType, whereClauses, body, isForward, span, fullText);
    }

    private GenericParamsNode ParseGenericParams()
    {
        var start = _position;
        Expect('<');
        SkipWhitespaceAndComments();

        var paramStart = _position;
        var paramList = new List<GenericParamNode>();

        // First parameter
        var paramName = ScanIdentifier();
        var paramSpan = GetSpanFrom(paramStart);
        paramList.Add(new GenericParamNode(paramName, paramSpan));

        SkipWhitespaceAndComments();

        while (Current == ',')
        {
            Advance();
            SkipWhitespaceAndComments();

            paramStart = _position;
            paramName = ScanIdentifier();
            paramSpan = GetSpanFrom(paramStart);
            paramList.Add(new GenericParamNode(paramName, paramSpan));

            SkipWhitespaceAndComments();
        }

        // Handle > or >> (in generic context)
        SkipWhitespaceAndComments();
        if (Current == '>')
        {
            Advance();
        }
        else
        {
            _diagnostics.MissingGreaterThan(GetCurrentSpan());
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new GenericParamsNode(paramList, span, text);
    }

    private FuncParametersNode ParseFuncParameters()
    {
        var start = _position;
        var parameters = new List<FuncParameterNode>();

        parameters.Add(ParseFuncParameter());
        SkipWhitespaceAndComments();

        while (Current == ',')
        {
            Advance();
            SkipWhitespaceAndComments();
            parameters.Add(ParseFuncParameter());
            SkipWhitespaceAndComments();
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new FuncParametersNode(parameters, span, text);
    }

    private FuncParameterNode ParseFuncParameter()
    {
        var start = _position;
        var annotations = ParseAnnotations();
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        Expect(':');
        SkipWhitespaceAndComments();

        var type = ParseType();

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new FuncParameterNode(annotations, name, type, span, text);
    }

    private WhereClausesNode ParseWhereClauses()
    {
        var clauses = new List<WhereClauseNode>();

        while (LookAheadKeyword("where"))
        {
            clauses.Add(ParseWhereClause());
            SkipWhitespaceAndComments();
        }

        var start = clauses[0].Span.Start;
        var end = clauses[^1].Span.End;
        var span = TextSpan.FromBounds(start, end);
        var text = _source.GetText(span);

        return new WhereClausesNode(clauses, span, text);
    }

    private WhereClauseNode ParseWhereClause()
    {
        var start = _position;
        Match("where");
        SkipWhitespaceAndComments();

        var typeParamName = ScanIdentifier();
        SkipWhitespaceAndComments();

        Expect(':');
        SkipWhitespaceAndComments();

        var constraintType = ParseType();

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new WhereClauseNode(typeParamName, constraintType, span, text);
    }

    // ========================================
    // Type Declarations
    // ========================================

    private StructDeclNode ParseStructDecl(CtAnnotatesNode? annotations, int start)
    {
        Match("struct");
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        GenericParamsNode? genericParams = null;
        if (Current == '<')
        {
            genericParams = ParseGenericParams();
            SkipWhitespaceAndComments();
        }

        WhereClausesNode? whereClauses = null;
        if (LookAheadKeyword("where"))
        {
            whereClauses = ParseWhereClauses();
            SkipWhitespaceAndComments();
        }

        StructFieldsNode? fields = null;
        bool isForward = false;

        if (Current == '{')
        {
            fields = ParseStructFields();
        }
        else if (Current == ';')
        {
            Advance();
            isForward = true;
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new StructDeclNode(annotations, name, genericParams, whereClauses, fields, isForward, span, text);
    }

    private StructFieldsNode ParseStructFields()
    {
        var start = _position;
        Expect('{');
        SkipWhitespaceAndComments();

        var fields = new List<StructFieldNode>();

        if (Current != '}')
        {
            fields.Add(ParseStructField());
            SkipWhitespaceAndComments();

            while (Current == ',')
            {
                Advance();
                SkipWhitespaceAndComments();
                if (Current == '}')
                    break;

                fields.Add(ParseStructField());
                SkipWhitespaceAndComments();
            }
        }

        Expect('}');

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new StructFieldsNode(fields, span, text);
    }

    private StructFieldNode ParseStructField()
    {
        var start = _position;
        var annotations = ParseAnnotations();
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        Expect(':');
        SkipWhitespaceAndComments();

        var type = ParseType();

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new StructFieldNode(annotations, name, type, span, text);
    }

    // Similar implementations for Enum, Union, Variant, Interface...
    // (abbreviated for brevity, full implementation would follow same pattern)

    private EnumDeclNode ParseEnumDecl(CtAnnotatesNode? annotations, int start)
    {
        Match("enum");
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        TypeNode? underlyingType = null;
        if (Current == ':')
        {
            Advance();
            SkipWhitespaceAndComments();
            underlyingType = ParseType();
            SkipWhitespaceAndComments();
        }

        EnumFieldsNode? fields = null;
        bool isForward = false;

        if (Current == '{')
        {
            fields = ParseEnumFields();
        }
        else if (Current == ';')
        {
            Advance();
            isForward = true;
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new EnumDeclNode(annotations, name, underlyingType, fields, isForward, span, text);
    }

    private EnumFieldsNode ParseEnumFields()
    {
        var start = _position;
        Expect('{');
        SkipWhitespaceAndComments();

        var fields = new List<EnumFieldNode>();

        if (Current != '}')
        {
            fields.Add(ParseEnumField());
            SkipWhitespaceAndComments();

            while (Current == ',')
            {
                Advance();
                SkipWhitespaceAndComments();
                if (Current == '}')
                    break;

                fields.Add(ParseEnumField());
                SkipWhitespaceAndComments();
            }
        }

        Expect('}');

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new EnumFieldsNode(fields, span, text);
    }

    private EnumFieldNode ParseEnumField()
    {
        var start = _position;
        var annotations = ParseAnnotations();
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        ExprNode? value = null;
        if (Current == '=')
        {
            Advance();
            SkipWhitespaceAndComments();
            value = ParseExpression();
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new EnumFieldNode(annotations, name, value, span, text);
    }

    private UnionDeclNode ParseUnionDecl(CtAnnotatesNode? annotations, int start)
    {
        Match("union");
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        GenericParamsNode? genericParams = null;
        if (Current == '<')
        {
            genericParams = ParseGenericParams();
            SkipWhitespaceAndComments();
        }

        WhereClausesNode? whereClauses = null;
        if (LookAheadKeyword("where"))
        {
            whereClauses = ParseWhereClauses();
            SkipWhitespaceAndComments();
        }

        UnionFieldsNode? fields = null;
        bool isForward = false;

        if (Current == '{')
        {
            fields = ParseUnionFields();
        }
        else if (Current == ';')
        {
            Advance();
            isForward = true;
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new UnionDeclNode(annotations, name, genericParams, whereClauses, fields, isForward, span, text);
    }

    private UnionFieldsNode ParseUnionFields()
    {
        var start = _position;
        Expect('{');
        SkipWhitespaceAndComments();

        var fields = new List<UnionFieldNode>();

        if (Current != '}')
        {
            fields.Add(ParseUnionField());
            SkipWhitespaceAndComments();

            while (Current == ',')
            {
                Advance();
                SkipWhitespaceAndComments();
                if (Current == '}')
                    break;

                fields.Add(ParseUnionField());
                SkipWhitespaceAndComments();
            }
        }

        Expect('}');

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new UnionFieldsNode(fields, span, text);
    }

    private UnionFieldNode ParseUnionField()
    {
        var start = _position;
        var annotations = ParseAnnotations();
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        Expect(':');
        SkipWhitespaceAndComments();

        var type = ParseType();

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new UnionFieldNode(annotations, name, type, span, text);
    }

    private VariantDeclNode ParseVariantDecl(CtAnnotatesNode? annotations, int start)
    {
        Match("variant");
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        GenericParamsNode? genericParams = null;
        if (Current == '<')
        {
            genericParams = ParseGenericParams();
            SkipWhitespaceAndComments();
        }

        TypeNode? tagType = null;
        if (Current == ':')
        {
            Advance();
            SkipWhitespaceAndComments();
            tagType = ParseType();
            SkipWhitespaceAndComments();
        }

        WhereClausesNode? whereClauses = null;
        if (LookAheadKeyword("where"))
        {
            whereClauses = ParseWhereClauses();
            SkipWhitespaceAndComments();
        }

        VariantFieldsNode? fields = null;
        bool isForward = false;

        if (Current == '{')
        {
            fields = ParseVariantFields();
        }
        else if (Current == ';')
        {
            Advance();
            isForward = true;
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new VariantDeclNode(annotations, name, genericParams, tagType, whereClauses, fields, isForward, span, text);
    }

    private VariantFieldsNode ParseVariantFields()
    {
        var start = _position;
        Expect('{');
        SkipWhitespaceAndComments();

        var fields = new List<VariantFieldNode>();

        if (Current != '}')
        {
            fields.Add(ParseVariantField());
            SkipWhitespaceAndComments();

            while (Current == ',')
            {
                Advance();
                SkipWhitespaceAndComments();
                if (Current == '}')
                    break;

                fields.Add(ParseVariantField());
                SkipWhitespaceAndComments();
            }
        }

        Expect('}');

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new VariantFieldsNode(fields, span, text);
    }

    private VariantFieldNode ParseVariantField()
    {
        var start = _position;
        var annotations = ParseAnnotations();
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        TypeNode? type = null;
        if (Current == ':')
        {
            Advance();
            SkipWhitespaceAndComments();
            type = ParseType();
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new VariantFieldNode(annotations, name, type, span, text);
    }

    private InterfaceDeclNode ParseInterfaceDecl(CtAnnotatesNode? annotations, int start)
    {
        Match("interface");
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        GenericParamsNode? genericParams = null;
        if (Current == '<')
        {
            genericParams = ParseGenericParams();
            SkipWhitespaceAndComments();
        }

        WhereClausesNode? whereClauses = null;
        if (LookAheadKeyword("where"))
        {
            whereClauses = ParseWhereClauses();
            SkipWhitespaceAndComments();
        }

        Expect('{');
        SkipWhitespaceAndComments();

        var fields = new List<InterfaceFieldNode>();
        while (Current != '}' && Current != '\0')
        {
            fields.Add(ParseInterfaceField());
            SkipWhitespaceAndComments();
            Expect(';');
            SkipWhitespaceAndComments();
        }

        Expect('}');

        var fieldsStart = fields.Count > 0 ? fields[0].Span.Start : _position - 1;
        var fieldsEnd = fields.Count > 0 ? fields[^1].Span.End : _position;
        var fieldsSpan = TextSpan.FromBounds(fieldsStart, fieldsEnd);
        var fieldsNode = new InterfaceFieldsNode(fields, fieldsSpan, _source.GetText(fieldsSpan));

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new InterfaceDeclNode(annotations, name, genericParams, whereClauses, fieldsNode, span, text);
    }

    private InterfaceFieldNode ParseInterfaceField()
    {
        var start = _position;
        var annotations = ParseAnnotations();
        SkipWhitespaceAndComments();

        Match("func");
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        GenericParamsNode? genericParams = null;
        if (Current == '<')
        {
            genericParams = ParseGenericParams();
            SkipWhitespaceAndComments();
        }

        Expect('(');
        SkipWhitespaceAndComments();

        FuncParametersNode? parameters = null;
        if (Current != ')')
            parameters = ParseFuncParameters();

        Expect(')');
        SkipWhitespaceAndComments();

        TypeNode? returnType = null;
        if (Current == ':')
        {
            Advance();
            SkipWhitespaceAndComments();
            returnType = ParseType();
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new InterfaceFieldNode(annotations, name, genericParams, parameters, returnType, span, text);
    }

    // ========================================
    // Variable Declarations
    // ========================================

    private ConstDeclNode ParseConstDecl(CtAnnotatesNode? annotations, int start)
    {
        Match("const");
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        // Handle Type<generics>::member_name and Type::member_name patterns
        if (Current == '<')
        {
            ParseGenericParams();
            SkipWhitespaceAndComments();
        }

        if (Current == ':' && Peek() == ':')
        {
            Advance(); Advance();
            SkipWhitespaceAndComments();
            name = ScanIdentifier();
            SkipWhitespaceAndComments();
        }

        TypeNode? type = null;
        if (Current == ':')
        {
            Advance();
            SkipWhitespaceAndComments();
            type = ParseType();
            SkipWhitespaceAndComments();
        }

        Expect('=');
        SkipWhitespaceAndComments();

        var initializer = ParseExpression();
        SkipWhitespaceAndComments();

        Expect(';');

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new ConstDeclNode(annotations, name, type, initializer, span, text);
    }

    private StaticDeclNode ParseStaticDecl(CtAnnotatesNode? annotations, int start)
    {
        Match("static");
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        TypeNode? type = null;
        if (Current == ':')
        {
            Advance();
            SkipWhitespaceAndComments();
            type = ParseType();
            SkipWhitespaceAndComments();
        }

        Expect('=');
        SkipWhitespaceAndComments();

        var initializer = ParseExpression();
        SkipWhitespaceAndComments();

        Expect(';');

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new StaticDeclNode(annotations, name, type, initializer, span, text);
    }

    private VarDeclNode ParseVarDecl(CtAnnotatesNode? annotations, int start)
    {
        Match("var");
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        TypeNode? type = null;
        if (Current == ':')
        {
            Advance();
            SkipWhitespaceAndComments();
            type = ParseType();
            SkipWhitespaceAndComments();
        }

        Expect('=');
        SkipWhitespaceAndComments();

        var initializer = ParseExpression();
        SkipWhitespaceAndComments();

        Expect(';');

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new VarDeclNode(annotations, name, type, initializer, span, text);
    }

    // ========================================
    // Keywordless Function Declaration
    // ========================================

    /// <summary>
    /// Tries to parse a function declaration without the 'func' keyword.
    /// Pattern: TypeName&lt;generics&gt;::method_name(params): return_type { body }
    /// Returns null if the current tokens don't match this pattern.
    /// </summary>
    private FunctionDeclNode? TryParseKeywordlessFunctionDecl(CtAnnotatesNode? annotations, int start)
    {
        var savedPos = _position;

        // Must see: identifier (<generics>)? :: identifier (
        var typeName = ScanIdentifier();
        if (string.IsNullOrEmpty(typeName))
        {
            _position = savedPos;
            return null;
        }
        SkipWhitespaceAndComments();

        // Optional generic params: <T, U, ...>
        if (Current == '<')
        {
            ParseGenericParams();
            SkipWhitespaceAndComments();
        }

        // Must have :: followed by method name
        if (Current != ':' || Peek() != ':')
        {
            _position = savedPos;
            return null;
        }
        Advance(); Advance(); // Skip ::
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        if (string.IsNullOrEmpty(name))
        {
            _position = savedPos;
            return null;
        }
        SkipWhitespaceAndComments();

        // Must have ( for function parameters
        if (Current != '(')
        {
            _position = savedPos;
            return null;
        }

        // Commit: this is a function declaration without 'func' keyword
        Advance(); // Skip (
        SkipWhitespaceAndComments();

        FuncParametersNode? parameters = null;
        if (Current != ')')
            parameters = ParseFuncParameters();

        Expect(')');
        SkipWhitespaceAndComments();

        // Optional : return_type
        TypeNode? returnType = null;
        if (Current == ':')
        {
            Advance();
            SkipWhitespaceAndComments();
            returnType = ParseType();
            SkipWhitespaceAndComments();
        }

        // Optional where clauses
        WhereClausesNode? whereClauses = null;
        if (LookAheadKeyword("where"))
            whereClauses = ParseWhereClauses();

        SkipWhitespaceAndComments();

        // Body or forward declaration
        BodyStmtNode? body = null;
        bool isForward = false;
        if (Current == '{')
        {
            body = ParseBodyStmt();
        }
        else if (Current == ';')
        {
            Advance();
            isForward = true;
        }

        var span = GetSpanFrom(start);
        var fullText = _source.GetText(span);
        return new FunctionDeclNode(annotations, null, name, null, parameters, null, returnType, whereClauses, body, isForward, span, fullText);
    }

    // ========================================
    // Error Recovery
    // ========================================

    private void SyncToNextDeclaration()
    {
        while (Current != '\0')
        {
            if (Current == ';')
            {
                Advance();
                return;
            }

            if (Current == '}')
            {
                Advance();
                return;
            }

            if (IsAtDeclarationKeyword())
            {
                return;
            }

            Advance();
        }
    }
}
