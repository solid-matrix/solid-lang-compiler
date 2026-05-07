using System.Text;
using SolidLang.Parser.Nodes;
using SolidLang.Parser.Nodes.Declarations;
using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Parser;

/// <summary>
/// Type parsing methods for the parser.
/// </summary>
public sealed partial class Parser
{
    // ========================================
    // Type Parsing
    // ========================================

    /// <summary>
    /// Parses a type.
    /// type: primitive_type | array_type | pointer_type | func_pointer_type | named_type
    /// </summary>
    private TypeNode ParseType()
    {
        SkipWhitespaceAndComments();

        // Check for primitive types first
        if (IsAtPrimitiveTypeKeyword())
        {
            return ParsePrimitiveType();
        }

        // Array type: [expr] type
        if (Current == '[')
        {
            return ParseArrayType();
        }

        // Pointer type: * type or *! type
        if (Current == '*')
        {
            return ParsePointerType();
        }

        // Function pointer type: *func(...) call_conv: type
        if (LookAheadKeyword("func"))
        {
            return ParseFuncPointerType();
        }

        // Named type (possibly with generic arguments)
        return ParseNamedType();
    }

    private TypeNode? TryParseType()
    {
        SkipWhitespaceAndComments();

        if (IsAtPrimitiveTypeKeyword() || char.IsLetter(Current) || Current == '[' || Current == '*')
        {
            return ParseType();
        }

        return null;
    }

    private PrimitiveTypeNode ParsePrimitiveType()
    {
        var start = _position;
        var kind = ScanKeyword();
        var span = GetSpanFrom(start);
        var text = _source.GetText(span);

        return new PrimitiveTypeNode(kind, span, text);
    }

    private ArrayTypeNode ParseArrayType()
    {
        var start = _position;

        Expect('[');
        SkipWhitespaceAndComments();

        var size = ParseExpression();
        SkipWhitespaceAndComments();

        Expect(']');
        SkipWhitespaceAndComments();

        var elementType = ParseType();

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new ArrayTypeNode(size, elementType, span, text);
    }

    private PointerTypeNode ParsePointerType()
    {
        var start = _position;

        Expect('*');
        SkipWhitespaceAndComments();

        var hasWritePermission = false;
        if (Current == '!')
        {
            Advance();
            hasWritePermission = true;
            SkipWhitespaceAndComments();
        }

        var pointeeType = ParseType();

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new PointerTypeNode(hasWritePermission, pointeeType, span, text);
    }

    private FuncPointerTypeNode ParseFuncPointerType()
    {
        var start = _position;

        Expect('*');
        Match("func");
        SkipWhitespaceAndComments();

        Expect('(');
        SkipWhitespaceAndComments();

        var paramTypes = new List<TypeNode>();
        if (Current != ')')
        {
            paramTypes.Add(ParseType());
            SkipWhitespaceAndComments();

            while (Current == ',')
            {
                Advance();
                SkipWhitespaceAndComments();
                paramTypes.Add(ParseType());
                SkipWhitespaceAndComments();
            }
        }

        Expect(')');
        SkipWhitespaceAndComments();

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

        Expect(':');
        SkipWhitespaceAndComments();

        var returnType = ParseType();

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new FuncPointerTypeNode(paramTypes, callConv, returnType, span, text);
    }

    /// <summary>
    /// Parses a named type, potentially with generic arguments.
    /// This is the key method that handles the >> ambiguity.
    /// </summary>
    private NamedTypeNode ParseNamedType()
    {
        var start = _position;

        // namespace_prefix?
        NamespacePrefixNode? nsPrefix = null;

        // Check if this is a namespace-qualified type
        var savedPos = _position;
        var id1 = ScanIdentifier();
        SkipWhitespaceAndComments();

        if (Match("::"))
        {
            // It was a namespace prefix
            var pathSegments = new List<string> { id1 };
            var sb = new StringBuilder(id1);

            while (true)
            {
                SkipWhitespaceAndComments();

                // Check if next identifier is followed by :: or <
                var nextIdStart = _position;
                var nextId = ScanIdentifier();
                SkipWhitespaceAndComments();

                if (Current == ':' && Peek() == ':')
                {
                    // Continue the namespace path
                    pathSegments.Add(nextId);
                    sb.Append("::");
                    sb.Append(nextId);
                    Advance(); Advance(); // Skip ::
                }
                else
                {
                    // This is the type name, backtrack
                    _position = nextIdStart;
                    break;
                }
            }

            var pathSpan = TextSpan.FromBounds(savedPos, _position);
            var pathText = sb.ToString();
            var path = new NamespacePathNode(pathSegments, pathSpan, pathText);
            nsPrefix = new NamespacePrefixNode(path, pathSpan, pathText + "::");
        }
        else
        {
            // Not a namespace prefix, backtrack
            _position = savedPos;
        }

        SkipWhitespaceAndComments();
        var name = ScanIdentifier();
        SkipWhitespaceAndComments();

        // Generic arguments: <T1, T2, ...>
        TypeArgumentListNode? typeArgs = null;
        if (name.Length > 0 && Current == '<')
        {
            // ========================================
            // ENTER GENERIC CONTEXT
            // This is where the >> ambiguity is resolved!
            // ========================================
            _genericDepth++;
            Advance(); // Skip <
            SkipWhitespaceAndComments();

            var args = new List<TypeNode>();
            args.Add(ParseType());
            SkipWhitespaceAndComments();

            while (Current == ',')
            {
                Advance();
                SkipWhitespaceAndComments();
                args.Add(ParseType());
                SkipWhitespaceAndComments();
            }

            // Handle closing >
            // In generic context, >> is treated as two > tokens
            SkipWhitespaceAndComments();
            if (Current == '>')
            {
                Advance();
                _genericDepth--;
            }
            else
            {
                _genericDepth--;
                _diagnostics.MissingGreaterThan(GetCurrentSpan());
            }

            var argsSpan = TextSpan.FromBounds(start + name.Length + 1, _position);
            var argsText = _source.GetText(argsSpan);
            typeArgs = new TypeArgumentListNode(args, argsSpan, argsText);
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new NamedTypeNode(nsPrefix, name, typeArgs, span, text);
    }
}
