using Antlr4.Runtime.Tree;
using SolidLang.Compilers.Symbols;
using SolidLang.Compilers.Types;

namespace SolidLang.Compilers.Semantic;

public sealed class SemanticAnalyzer : SolidLangParserBaseListener
{
    private SymbolTable _currentScope = new();
    private readonly List<FunctionSymbol> _functions = new();
    private readonly Dictionary<string, SolidType> _types = new();

    public IReadOnlyList<FunctionSymbol> Functions => _functions;

    public override void EnterStruct_decl_stmt(SolidLangParser.Struct_decl_stmtContext context)
    {
        var name = context.ID().GetText();
        var fields = new List<(string Name, SolidType Type)>();

        var structFields = context.struct_fields();
        if (structFields != null)
        {
            foreach (var field in structFields.struct_field())
            {
                var fieldName = field.ID().GetText();
                var fieldType = ResolveType(field.type());
                fields.Add((fieldName, fieldType));
            }
        }

        var structType = new StructType(name, fields);
        _types[name] = structType;
        _currentScope.Define(new StructSymbol(name, fields));
    }

    public override void EnterUnion_decl_stmt(SolidLangParser.Union_decl_stmtContext context)
    {
        var name = context.ID().GetText();
        var fields = new List<(string Name, SolidType Type)>();

        var unionFields = context.union_fields();
        if (unionFields != null)
        {
            foreach (var field in unionFields.union_field())
            {
                var fieldName = field.ID().GetText();
                var fieldType = ResolveType(field.type());
                fields.Add((fieldName, fieldType));
            }
        }

        var unionType = new UnionType(name, fields);
        _types[name] = unionType;
        _currentScope.Define(new UnionSymbol(name, fields));
    }

    public override void EnterEnum_decl_stmt(SolidLangParser.Enum_decl_stmtContext context)
    {
        var name = context.ID().GetText();
        var underlyingType = context.type() != null
            ? ResolveType(context.type())
            : new I32Type();

        var fields = new List<(string Name, long Value)>();
        long nextValue = 0;

        var enumFields = context.enum_fields();
        if (enumFields != null)
        {
            foreach (var field in enumFields.enum_field())
            {
                var fieldName = field.ID().GetText();

                if (field.expr() != null)
                {
                    var valueExpr = field.expr().GetText();
                    // Handle negative numbers
                    if (valueExpr.StartsWith("-"))
                    {
                        nextValue = -long.Parse(valueExpr.Substring(1));
                    }
                    else
                    {
                        nextValue = long.Parse(valueExpr);
                    }
                }

                fields.Add((fieldName, nextValue));
                nextValue++;
            }
        }

        var enumType = new EnumType(name, underlyingType, fields);
        _types[name] = enumType;
        _currentScope.Define(new EnumSymbol(name, underlyingType, fields));
    }

    public override void EnterFunc_decl_stmt(SolidLangParser.Func_decl_stmtContext context)
    {
        var header = context.func_decl_header();
        var funcName = header.ID().GetText();
        var returnType = ResolveType(header.type());

        var parameters = new List<VariableSymbol>();
        var funcParams = header.func_parameters();
        if (funcParams != null)
        {
            foreach (var param in funcParams.func_parameter())
            {
                var paramName = param.ID().GetText();
                var paramType = ResolveType(param.type());
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
        var varType = context.type() != null
            ? ResolveType(context.type())
            : new I32Type(); // Default type inference placeholder
        _currentScope.Define(new VariableSymbol(varName, varType));
    }

    private SolidType ResolveType(SolidLangParser.TypeContext typeContext)
    {
        // Handle tuple type: (type1, type2, ...)
        if (typeContext.tuple_type() is { } tupleType)
        {
            var elements = new List<SolidType>();
            var types = tupleType.type();
            if (types != null)
            {
                foreach (var t in types)
                {
                    elements.Add(ResolveType(t));
                }
            }
            return new TupleType(elements);
        }

        // Handle named type (including user-defined types)
        if (typeContext.named_type() is { } namedType)
        {
            var typeName = namedType.ID().GetText();

            if (_types.TryGetValue(typeName, out var resolvedType))
            {
                return resolvedType;
            }

            return typeName switch
            {
                "i32" => new I32Type(),
                "void" => new VoidType(),
                "bool" => new BoolType(),
                _ => throw new NotSupportedException($"Unknown type: {typeName}")
            };
        }

        // Handle other types
        var text = typeContext.GetText();
        return ResolveType(text);
    }

    private SolidType ResolveType(string typeName)
    {
        if (_types.TryGetValue(typeName, out var resolvedType))
        {
            return resolvedType;
        }

        return typeName switch
        {
            "i32" => new I32Type(),
            "void" => new VoidType(),
            "bool" => new BoolType(),
            _ => throw new NotSupportedException($"Unknown type: {typeName}")
        };
    }
}
