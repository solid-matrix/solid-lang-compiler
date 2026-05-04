using Antlr4.Runtime;
using SolidLangCompiler.AST;
using SolidLangCompiler.Generated;
using SolidLangCompiler.SemanticTree;
using SolidLangCompiler.SemanticAnalyzers.Builders;
using SemaSourceLocation = SolidLangCompiler.SemanticTree.SourceLocation;

namespace SolidLangCompiler.SemanticAnalyzers;

/// <summary>
/// Performs semantic analysis on source code.
///
/// Compilation pipeline:
/// 1. Lexical analysis (ANTLR lexer)
/// 2. Syntax analysis (ANTLR parser)
/// 3. AST building (AstBuilder)
/// 4. Semantic analysis (this class)
///   5. SemanticTree generation (SemaBuilder)
/// 6. Code generation (CodeGenerator)
/// </summary>
public sealed class SemanticAnalyzer : IDisposable
{
    private readonly AstBuilder _astBuilder = new();
    private readonly SemaBuilder _semaBuilder = new();

    /// <summary>
    /// Analyzes source code and produces a semantic tree.
    /// </summary>
    public SemaProgram Analyze(string source, string filePath)
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
            var errorProgram = new SemaProgram();
            errorProgram.Errors.Add(new SemanticError($"Found {parser.NumberOfSyntaxErrors} syntax errors",
                new SemaSourceLocation(filePath, 0, 0)));
            return errorProgram;
        }

        // Build AST
        var ast = _astBuilder.Build(parseTree, filePath);

        // Build SemanticTree (includes type resolution, generic specialization)
        var semaProgram = _semaBuilder.Build(ast);

        return semaProgram;
    }

    public void Dispose()
    {
    }
}
