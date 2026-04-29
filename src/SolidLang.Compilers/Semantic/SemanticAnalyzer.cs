using Antlr4.Runtime.Tree;
using SolidLang.Compilers.Symbols;
using SolidLang.Compilers.Types;

namespace SolidLang.Compilers.Semantic;

public sealed class SemanticAnalyzer : SolidLangParserBaseListener
{
    private SymbolTable _currentScope = new();
    private readonly Stack<SymbolTable> _scopeStack = new();
    private readonly List<FunctionSymbol> _functions = new();
    private readonly Dictionary<string, SolidType> _types = new();
    private readonly List<ConstSymbol> _consts = new();
    private readonly List<StaticSymbol> _statics = new();
    private readonly List<ConstStaticSymbol> _constStatics = new();

    public IReadOnlyList<FunctionSymbol> Functions => _functions;
    public IReadOnlyList<ConstSymbol> Consts => _consts;
    public IReadOnlyList<StaticSymbol> Statics => _statics;
    public IReadOnlyList<ConstStaticSymbol> ConstStatics => _constStatics;

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

        _scopeStack.Push(_currentScope);
        _currentScope = _currentScope.EnterScope();
        foreach (var param in parameters)
        {
            _currentScope.Define(param);
        }
    }

    public override void ExitFunc_decl_stmt(SolidLangParser.Func_decl_stmtContext context)
    {
        if (_scopeStack.Count > 0)
        {
            _currentScope = _scopeStack.Pop();
        }
    }

    public override void EnterVar_decl_stmt(SolidLangParser.Var_decl_stmtContext context)
    {
        var varName = context.ID().GetText();
        var varType = context.type() != null
            ? ResolveType(context.type())
            : new I32Type(); // Default type inference placeholder
        _currentScope.Define(new VariableSymbol(varName, varType));
    }

    // Handle top-level const declarations
    public override void EnterConst_decl_stmt(SolidLangParser.Const_decl_stmtContext context)
    {
        var constName = context.ID().GetText();
        var constType = context.type() != null
            ? ResolveType(context.type())
            : new I32Type();
        var valueExpr = context.expr().GetText();

        var constSymbol = new ConstSymbol(constName, constType, valueExpr);
        _consts.Add(constSymbol);
        _currentScope.Define(constSymbol);
    }

    // Handle top-level const static declarations
    public override void EnterConst_static_decl_stmt(SolidLangParser.Const_static_decl_stmtContext context)
    {
        var constName = context.ID().GetText();
        var constType = context.type() != null
            ? ResolveType(context.type())
            : new I32Type();
        var valueExpr = context.expr().GetText();

        var constStaticSymbol = new ConstStaticSymbol(constName, constType, valueExpr);
        _constStatics.Add(constStaticSymbol);
        _currentScope.Define(constStaticSymbol);
    }

    // Handle static variable declarations (both top-level and inside functions)
    public override void EnterStatic_decl_stmt(SolidLangParser.Static_decl_stmtContext context)
    {
        var staticName = context.ID().GetText();
        var staticType = context.type() != null
            ? ResolveType(context.type())
            : new I32Type();

        var staticSymbol = new StaticSymbol(staticName, staticType);
        _statics.Add(staticSymbol);
        _currentScope.Define(staticSymbol);
    }

    private SolidType ResolveType(SolidLangParser.TypeContext typeContext)
    {
        // Handle pointer type: *T or !*T
        if (typeContext.pointer_type() is { } pointerType)
        {
            var isMutable = pointerType.NOT() == null;
            var elementType = ResolveType(pointerType.type());
            return new PointerType(elementType, isMutable);
        }

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
                // Signed integers
                "i8" => new I8Type(),
                "i16" => new I16Type(),
                "i32" => new I32Type(),
                "i64" => new I64Type(),
                "i128" => new I128Type(),
                "isize" => new IsizeType(),
                // Unsigned integers
                "u8" => new U8Type(),
                "u16" => new U16Type(),
                "u32" => new U32Type(),
                "u64" => new U64Type(),
                "u128" => new U128Type(),
                "usize" => new UsizeType(),
                // Floating-point
                "f16" => new F16Type(),
                "f32" => new F32Type(),
                "f64" => new F64Type(),
                "f128" => new F128Type(),
                // Other
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
