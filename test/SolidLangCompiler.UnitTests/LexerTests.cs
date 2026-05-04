using Antlr4.Runtime;
using FluentAssertions;
using SolidLangCompiler.Generated;
using Xunit;

namespace SolidLangCompiler.UnitTests;

public class LexerTests
{
    private SolidLangLexer CreateLexer(string input)
    {
        var inputStream = new AntlrInputStream(input);
        return new SolidLangLexer(inputStream);
    }

    private IList<IToken> GetTokens(string input)
    {
        var lexer = CreateLexer(input);
        return lexer.GetAllTokens();
    }

    [Fact]
    public void Lexer_ShouldTokenizeKeywords()
    {
        var input = "namespace func var const return if else for switch break continue";
        var tokens = GetTokens(input);
        var types = tokens.Select(t => t.Type).ToList();

        types.Should().Contain(SolidLangLexer.NAMESPACE);
        types.Should().Contain(SolidLangLexer.FUNC);
        types.Should().Contain(SolidLangLexer.VAR);
        types.Should().Contain(SolidLangLexer.CONST);
        types.Should().Contain(SolidLangLexer.RETURN);
        types.Should().Contain(SolidLangLexer.IF);
        types.Should().Contain(SolidLangLexer.ELSE);
        types.Should().Contain(SolidLangLexer.FOR);
        types.Should().Contain(SolidLangLexer.SWITCH);
        types.Should().Contain(SolidLangLexer.BREAK);
        types.Should().Contain(SolidLangLexer.CONTINUE);
    }

    [Fact]
    public void Lexer_ShouldTokenizePrimitiveTypes()
    {
        var input = "i8 i16 i32 i64 isize u8 u16 u32 u64 usize f32 f64 bool";
        var tokens = GetTokens(input);
        var types = tokens.Select(t => t.Type).ToList();

        types.Should().Contain(SolidLangLexer.I8);
        types.Should().Contain(SolidLangLexer.I16);
        types.Should().Contain(SolidLangLexer.I32);
        types.Should().Contain(SolidLangLexer.I64);
        types.Should().Contain(SolidLangLexer.ISIZE);
        types.Should().Contain(SolidLangLexer.U8);
        types.Should().Contain(SolidLangLexer.U16);
        types.Should().Contain(SolidLangLexer.U32);
        types.Should().Contain(SolidLangLexer.U64);
        types.Should().Contain(SolidLangLexer.USIZE);
        types.Should().Contain(SolidLangLexer.F32);
        types.Should().Contain(SolidLangLexer.F64);
        types.Should().Contain(SolidLangLexer.BOOL);
    }

    [Fact]
    public void Lexer_ShouldTokenizeIntegerLiterals()
    {
        var input = "42 0xFF 0o77 0b1010";
        var tokens = GetTokens(input);

        var intTokens = tokens.Where(t => t.Type == SolidLangLexer.INTEGER_LITERAL).ToList();
        intTokens.Should().HaveCount(4);
    }

    [Fact]
    public void Lexer_ShouldTokenizeFloatLiterals()
    {
        var input = "3.14 2.5e10";
        var tokens = GetTokens(input);

        var floatTokens = tokens.Where(t => t.Type == SolidLangLexer.FLOAT_LITERAL).ToList();
        floatTokens.Should().HaveCount(2);
    }

    [Fact]
    public void Lexer_ShouldTokenizeStringLiteral()
    {
        var input = "\"hello world\"";
        var tokens = GetTokens(input);

        tokens[0].Type.Should().Be(SolidLangLexer.STRING_LITERAL);
        tokens[0].Text.Should().Be("\"hello world\"");
    }

    [Fact]
    public void Lexer_ShouldTokenizeCharLiteral()
    {
        var input = "'a'";
        var tokens = GetTokens(input);

        tokens[0].Type.Should().Be(SolidLangLexer.CHAR_LITERAL);
    }

    [Fact]
    public void Lexer_ShouldTokenizeOperators()
    {
        var input = "+ - * / % == != < > <= >= || && ! & | ^ << >>";
        var tokens = GetTokens(input);
        var types = tokens.Select(t => t.Type).ToList();

        types.Should().Contain(SolidLangLexer.PLUS);
        types.Should().Contain(SolidLangLexer.MINUS);
        types.Should().Contain(SolidLangLexer.STAR);
        types.Should().Contain(SolidLangLexer.SLASH);
        types.Should().Contain(SolidLangLexer.MOD);
        types.Should().Contain(SolidLangLexer.EQEQ);
        types.Should().Contain(SolidLangLexer.NOTEQ);
        types.Should().Contain(SolidLangLexer.LT);
        types.Should().Contain(SolidLangLexer.GT);
        types.Should().Contain(SolidLangLexer.LE);
        types.Should().Contain(SolidLangLexer.GE);
        types.Should().Contain(SolidLangLexer.OROR);
        types.Should().Contain(SolidLangLexer.ANDAND);
        types.Should().Contain(SolidLangLexer.NOT);
        types.Should().Contain(SolidLangLexer.AND);
        types.Should().Contain(SolidLangLexer.OR);
        types.Should().Contain(SolidLangLexer.CARET);
        types.Should().Contain(SolidLangLexer.SHL);
        types.Should().Contain(SolidLangLexer.SHR);
    }

    [Fact]
    public void Lexer_ShouldTokenizeAssignmentOperators()
    {
        var input = "= += -= *= /= %= &= |= ^= <<= >>=";
        var tokens = GetTokens(input);
        var types = tokens.Select(t => t.Type).ToList();

        types.Should().Contain(SolidLangLexer.EQ);
        types.Should().Contain(SolidLangLexer.PLUSEQ);
        types.Should().Contain(SolidLangLexer.MINUSEQ);
        types.Should().Contain(SolidLangLexer.STAREQ);
        types.Should().Contain(SolidLangLexer.SLASHEQ);
        types.Should().Contain(SolidLangLexer.PERCENTEQ);
        types.Should().Contain(SolidLangLexer.ANDEQ);
        types.Should().Contain(SolidLangLexer.OREQ);
        types.Should().Contain(SolidLangLexer.CARETEQ);
        types.Should().Contain(SolidLangLexer.SHLEQ);
        types.Should().Contain(SolidLangLexer.SHREQ);
    }

    [Fact]
    public void Lexer_ShouldTokenizeDelimiters()
    {
        var input = "( ) [ ] { } , ; : . -> =>";
        var tokens = GetTokens(input);
        var types = tokens.Select(t => t.Type).ToList();

        types.Should().Contain(SolidLangLexer.LPAREN);
        types.Should().Contain(SolidLangLexer.RPAREN);
        types.Should().Contain(SolidLangLexer.LBRACKET);
        types.Should().Contain(SolidLangLexer.RBRACKET);
        types.Should().Contain(SolidLangLexer.LBRACE);
        types.Should().Contain(SolidLangLexer.RBRACE);
        types.Should().Contain(SolidLangLexer.COMMA);
        types.Should().Contain(SolidLangLexer.SEMI);
        types.Should().Contain(SolidLangLexer.COLON);
        types.Should().Contain(SolidLangLexer.DOT);
        types.Should().Contain(SolidLangLexer.MINUSRARROW);
        types.Should().Contain(SolidLangLexer.EQARROW);
    }

    [Fact]
    public void Lexer_ShouldHandleIdentifiers()
    {
        var input = "foo bar123 _baz _qux123";
        var tokens = GetTokens(input);

        var idTokens = tokens.Where(t => t.Type == SolidLangLexer.ID).ToList();
        idTokens.Should().HaveCount(4);
    }
}
