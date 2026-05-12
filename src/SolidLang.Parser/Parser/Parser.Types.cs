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

        // Function pointer type: *func(...) call_conv: type
        // Must be checked before plain pointer since both start with '*'
        if (Current == '*')
        {
            var savedPos = _position;
            Advance(); // skip *
            SkipWhitespaceAndComments();
            if (LookAheadKeyword("func"))
            {
                _position = savedPos;
                return ParseFuncPointerType();
            }
            _position = savedPos;

            // Pointer type: * type or *! type
            return ParsePointerType();
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
        SkipWhitespaceAndComments();
        Match("func");
        SkipWhitespaceAndComments();

        Expect('(');
        SkipWhitespaceAndComments();

        var paramTypes = ParseCommaSeparatedList(ParseType, ')');

        Expect(')');
        SkipWhitespaceAndComments();

        var callConv = TryParseCallConvention();

        Expect(':');
        SkipWhitespaceAndComments();

        var returnType = ParseType();

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new FuncPointerTypeNode(paramTypes, callConv, returnType, span, text);
    }

    /// <summary>
    /// Parses a simple named type (identifier + optional generics) without namespace prefix.
    /// Used in composite literal and switch pattern contexts where :: separates type from member.
    /// </summary>
    private NamedTypeNode ParseSimpleNamedType()
    {
        var start = _position;

        SkipWhitespaceAndComments();
        var name = ScanIdentifier();

        // Generic arguments: <T1, T2, ...>
        TypeArgumentListNode? typeArgs = null;
        if (name.Length > 0 && Current == '<')
        {
            _genericDepth++;
            Advance(); // Skip <
            SkipWhitespaceAndComments();

            var args = ParseCommaSeparatedList(ParseType);

            SkipWhitespaceAndComments();
            if (Current == '>')
            {
                Advance();
                _genericDepth = Math.Max(0, _genericDepth - 1);
            }
            else
            {
                _genericDepth = Math.Max(0, _genericDepth - 1);
                _diagnostics.MissingGreaterThan(GetCurrentSpan());
            }

            var argsSpan = TextSpan.FromBounds(start + name.Length + 1, _position);
            var argsText = _source.GetText(argsSpan);
            typeArgs = new TypeArgumentListNode(args, argsSpan, argsText);
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new NamedTypeNode(null, name, typeArgs, span, text);
    }

    /// <summary>
    /// Parses type argument list: &lt;T1, T2, ...&gt;
    /// Expects Current to be '&lt;'. Handles &gt;&gt; ambiguity via _genericDepth.
    /// </summary>
    internal TypeArgumentListNode ParseTypeArgumentList()
    {
        var start = _position;
        _genericDepth++;
        Advance(); // Skip <
        SkipWhitespaceAndComments();

        var args = ParseCommaSeparatedList(ParseType);

        SkipWhitespaceAndComments();
        if (Current == '>')
        {
            Advance();
            _genericDepth = Math.Max(0, _genericDepth - 1);
        }
        else
        {
            _genericDepth = Math.Max(0, _genericDepth - 1);
            _diagnostics.MissingGreaterThan(GetCurrentSpan());
        }

        var span = GetSpanFrom(start);
        var text = _source.GetText(span);
        return new TypeArgumentListNode(args, span, text);
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

            var args = ParseCommaSeparatedList(ParseType);

            // Handle closing >
            // In generic context, >> is treated as two > tokens
            SkipWhitespaceAndComments();
            if (Current == '>')
            {
                Advance();
                _genericDepth = Math.Max(0, _genericDepth - 1);
            }
            else
            {
                _genericDepth = Math.Max(0, _genericDepth - 1);
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
