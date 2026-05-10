using SolidLang.Parser;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Central registry for mapping SyntaxKind to built-in types and symbols.
/// </summary>
internal static class TypeFactory
{
    private static TypeSymbol[]? _primitiveTypeSymbols;

    /// <summary>
    /// All built-in primitive TypeSymbol instances.
    /// </summary>
    public static IReadOnlyList<TypeSymbol> PrimitiveTypeSymbols => _primitiveTypeSymbols!;

    /// <summary>
    /// Registers all built-in primitive types into a global scope.
    /// Must be called before binding begins.
    /// </summary>
    public static void RegisterPrimitives(Scope globalScope)
    {
        var types = new[]
        {
            TypeSymbol.CreatePrimitive("i8"),
            TypeSymbol.CreatePrimitive("i16"),
            TypeSymbol.CreatePrimitive("i32"),
            TypeSymbol.CreatePrimitive("i64"),
            TypeSymbol.CreatePrimitive("isize"),
            TypeSymbol.CreatePrimitive("u8"),
            TypeSymbol.CreatePrimitive("u16"),
            TypeSymbol.CreatePrimitive("u32"),
            TypeSymbol.CreatePrimitive("u64"),
            TypeSymbol.CreatePrimitive("usize"),
            TypeSymbol.CreatePrimitive("f32"),
            TypeSymbol.CreatePrimitive("f64"),
            TypeSymbol.CreatePrimitive("bool"),
        };

        foreach (var t in types)
            globalScope.Declare(t);

        _primitiveTypeSymbols = types;
    }

    /// <summary>
    /// Gets the PrimitiveTypeSymbol for a primitive keyword SyntaxKind.
    /// </summary>
    public static TypeSymbol? GetPrimitiveTypeSymbol(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.I8Keyword => FindPrimitive("i8"),
            SyntaxKind.I16Keyword => FindPrimitive("i16"),
            SyntaxKind.I32Keyword => FindPrimitive("i32"),
            SyntaxKind.I64Keyword => FindPrimitive("i64"),
            SyntaxKind.ISizeKeyword => FindPrimitive("isize"),
            SyntaxKind.U8Keyword => FindPrimitive("u8"),
            SyntaxKind.U16Keyword => FindPrimitive("u16"),
            SyntaxKind.U32Keyword => FindPrimitive("u32"),
            SyntaxKind.U64Keyword => FindPrimitive("u64"),
            SyntaxKind.USizeKeyword => FindPrimitive("usize"),
            SyntaxKind.F32Keyword => FindPrimitive("f32"),
            SyntaxKind.F64Keyword => FindPrimitive("f64"),
            SyntaxKind.BoolKeyword => FindPrimitive("bool"),
            _ => null,
        };
    }

    private static TypeSymbol? FindPrimitive(string name)
    {
        if (_primitiveTypeSymbols == null) return null;
        foreach (var t in _primitiveTypeSymbols)
            if (t.Name == name) return t;
        return null;
    }
}
