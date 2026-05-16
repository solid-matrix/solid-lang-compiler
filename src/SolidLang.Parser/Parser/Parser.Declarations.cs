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

        // Duplicate/misplaced namespace
        if (LookAheadKeyword("namespace"))
        {
            _diagnostics.DuplicateNamespace(GetCurrentSpan());
            ParseNamespaceDecl(); // consume for error recovery
            SkipWhitespaceAndComments();
        }

        // declarations*
        var declarations = new List<DeclNode>();
        while (Current != '\0')
        {
            SkipWhitespaceAndComments();
            if (Current == '\0')
                break;

            var savedPos = _position;
            var decl = ParseDeclaration();
            if (decl is not BadDeclNode)
                declarations.Add(decl);
            else if (Current != '\0')
            {
                // Error recovery: skip to next declaration
                SyncToNextDeclaration();
                // Safety: ensure progress to avoid infinite loop
                if (_position == savedPos)
                    Advance();
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
            return ParseVariableDecl(annotations, start, SyntaxKind.ConstKeyword);

        if (LookAheadKeyword("static"))
            return ParseVariableDecl(annotations, start, SyntaxKind.StaticKeyword);

        if (LookAheadKeyword("var"))
            return ParseVariableDecl(annotations, start, SyntaxKind.VarKeyword);

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

    private IReadOnlyList<CtAnnotateNode> ParseAnnotations()
    {
        var annotations = new List<CtAnnotateNode>();
        while (Current == '@')
        {
            annotations.Add(ParseAnnotation());
            SkipWhitespaceAndComments();
        }
        return annotations;
    }

    private CtAnnotateNode ParseAnnotation()
    {
        var start = _position;
        Advance(); // Skip @

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        var args = new List<CtAnnotateArgNode>();
        if (Current == '(')
        {
            Advance();
            SkipWhitespaceAndComments();

            if (Current != ')')
            {
                args.Add(ParseAnnotateArg());

                while (Current == ',')
                {
                    Advance();
                    SkipWhitespaceAndComments();
                    args.Add(ParseAnnotateArg());
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

    /// <summary>
    /// Tries to parse a named-type prefix for out-of-line declarations (NS::Type&lt;T&gt;::).
    /// Returns null if no :: follows the first identifier+generics (backtracks).
    /// Each segment may be followed by optional generic params.
    /// The last segment is the type name; earlier segments form the namespace prefix.
    /// </summary>
    private NamedTypeNode? TryParseNamedTypePrefix()
    {
        var savedPos = _position;
        var savedDiagCount = _diagnostics.Count;
        var id1 = ScanIdentifier();
        SkipWhitespaceAndComments();

        // Optional generics after first identifier
        if (Current == '<')
        {
            ParseGenericParams();
            SkipWhitespaceAndComments();
        }

        if (!Match("::"))
        {
            _position = savedPos;
            _diagnostics.TruncateTo(savedDiagCount);
            return null;
        }

        // Collect segments. The last segment will be the type name.
        var prefixStart = savedPos;
        var segments = new List<string> { id1 };
        // Track the start position of each potential type name
        var typeNameStart = savedPos;
        // Track the end of the prefix (right before the member name)
        var memberStart = _position;

        while (true)
        {
            SkipWhitespaceAndComments();
            var nextIdStart = _position;
            var segment = ScanIdentifier();
            SkipWhitespaceAndComments();

            // Optional generics after this segment
            if (Current == '<')
            {
                var segDiagCount = _diagnostics.Count;
                ParseGenericParams();
                SkipWhitespaceAndComments();

                // If :: doesn't follow, the generics were speculative — rollback diagnostics
                if (!(Current == ':' && Peek() == ':'))
                {
                    _diagnostics.TruncateTo(segDiagCount);
                }
            }

            if (Current == ':' && Peek() == ':')
            {
                segments.Add(segment);
                typeNameStart = nextIdStart;
                Advance(); Advance();
            }
            else
            {
                _position = nextIdStart;
                memberStart = nextIdStart;
                break;
            }
        }

        // The last segment is the type name, the rest form the namespace prefix
        var typeName = segments[^1];
        var nsSegmentCount = segments.Count - 1;

        // Build optional namespace prefix from the non-last segments
        NamespacePrefixNode? nsPrefix = null;
        if (nsSegmentCount > 0)
        {
            var nsSb = new StringBuilder();
            for (int i = 0; i < nsSegmentCount; i++)
            {
                if (i > 0) nsSb.Append("::");
                nsSb.Append(segments[i]);
            }
            var pathSpan = TextSpan.FromBounds(prefixStart, typeNameStart);
            var pathText = nsSb.ToString();
            var path = new NamespacePathNode(segments.Take(nsSegmentCount).ToList(), pathSpan, pathText);
            nsPrefix = new NamespacePrefixNode(path, pathSpan, pathText + "::");
        }

        // Re-read the type name at its saved position and parse proper type arguments.
        // Save and restore position so the caller sees the member name, not the end of type args.
        var savedAfterPrefix = _position; // equals memberStart
        _position = typeNameStart;
        SkipWhitespaceAndComments();
        typeName = ScanIdentifier();
        SkipWhitespaceAndComments();

        IReadOnlyList<TypeNode> typeArgs = Array.Empty<TypeNode>();
        int typeArgsEnd = _position;
        if (Current == '<')
        {
            typeArgs = ParseTypeArgumentList();
            typeArgsEnd = _position;
            SkipWhitespaceAndComments();
        }

        // Build the NamedType (spans from type start to just before the trailing ::)
        var namedTypeSpanStart = nsPrefix != null ? nsPrefix.Span.Start : typeNameStart;
        var namedTypeSpanEnd = typeArgs.Count > 0 ? typeArgsEnd : _position;
        var namedTypeSpan = TextSpan.FromBounds(namedTypeSpanStart, namedTypeSpanEnd);
        var namedTypeText = _source.GetText(namedTypeSpan);
        var namedType = new NamedTypeNode(nsPrefix, typeName, typeArgs, namedTypeSpan, namedTypeText);

        // Restore position so the caller can scan the member name
        _position = savedAfterPrefix;

        return namedType;
    }

    private FunctionDeclNode ParseFunctionDecl(IReadOnlyList<CtAnnotateNode> annotations, int start)
    {
        Match("func");
        SkipWhitespaceAndComments();

        var namedTypePrefix = TryParseNamedTypePrefix();
        SkipWhitespaceAndComments();
        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        // generic_params? (function-level generics)
        IReadOnlyList<GenericParamNode> genericParams = new List<GenericParamNode>();
        if (Current == '<')
        {
            genericParams = ParseGenericParams();
            SkipWhitespaceAndComments();
        }

        // ( func_parameters? )
        Expect('(');
        SkipWhitespaceAndComments();

        IReadOnlyList<FuncParameterNode> parameters = new List<FuncParameterNode>();
        if (Current != ')')
            parameters = ParseFuncParameters();

        Expect(')');
        SkipWhitespaceAndComments();

        // call_convention?
        var callConv = TryParseCallConvention();

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
        IReadOnlyList<WhereClauseNode> whereClauses = new List<WhereClauseNode>();
        if (LookAheadKeyword("where"))
            whereClauses = ParseWhereClauses();

        SkipWhitespaceAndComments();

        // body_stmt | ;
        BodyStmtNode? body = null;

        if (Current == '{')
        {
            body = ParseBodyStmt();
        }
        else if (Current == ';')
        {
            Advance();
        }
        else
        {
            _diagnostics.ExpectedToken(SyntaxKind.OpenBraceToken, SyntaxKind.BadToken, GetCurrentSpan());
        }

        var span = GetSpanFrom(start);
        var fullText = _source.GetText(span);
        return new FunctionDeclNode(annotations, namedTypePrefix, name, genericParams, parameters, callConv, returnType, whereClauses, body, span, fullText);
    }

    private IReadOnlyList<GenericParamNode> ParseGenericParams()
    {
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

        return paramList;
    }

    private IReadOnlyList<FuncParameterNode> ParseFuncParameters()
    {
        return ParseCommaSeparatedList(ParseFuncParameter);
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

    private IReadOnlyList<WhereClauseNode> ParseWhereClauses()
    {
        var clauses = new List<WhereClauseNode>();

        while (LookAheadKeyword("where"))
        {
            clauses.Add(ParseWhereClause());
            SkipWhitespaceAndComments();
        }

        return clauses;
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

    private StructDeclNode ParseStructDecl(IReadOnlyList<CtAnnotateNode> annotations, int start)
    {
        Match("struct");
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        IReadOnlyList<GenericParamNode> genericParams = new List<GenericParamNode>();
        if (Current == '<')
        {
            genericParams = ParseGenericParams();
            SkipWhitespaceAndComments();
        }

        IReadOnlyList<WhereClauseNode> whereClauses = new List<WhereClauseNode>();
        if (LookAheadKeyword("where"))
        {
            whereClauses = ParseWhereClauses();
            SkipWhitespaceAndComments();
        }

        IReadOnlyList<StructFieldNode> fields = new List<StructFieldNode>();

        if (Current == '{')
        {
            fields = ParseStructFields();
        }
        else if (Current == ';')
        {
            Advance();
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new StructDeclNode(annotations, name, genericParams, whereClauses, fields, span, text);
    }

    private IReadOnlyList<StructFieldNode> ParseStructFields()
    {
        Expect('{');
        SkipWhitespaceAndComments();

        var fields = ParseCommaSeparatedList(ParseStructField, '}');

        Expect('}');

        return fields;
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

    private EnumDeclNode ParseEnumDecl(IReadOnlyList<CtAnnotateNode> annotations, int start)
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

        IReadOnlyList<EnumFieldNode> fields = new List<EnumFieldNode>();

        if (Current == '{')
        {
            fields = ParseEnumFields();
        }
        else if (Current == ';')
        {
            Advance();
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new EnumDeclNode(annotations, name, underlyingType, fields, span, text);
    }

    private IReadOnlyList<EnumFieldNode> ParseEnumFields()
    {
        Expect('{');
        SkipWhitespaceAndComments();

        var fields = ParseCommaSeparatedList(ParseEnumField, '}');

        Expect('}');

        return fields;
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

    private UnionDeclNode ParseUnionDecl(IReadOnlyList<CtAnnotateNode> annotations, int start)
    {
        Match("union");
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        IReadOnlyList<GenericParamNode> genericParams = new List<GenericParamNode>();
        if (Current == '<')
        {
            genericParams = ParseGenericParams();
            SkipWhitespaceAndComments();
        }

        IReadOnlyList<WhereClauseNode> whereClauses = new List<WhereClauseNode>();
        if (LookAheadKeyword("where"))
        {
            whereClauses = ParseWhereClauses();
            SkipWhitespaceAndComments();
        }

        IReadOnlyList<UnionFieldNode> fields = new List<UnionFieldNode>();

        if (Current == '{')
        {
            fields = ParseUnionFields();
        }
        else if (Current == ';')
        {
            Advance();
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new UnionDeclNode(annotations, name, genericParams, whereClauses, fields, span, text);
    }

    private IReadOnlyList<UnionFieldNode> ParseUnionFields()
    {
        Expect('{');
        SkipWhitespaceAndComments();

        var fields = ParseCommaSeparatedList(ParseUnionField, '}');

        Expect('}');

        return fields;
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

    private VariantDeclNode ParseVariantDecl(IReadOnlyList<CtAnnotateNode> annotations, int start)
    {
        Match("variant");
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        IReadOnlyList<GenericParamNode> genericParams = new List<GenericParamNode>();
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

        IReadOnlyList<WhereClauseNode> whereClauses = new List<WhereClauseNode>();
        if (LookAheadKeyword("where"))
        {
            whereClauses = ParseWhereClauses();
            SkipWhitespaceAndComments();
        }

        IReadOnlyList<VariantFieldNode> fields = new List<VariantFieldNode>();

        if (Current == '{')
        {
            fields = ParseVariantFields();
        }
        else if (Current == ';')
        {
            Advance();
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new VariantDeclNode(annotations, name, genericParams, tagType, whereClauses, fields, span, text);
    }

    private IReadOnlyList<VariantFieldNode> ParseVariantFields()
    {
        Expect('{');
        SkipWhitespaceAndComments();

        var fields = ParseCommaSeparatedList(ParseVariantField, '}');

        Expect('}');

        return fields;
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

    private InterfaceDeclNode ParseInterfaceDecl(IReadOnlyList<CtAnnotateNode> annotations, int start)
    {
        Match("interface");
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        IReadOnlyList<GenericParamNode> genericParams = new List<GenericParamNode>();
        if (Current == '<')
        {
            genericParams = ParseGenericParams();
            SkipWhitespaceAndComments();
        }

        IReadOnlyList<WhereClauseNode> whereClauses = new List<WhereClauseNode>();
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
            var savedPos = _position;
            fields.Add(ParseInterfaceField());
            SkipWhitespaceAndComments();
            Expect(';');
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
        return new InterfaceDeclNode(annotations, name, genericParams, whereClauses, fields, span, text);
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

        IReadOnlyList<GenericParamNode> genericParams = new List<GenericParamNode>();
        if (Current == '<')
        {
            genericParams = ParseGenericParams();
            SkipWhitespaceAndComments();
        }

        Expect('(');
        SkipWhitespaceAndComments();

        IReadOnlyList<FuncParameterNode> parameters = new List<FuncParameterNode>();
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

    private static bool HasImportAnnotation(IReadOnlyList<CtAnnotateNode> annotations)
    {
        return annotations.Any(a => a.Name == "import");
    }

    private VariableDeclNode ParseVariableDecl(IReadOnlyList<CtAnnotateNode> annotations, int start, SyntaxKind keyword)
    {
        Match(keyword switch
        {
            SyntaxKind.ConstKeyword => "const",
            SyntaxKind.StaticKeyword => "static",
            SyntaxKind.VarKeyword => "var",
            _ => throw new ArgumentOutOfRangeException(nameof(keyword))
        });
        SkipWhitespaceAndComments();

        var namedTypePrefix = TryParseNamedTypePrefix();
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

        ExprNode? initializer = null;
        if (HasImportAnnotation(annotations) && keyword != SyntaxKind.VarKeyword)
        {
            // @import const/static — external declaration, no initializer
            Expect(';');
        }
        else
        {
            Expect('=');
            SkipWhitespaceAndComments();
            initializer = ParseExpression();
            SkipWhitespaceAndComments();
            Expect(';');
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new VariableDeclNode(annotations, keyword, namedTypePrefix, name, type, initializer, span, text);
    }

    // ========================================
    // Keywordless Function Declaration
    // ========================================

    /// <summary>
    /// Tries to parse a function declaration without the 'func' keyword.
    /// Pattern: TypeName&lt;generics&gt;::method_name(params): return_type { body }
    /// Returns null if the current tokens don't match this pattern.
    /// </summary>
    private FunctionDeclNode? TryParseKeywordlessFunctionDecl(IReadOnlyList<CtAnnotateNode> annotations, int start)
    {
        var savedPos = _position;
        var savedDiagCount = _diagnostics.Count;

        // Must see: identifier (<generics>)? :: identifier (
        var typeName = ScanIdentifier();
        if (string.IsNullOrEmpty(typeName))
        {
            _position = savedPos;
            _diagnostics.TruncateTo(savedDiagCount);
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
            _diagnostics.TruncateTo(savedDiagCount);
            return null;
        }
        Advance(); Advance(); // Skip ::
        SkipWhitespaceAndComments();

        var name = ScanIdentifier();
        if (string.IsNullOrEmpty(name))
        {
            _position = savedPos;
            _diagnostics.TruncateTo(savedDiagCount);
            return null;
        }
        SkipWhitespaceAndComments();

        // Must have ( for function parameters
        if (Current != '(')
        {
            _position = savedPos;
            _diagnostics.TruncateTo(savedDiagCount);
            return null;
        }

        // Commit: this is a function declaration without 'func' keyword
        Advance(); // Skip (
        SkipWhitespaceAndComments();

        IReadOnlyList<FuncParameterNode> parameters = new List<FuncParameterNode>();
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
        IReadOnlyList<WhereClauseNode> whereClauses = new List<WhereClauseNode>();
        if (LookAheadKeyword("where"))
            whereClauses = ParseWhereClauses();

        SkipWhitespaceAndComments();

        // Body or forward declaration
        BodyStmtNode? body = null;
        if (Current == '{')
        {
            body = ParseBodyStmt();
        }
        else if (Current == ';')
        {
            Advance();
        }

        var span = GetSpanFrom(start);
        var fullText = _source.GetText(span);
        return new FunctionDeclNode(annotations, null, name, new List<GenericParamNode>(), parameters, null, returnType, whereClauses, body, span, fullText);
    }

    // ========================================
    // Error Recovery
    // ========================================

    private void SyncToNextDeclaration()
    {
        var braceDepth = 0;
        while (Current != '\0')
        {
            // Track brace depth to avoid premature exit on nested blocks
            if (Current == '{')
            {
                braceDepth++;
                Advance();
                continue;
            }

            if (Current == '}')
            {
                if (braceDepth == 0)
                {
                    Advance();
                    return;
                }
                braceDepth--;
                Advance();
                continue;
            }

            // Only stop at ; or next declaration when not inside nested braces
            if (braceDepth == 0)
            {
                if (Current == ';')
                {
                    Advance();
                    return;
                }

                if (IsAtDeclarationKeyword())
                {
                    return;
                }
            }

            Advance();
        }
    }
}
