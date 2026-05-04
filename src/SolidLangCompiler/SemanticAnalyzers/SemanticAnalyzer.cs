using Antlr4.Runtime;
using SolidLangCompiler.AST;
using SolidLangCompiler.Generated;
using SolidLangCompiler.SemanticAnalyzers.Builders;

namespace SolidLangCompiler.SemanticAnalyzers;

/// <summary>
/// Performs semantic analysis on source code.
/// </summary>
public sealed class SemanticAnalyzer : IDisposable
{
    private readonly AstBuilder _astBuilder = new();

    /// <summary>
    /// Analyzes source code and produces a semantic tree.
    /// </summary>
    public SemanticTree Analyze(string source, string filePath)
    {
        // Lexical analysis
        var inputStream = new AntlrInputStream(source);
        var lexer = new SolidLangLexer(inputStream);

        // Syntax analysis
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new SolidLangParser(tokenStream);

        // Parse
        var parseTree = parser.program();

        // Check for syntax errors
        if (parser.NumberOfSyntaxErrors > 0)
        {
            var tree = new SemanticTree();
            tree.Errors.Add(new SemanticError($"Found {parser.NumberOfSyntaxErrors} syntax errors", SourceLocation.Unknown));
            return tree;
        }

        // Build AST
        var program = _astBuilder.Build(parseTree, filePath);

        // Create semantic tree
        var semanticTree = new SemanticTree(program);

        // TODO: Perform semantic analysis
        // - Build symbol table
        // - Resolve names
        // - Type checking
        // - Other semantic checks

        return semanticTree;
    }

    public void Dispose()
    {
    }
}
