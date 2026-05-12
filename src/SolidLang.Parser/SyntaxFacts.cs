using System.Collections.Immutable;

namespace SolidLang.Parser;

/// <summary>
/// Provides lookup tables and utility methods for syntax elements.
/// </summary>
public static class SyntaxFacts
{
    private static readonly ImmutableDictionary<string, SyntaxKind> s_keywords =
        new Dictionary<string, SyntaxKind>
        {
            // Keywords
            ["namespace"] = SyntaxKind.NamespaceKeyword,
            ["using"] = SyntaxKind.UsingKeyword,
            ["func"] = SyntaxKind.FuncKeyword,
            ["interface"] = SyntaxKind.InterfaceKeyword,
            ["struct"] = SyntaxKind.StructKeyword,
            ["enum"] = SyntaxKind.EnumKeyword,
            ["union"] = SyntaxKind.UnionKeyword,
            ["variant"] = SyntaxKind.VariantKeyword,
            ["var"] = SyntaxKind.VarKeyword,
            ["const"] = SyntaxKind.ConstKeyword,
            ["static"] = SyntaxKind.StaticKeyword,
            ["if"] = SyntaxKind.IfKeyword,
            ["else"] = SyntaxKind.ElseKeyword,
            ["for"] = SyntaxKind.ForKeyword,
            ["switch"] = SyntaxKind.SwitchKeyword,
            ["break"] = SyntaxKind.BreakKeyword,
            ["continue"] = SyntaxKind.ContinueKeyword,
            ["return"] = SyntaxKind.ReturnKeyword,
            ["defer"] = SyntaxKind.DeferKeyword,
            ["where"] = SyntaxKind.WhereKeyword,
            ["null"] = SyntaxKind.NullKeyword,
            ["true"] = SyntaxKind.TrueKeyword,
            ["false"] = SyntaxKind.FalseKeyword,
            ["cdecl"] = SyntaxKind.CDeclKeyword,
            ["stdcall"] = SyntaxKind.StdCallKeyword,

            // Primitive Types
            ["u8"] = SyntaxKind.U8Keyword,
            ["u16"] = SyntaxKind.U16Keyword,
            ["u32"] = SyntaxKind.U32Keyword,
            ["u64"] = SyntaxKind.U64Keyword,
            ["usize"] = SyntaxKind.USizeKeyword,
            ["i8"] = SyntaxKind.I8Keyword,
            ["i16"] = SyntaxKind.I16Keyword,
            ["i32"] = SyntaxKind.I32Keyword,
            ["i64"] = SyntaxKind.I64Keyword,
            ["isize"] = SyntaxKind.ISizeKeyword,
            ["f32"] = SyntaxKind.F32Keyword,
            ["f64"] = SyntaxKind.F64Keyword,
            ["bool"] = SyntaxKind.BoolKeyword,
        }.ToImmutableDictionary();

    private static readonly ImmutableDictionary<string, SyntaxKind> s_integerTypeSuffixes =
        new Dictionary<string, SyntaxKind>
        {
            ["u8"] = SyntaxKind.U8Keyword,
            ["u16"] = SyntaxKind.U16Keyword,
            ["u32"] = SyntaxKind.U32Keyword,
            ["u64"] = SyntaxKind.U64Keyword,
            ["usize"] = SyntaxKind.USizeKeyword,
            ["u"] = SyntaxKind.USizeKeyword,
            ["i8"] = SyntaxKind.I8Keyword,
            ["i16"] = SyntaxKind.I16Keyword,
            ["i32"] = SyntaxKind.I32Keyword,
            ["i64"] = SyntaxKind.I64Keyword,
            ["isize"] = SyntaxKind.ISizeKeyword,
            ["i"] = SyntaxKind.ISizeKeyword,
        }.ToImmutableDictionary();

    private static readonly ImmutableDictionary<string, SyntaxKind> s_floatTypeSuffixes =
        new Dictionary<string, SyntaxKind>
        {
            ["f32"] = SyntaxKind.F32Keyword,
            ["f64"] = SyntaxKind.F64Keyword,
        }.ToImmutableDictionary();

    /// <summary>
    /// Gets the keyword kind for the given text, or None if not a keyword.
    /// </summary>
    public static SyntaxKind GetKeywordKind(string text)
    {
        return s_keywords.TryGetValue(text, out var kind) ? kind : SyntaxKind.None;
    }

    /// <summary>
    /// Gets the integer type suffix kind for the given text, or None if not a valid suffix.
    /// </summary>
    public static SyntaxKind GetIntegerTypeSuffix(string text)
    {
        return s_integerTypeSuffixes.TryGetValue(text, out var kind) ? kind : SyntaxKind.None;
    }

    /// <summary>
    /// Gets the float type suffix kind for the given text, or None if not a valid suffix.
    /// </summary>
    public static SyntaxKind GetFloatTypeSuffix(string text)
    {
        return s_floatTypeSuffixes.TryGetValue(text, out var kind) ? kind : SyntaxKind.None;
    }

    /// <summary>
    /// Determines if the given kind is a keyword.
    /// </summary>
    public static bool IsKeyword(SyntaxKind kind)
    {
        return kind >= SyntaxKind.NamespaceKeyword && kind <= SyntaxKind.BoolKeyword;
    }

    /// <summary>
    /// Determines if the given kind is a primitive type keyword.
    /// </summary>
    public static bool IsPrimitiveTypeKeyword(SyntaxKind kind)
    {
        return kind >= SyntaxKind.U8Keyword && kind <= SyntaxKind.BoolKeyword;
    }

    /// <summary>
    /// Determines if the given kind is a declaration keyword.
    /// </summary>
    public static bool IsDeclarationKeyword(SyntaxKind kind)
    {
        return kind is SyntaxKind.FuncKeyword
            or SyntaxKind.StructKeyword
            or SyntaxKind.EnumKeyword
            or SyntaxKind.UnionKeyword
            or SyntaxKind.VariantKeyword
            or SyntaxKind.InterfaceKeyword
            or SyntaxKind.ConstKeyword
            or SyntaxKind.StaticKeyword
            or SyntaxKind.VarKeyword;
    }

    /// <summary>
    /// Determines if the given kind is a statement keyword.
    /// </summary>
    public static bool IsStatementKeyword(SyntaxKind kind)
    {
        return kind is SyntaxKind.IfKeyword
            or SyntaxKind.ForKeyword
            or SyntaxKind.SwitchKeyword
            or SyntaxKind.BreakKeyword
            or SyntaxKind.ContinueKeyword
            or SyntaxKind.ReturnKeyword
            or SyntaxKind.DeferKeyword;
    }

    /// <summary>
    /// Gets the text representation of a keyword.
    /// </summary>
    public static string? GetKeywordText(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.NamespaceKeyword => "namespace",
            SyntaxKind.UsingKeyword => "using",
            SyntaxKind.FuncKeyword => "func",
            SyntaxKind.InterfaceKeyword => "interface",
            SyntaxKind.StructKeyword => "struct",
            SyntaxKind.EnumKeyword => "enum",
            SyntaxKind.UnionKeyword => "union",
            SyntaxKind.VariantKeyword => "variant",
            SyntaxKind.VarKeyword => "var",
            SyntaxKind.ConstKeyword => "const",
            SyntaxKind.StaticKeyword => "static",
            SyntaxKind.IfKeyword => "if",
            SyntaxKind.ElseKeyword => "else",
            SyntaxKind.ForKeyword => "for",
            SyntaxKind.SwitchKeyword => "switch",
            SyntaxKind.BreakKeyword => "break",
            SyntaxKind.ContinueKeyword => "continue",
            SyntaxKind.ReturnKeyword => "return",
            SyntaxKind.DeferKeyword => "defer",
            SyntaxKind.WhereKeyword => "where",
            SyntaxKind.NullKeyword => "null",
            SyntaxKind.TrueKeyword => "true",
            SyntaxKind.FalseKeyword => "false",
            SyntaxKind.CDeclKeyword => "cdecl",
            SyntaxKind.StdCallKeyword => "stdcall",
            SyntaxKind.U8Keyword => "u8",
            SyntaxKind.U16Keyword => "u16",
            SyntaxKind.U32Keyword => "u32",
            SyntaxKind.U64Keyword => "u64",
            SyntaxKind.USizeKeyword => "usize",
            SyntaxKind.I8Keyword => "i8",
            SyntaxKind.I16Keyword => "i16",
            SyntaxKind.I32Keyword => "i32",
            SyntaxKind.I64Keyword => "i64",
            SyntaxKind.ISizeKeyword => "isize",
            SyntaxKind.F32Keyword => "f32",
            SyntaxKind.F64Keyword => "f64",
            SyntaxKind.BoolKeyword => "bool",
            _ => null,
        };
    }

    /// <summary>
    /// Gets the text representation of a punctuation or operator token.
    /// </summary>
    public static string? GetTokenText(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.OpenBraceToken => "{",
            SyntaxKind.CloseBraceToken => "}",
            SyntaxKind.OpenBracketToken => "[",
            SyntaxKind.CloseBracketToken => "]",
            SyntaxKind.OpenParenToken => "(",
            SyntaxKind.CloseParenToken => ")",
            SyntaxKind.DotToken => ".",
            SyntaxKind.CommaToken => ",",
            SyntaxKind.ColonToken => ":",
            SyntaxKind.SemicolonToken => ";",
            SyntaxKind.QuestionToken => "?",
            SyntaxKind.AtToken => "@",
            SyntaxKind.ColonColonToken => "::",
            SyntaxKind.EqualsArrowToken => "=>",
            SyntaxKind.EqualsToken => "=",
            SyntaxKind.PlusEqualsToken => "+=",
            SyntaxKind.MinusEqualsToken => "-=",
            SyntaxKind.StarEqualsToken => "*=",
            SyntaxKind.SlashEqualsToken => "/=",
            SyntaxKind.PercentEqualsToken => "%=",
            SyntaxKind.AmpersandEqualsToken => "&=",
            SyntaxKind.PipeEqualsToken => "|=",
            SyntaxKind.CaretEqualsToken => "^=",
            SyntaxKind.LessLessEqualsToken => "<<=",
            SyntaxKind.GreaterGreaterEqualsToken => ">>=",
            SyntaxKind.EqualsEqualsToken => "==",
            SyntaxKind.BangEqualsToken => "!=",
            SyntaxKind.LessToken => "<",
            SyntaxKind.GreaterToken => ">",
            SyntaxKind.LessEqualsToken => "<=",
            SyntaxKind.GreaterEqualsToken => ">=",
            SyntaxKind.AmpersandAmpersandToken => "&&",
            SyntaxKind.PipePipeToken => "||",
            SyntaxKind.AmpersandToken => "&",
            SyntaxKind.PipeToken => "|",
            SyntaxKind.CaretToken => "^",
            SyntaxKind.LessLessToken => "<<",
            SyntaxKind.GreaterGreaterToken => ">>",
            SyntaxKind.PlusToken => "+",
            SyntaxKind.MinusToken => "-",
            SyntaxKind.StarToken => "*",
            SyntaxKind.SlashToken => "/",
            SyntaxKind.PercentToken => "%",
            SyntaxKind.BangToken => "!",
            SyntaxKind.TildeToken => "~",
            _ => null,
        };
    }
}
