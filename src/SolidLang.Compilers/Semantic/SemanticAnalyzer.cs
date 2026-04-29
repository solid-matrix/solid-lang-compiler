using Antlr4.Runtime.Tree;
using SolidLang.Compilers.Symbols;
using SolidLang.Compilers.Types;

namespace SolidLang.Compilers.Semantic;

public sealed class SemanticAnalyzer : SolidLangParserBaseListener
{
    private SymbolTable _currentScope = new();
    private readonly List<FunctionSymbol> _functions = new();

    public IReadOnlyList<FunctionSymbol> Functions => _functions;

    public override void EnterFunc_decl_stmt(SolidLangParser.Func_decl_stmtContext context)
    {
        var header = context.func_decl_header();
        var funcName = header.ID().GetText();
        var returnType = ResolveType(header.type().GetText());

        var parameters = new List<VariableSymbol>();
        var funcParams = header.func_parameters();
        if (funcParams != null)
        {
            foreach (var param in funcParams.func_parameter())
            {
                var paramName = param.ID().GetText();
                var paramType = ResolveType(param.type().GetText());
                parameters.Add(new VariableSymbol(paramName, paramType));
            }
        }

        var funcSymbol = new FunctionSymbol(funcName, returnType, parameters);
        _functions.Add(funcSymbol);
        _currentScope.Define(funcSymbol);

        _currentScope = _currentScope.EnterScope();
        foreach (var param in parameters)
        {
            _currentScope.Define(param);
        }
    }

    public override void ExitFunc_decl_stmt(SolidLangParser.Func_decl_stmtContext context)
    {
        _currentScope = new SymbolTable();
    }

    public override void EnterVar_decl_stmt(SolidLangParser.Var_decl_stmtContext context)
    {
        var varName = context.ID().GetText();
        var varType = ResolveType(context.type().GetText());
        _currentScope.Define(new VariableSymbol(varName, varType));
    }

    private static SolidType ResolveType(string typeName)
    {
        return typeName switch
        {
            "i32" => new I32Type(),
            "void" => new VoidType(),
            "bool" => new BoolType(),
            _ => throw new NotSupportedException($"Unknown type: {typeName}")
        };
    }
}
