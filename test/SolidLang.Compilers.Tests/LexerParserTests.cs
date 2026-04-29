using Antlr4.Runtime;
using SolidLang.Compilers;
using Xunit;

namespace SolidLang.Compilers.Tests;

public class LexerTests
{
    private static List<IToken> GetVisibleTokens(SolidLangLexer lexer)
    {
        var tokenStream = new CommonTokenStream(lexer);
        tokenStream.Fill();
        // GetTokens() 返回所有通道的 token，需要过滤掉 HIDDEN 通道和 EOF
        return tokenStream.GetTokens()
            .Where(t => t.Channel == TokenConstants.DefaultChannel && t.Type != -1)
            .ToList();
    }

    [Fact]
    public void Lexer_ShouldTokenize_NamespaceDeclaration()
    {
        var input = "namespace core::math;";
        var inputStream = new AntlrInputStream(input);
        var lexer = new SolidLangLexer(inputStream);
        var tokens = GetVisibleTokens(lexer);

        Assert.True(tokens.Count >= 4);
        Assert.Equal(SolidLangLexer.NAMESPACE, tokens[0].Type);
        Assert.Equal(SolidLangLexer.ID, tokens[1].Type);
        Assert.Equal(SolidLangLexer.SCOPE, tokens[2].Type);
        Assert.Equal(SolidLangLexer.ID, tokens[3].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenize_IntegerLiteral()
    {
        var input = "42";
        var inputStream = new AntlrInputStream(input);
        var lexer = new SolidLangLexer(inputStream);
        var tokens = GetVisibleTokens(lexer);

        Assert.Single(tokens);
        Assert.Equal(SolidLangLexer.INTEGER_LITERAL, tokens[0].Type);
        Assert.Equal("42", tokens[0].Text);
    }

    [Fact]
    public void Lexer_ShouldTokenize_StringLiteral()
    {
        var input = "\"hello world\"";
        var inputStream = new AntlrInputStream(input);
        var lexer = new SolidLangLexer(inputStream);
        var tokens = GetVisibleTokens(lexer);

        Assert.Single(tokens);
        Assert.Equal(SolidLangLexer.STRING_LITERAL, tokens[0].Type);
        Assert.Equal("\"hello world\"", tokens[0].Text);
    }
}
