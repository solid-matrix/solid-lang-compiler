using SolidLangCompiler.AST;
using SolidLangCompiler.AST.Declarations;
using SolidLangCompiler.AST.Expressions;
using SolidLangCompiler.AST.Statements;
using SolidLangCompiler.AST.Types;

namespace SolidLangCompiler.SemanticTree;

/// <summary>
/// Builds SemanticTree from AST.
/// Performs type resolution, generic specialization, and lowering.
/// </summary>
public class SemaBuilder
{
    private readonly List<SemanticError> _errors = new();
    private readonly List<SemanticError> _warnings = new();
    private readonly List<SemaFunction> _functions = new();
    private readonly List<SemaGlobal> _globals = new();
    private readonly List<SemaStructType> _structs = new();
    private readonly List<SemaEnumType> _enums = new();
    private readonly Dictionary<string, int> _localIndices = new();
    private readonly Dictionary<string, SemaType> _localTypes = new();
    private int _localCount;

    /// <summary>
    /// Builds a SemanticTree from an AST ProgramNode.
    /// </summary>
    public SemaProgram Build(ProgramNode ast)
    {
        _errors.Clear();
        _warnings.Clear();
        _functions.Clear();
        _globals.Clear();
        _structs.Clear();
        _enums.Clear();

        // Collect all declarations first
        CollectDeclarations(ast);

        // Process each declaration
        foreach (var decl in ast.Declarations ?? [])
        {
            ProcessDeclaration(decl);
        }

        return new SemaProgram
        {
            Namespace = ast.Namespace?.Path,
            Usings = ast.Usings?.Select(u => u.Path).ToList(),
            Functions = _functions,
            Globals = _globals,
            Structs = _structs,
            Enums = _enums,
            Errors = _errors,
            Warnings = _warnings
        };
    }

    private void CollectDeclarations(ProgramNode ast)
    {
        foreach (var decl in ast.Declarations ?? [])
        {
            switch (decl)
            {
                case StructDeclarationNode structDecl:
                    _structs.Add(BuildStructType(structDecl));
                    break;
                case EnumDeclarationNode enumDecl:
                    _enums.Add(BuildEnumType(enumDecl));
                    break;
            }
        }
    }

    private void ProcessDeclaration(DeclarationNode decl)
    {
        switch (decl)
        {
            case FuncDeclarationNode funcDecl:
                var func = BuildFunction(funcDecl);
                if (func != null)
                    _functions.Add(func);
                break;
            case ConstDeclarationNode constDecl:
                _globals.Add(BuildGlobal(constDecl));
                break;
            case StaticDeclarationNode staticDecl:
                _globals.Add(BuildGlobal(staticDecl));
                break;
        }
    }

    private SemaFunction? BuildFunction(FuncDeclarationNode node)
    {
        if (node.Body == null)
        {
            // External function declaration - skip for now
            return null;
        }

        _localIndices.Clear();
        _localTypes.Clear();
        _localCount = 0;

        var parameters = new List<SemaParameter>();
        var paramIndex = 0;
        foreach (var param in node.Parameters ?? [])
        {
            var paramType = ResolveType(param.Type);
            if (paramType == null)
            {
                _errors.Add(new SemanticError($"Unknown parameter type: {param.Type}",
                    ConvLoc(node.Location)));
                return null;
            }

            var semaParam = new SemaParameter
            {
                Name = param.Name,
                Index = paramIndex,
                Type = paramType,
                Location = ConvLoc(param.Location)
            };
            parameters.Add(semaParam);
            _localIndices[param.Name] = paramIndex;
            _localTypes[param.Name] = paramType;
            paramIndex++;
        }

        var returnType = node.ReturnType != null ? ResolveType(node.ReturnType) : null;

        var body = BuildBlock(node.Body);
        if (body == null)
            return null;

        var mangledName = GetMangledName(node);
        var isEntryPoint = node.Name == "main";

        return new SemaFunction
        {
            Name = node.Name,
            MangledName = mangledName,
            Parameters = parameters,
            ReturnType = returnType,
            Body = body,
            IsEntryPoint = isEntryPoint,
            LocalCount = _localCount,
            Location = ConvLoc(node.Location)
        };
    }

    private static SourceLocation ConvLoc(AST.SourceLocation loc) =>
        new(loc.FilePath, loc.Line, loc.Column);

    private SemaBlock? BuildBlock(BlockStatementNode node)
    {
        var statements = new List<SemaStatement>();

        foreach (var stmt in node.Statements)
        {
            var semaStmt = BuildStatement(stmt);
            if (semaStmt != null)
                statements.Add(semaStmt);
        }

        return new SemaBlock { Statements = statements, Location = ConvLoc(node.Location) };
    }

    private SemaStatement? BuildStatement(StatementNode node)
    {
        return node switch
        {
            ReturnStatementNode ret => BuildReturn(ret),
            AssignmentStatementNode assign => BuildAssignment(assign),
            ExpressionStatementNode exprStmt => BuildExprStmt(exprStmt),
            IfStatementNode ifStmt => BuildIf(ifStmt),
            BlockStatementNode block => BuildBlock(block),
            EmptyStatementNode => new SemaEmpty { Location = ConvLoc(node.Location) },
            ForStatementNode forStmt => BuildFor(forStmt),
            BreakStatementNode => new SemaBreak { Location = ConvLoc(node.Location) },
            ContinueStatementNode => new SemaContinue { Location = ConvLoc(node.Location) },
            VarDeclStatementNode varDecl => BuildLocalDeclFromStmt(varDecl),
            SwitchStatementNode switchStmt => BuildSwitch(switchStmt),
            _ => null
        };
    }

    private SemaSwitch? BuildSwitch(SwitchStatementNode node)
    {
        var expr = BuildExpression(node.Expression);
        if (expr == null)
            return null;

        var arms = new List<SemaSwitchArm>();
        foreach (var arm in node.Arms)
        {
            var semaArm = BuildSwitchArm(arm);
            if (semaArm != null)
                arms.Add(semaArm);
        }

        return new SemaSwitch
        {
            Expression = expr,
            Arms = arms,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaSwitchArm? BuildSwitchArm(SwitchArmNode node)
    {
        SemaPattern? pattern = null;
        if (node.Pattern != null)
        {
            pattern = BuildPattern(node.Pattern);
        }

        var stmt = BuildStatement(node.Statement);
        if (stmt == null)
            return null;

        return new SemaSwitchArm
        {
            Pattern = pattern,
            Statement = stmt,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaPattern? BuildPattern(PatternNode node)
    {
        return node switch
        {
            LiteralPatternNode litPattern => BuildLiteralPattern(litPattern),
            TypePatternNode typePattern => BuildTypePattern(typePattern),
            IdentifierPatternNode idPattern => BuildIdentifierPattern(idPattern),
            _ => null
        };
    }

    private SemaLiteralPattern? BuildLiteralPattern(LiteralPatternNode node)
    {
        var literal = BuildExpression(node.Literal);
        if (literal == null)
            return null;

        return new SemaLiteralPattern
        {
            Literal = literal,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaTypePattern BuildTypePattern(TypePatternNode node)
    {
        IReadOnlyList<SemaPattern>? bindings = null;
        if (node.Bindings != null)
        {
            var bindingList = new List<SemaPattern>();
            foreach (var binding in node.Bindings)
            {
                var semaBinding = BuildPattern(binding);
                if (semaBinding != null)
                    bindingList.Add(semaBinding);
            }
            bindings = bindingList;
        }

        return new SemaTypePattern
        {
            TypeName = node.Type.FullyQualifiedName,
            MemberName = node.MemberName,
            Bindings = bindings,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaIdentifierPattern BuildIdentifierPattern(IdentifierPatternNode node)
    {
        return new SemaIdentifierPattern
        {
            Name = node.Name,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaStatement? BuildFor(ForStatementNode node)
    {
        return node switch
        {
            InfiniteForNode infinite => BuildForInfinite(infinite),
            ConditionalForNode cond => BuildForConditional(cond),
            CStyleForNode cstyle => BuildForCStyle(cstyle),
            ForeachNode each => BuildForeach(each),
            _ => null
        };
    }

    private SemaForInfinite BuildForInfinite(InfiniteForNode node)
    {
        var body = BuildBlock(node.Body);
        return new SemaForInfinite
        {
            Body = body!,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaForConditional? BuildForConditional(ConditionalForNode node)
    {
        var condition = BuildExpression(node.Condition);
        var body = BuildBlock(node.Body);
        if (condition == null || body == null)
            return null;

        return new SemaForConditional
        {
            Condition = condition,
            Body = body,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaForCStyle? BuildForCStyle(CStyleForNode node)
    {
        SemaStatement? init = null;
        if (node.Init != null)
        {
            init = BuildStatement(node.Init);
        }

        var condition = node.Condition != null ? BuildExpression(node.Condition) : null;
        var update = node.Update != null ? BuildExpression(node.Update) : null;
        var body = BuildBlock(node.Body);

        if (body == null)
            return null;

        return new SemaForCStyle
        {
            Init = init,
            Condition = condition,
            Update = update,
            Body = body,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaForeach? BuildForeach(ForeachNode node)
    {
        var iterable = BuildExpression(node.Iterable);
        var body = BuildBlock(node.Body);
        if (iterable == null || body == null)
            return null;

        var index = _localCount++;
        _localIndices[node.VariableName] = index;
        _localTypes[node.VariableName] = new SemaIntType(SemaIntegerKind.I32); // Simplified

        return new SemaForeach
        {
            VariableName = node.VariableName,
            Iterable = iterable,
            Body = body,
            VariableIndex = index,
            VariableType = new SemaIntType(SemaIntegerKind.I32), // Simplified
            Location = ConvLoc(node.Location)
        };
    }

    private SemaReturn? BuildReturn(ReturnStatementNode node)
    {
        SemaExpression? value = null;
        if (node.Value != null)
        {
            value = BuildExpression(node.Value);
        }

        return new SemaReturn
        {
            Value = value,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaLocalDecl? BuildLocalDecl(VarDeclarationNode node)
    {
        var index = _localCount++;
        _localIndices[node.Name] = index;

        var type = node.Type != null ? ResolveType(node.Type) : null;
        if (type == null && node.Initializer != null)
        {
            // Infer type from initializer
            var initExpr = BuildExpression(node.Initializer);
            type = initExpr?.Type;
        }

        if (type == null)
        {
            _errors.Add(new SemanticError($"Cannot infer type for variable '{node.Name}'",
                ConvLoc(node.Location)));
            return null;
        }

        var initializer = node.Initializer != null ? BuildExpression(node.Initializer) : null;

        // Store the type for later reference
        _localTypes[node.Name] = type;

        return new SemaLocalDecl
        {
            Name = node.Name,
            Index = index,
            Type = type,
            Initializer = initializer,
            IsMutable = true,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaLocalDecl? BuildLocalDeclFromStmt(VarDeclStatementNode node)
    {
        var index = _localCount++;
        _localIndices[node.Name] = index;

        var type = node.Type != null ? ResolveType(node.Type) : null;
        if (type == null && node.Initializer != null)
        {
            var initExpr = BuildExpression(node.Initializer);
            type = initExpr?.Type;
        }

        if (type == null)
        {
            _errors.Add(new SemanticError($"Cannot infer type for variable '{node.Name}'",
                ConvLoc(node.Location)));
            return null;
        }

        var initializer = node.Initializer != null ? BuildExpression(node.Initializer) : null;

        // Store the type for later reference
        _localTypes[node.Name] = type;

        return new SemaLocalDecl
        {
            Name = node.Name,
            Index = index,
            Type = type,
            Initializer = initializer,
            IsMutable = true,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaAssignment? BuildAssignment(AssignmentStatementNode node)
    {
        if (node.Target == null)
        {
            _errors.Add(new SemanticError("Assignment target is null",
                ConvLoc(node.Location)));
            return null;
        }
        if (node.Value == null)
        {
            _errors.Add(new SemanticError("Assignment value is null",
                ConvLoc(node.Location)));
            return null;
        }

        var target = BuildExpression(node.Target);
        var value = BuildExpression(node.Value);

        if (target == null || value == null)
            return null;

        return new SemaAssignment
        {
            Target = target,
            Operator = ConvertAssignOp(node.Operator),
            Value = value,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaExprStmt? BuildExprStmt(ExpressionStatementNode node)
    {
        var expr = BuildExpression(node.Expression);
        if (expr == null)
            return null;

        return new SemaExprStmt { Expression = expr, Location = ConvLoc(node.Location) };
    }

    private SemaIf? BuildIf(IfStatementNode node)
    {
        var condition = BuildExpression(node.Condition);
        if (condition == null)
            return null;

        var thenBlock = BuildBlock(node.ThenBlock);
        if (thenBlock == null)
            return null;

        SemaStatement? elseBranch = null;
        if (node.ElseBranch != null)
        {
            elseBranch = BuildStatement(node.ElseBranch);
        }

        return new SemaIf
        {
            Condition = condition,
            ThenBlock = thenBlock,
            ElseBranch = elseBranch,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaExpression? BuildExpression(ExpressionNode node)
    {
        return node switch
        {
            IntegerLiteralNode intLit => BuildIntLiteral(intLit),
            FloatLiteralNode floatLit => BuildFloatLiteral(floatLit),
            BoolLiteralNode boolLit => BuildBoolLiteral(boolLit),
            StringLiteralNode strLit => BuildStringLiteral(strLit),
            NullLiteralNode => new SemaNullLiteral { Type = new SemaPointerType(new SemaVoidType()), Location = ConvLoc(node.Location) },
            BinaryExpressionNode binExpr => BuildBinary(binExpr),
            UnaryExpressionNode unExpr => BuildUnary(unExpr),
            CallExpressionNode callExpr => BuildCall(callExpr),
            IdentifierExpressionNode idExpr => BuildIdentifier(idExpr),
            ConditionalExpressionNode condExpr => BuildConditional(condExpr),
            ArrayLiteralNode arrLit => BuildArrayLiteral(arrLit),
            StructLiteralNode structLit => BuildStructLiteral(structLit),
            EnumLiteralNode enumLit => BuildEnumLiteral(enumLit),
            FieldAccessExpressionNode fieldAccess => BuildFieldAccess(fieldAccess),
            IndexExpressionNode indexExpr => BuildIndexExpr(indexExpr),
            _ => BuildExpressionFallback(node)
        };
    }

    private SemaExpression? BuildExpressionFallback(ExpressionNode node)
    {
        if (node == null)
        {
            _errors.Add(new SemanticError("Expression node is null",
                new SourceLocation("<unknown>", 0, 0)));
            return null;
        }
        _errors.Add(new SemanticError($"Unsupported expression type: {node.GetType().Name}",
            ConvLoc(node.Location)));
        return null;
    }

    private SemaIntLiteral BuildIntLiteral(IntegerLiteralNode node)
    {
        // Determine type from suffix or default to i32
        var kind = node.Suffix switch
        {
            IntegerKind.I8 => SemaIntegerKind.I8,
            IntegerKind.I16 => SemaIntegerKind.I16,
            IntegerKind.I32 => SemaIntegerKind.I32,
            IntegerKind.I64 => SemaIntegerKind.I64,
            IntegerKind.ISize => SemaIntegerKind.ISize,
            IntegerKind.U8 => SemaIntegerKind.U8,
            IntegerKind.U16 => SemaIntegerKind.U16,
            IntegerKind.U32 => SemaIntegerKind.U32,
            IntegerKind.U64 => SemaIntegerKind.U64,
            IntegerKind.USize => SemaIntegerKind.USize,
            null => SemaIntegerKind.I32, // Default
            _ => SemaIntegerKind.I32
        };

        return new SemaIntLiteral
        {
            Value = node.Value,
            Type = new SemaIntType(kind),
            Location = ConvLoc(node.Location)
        };
    }

    private SemaFloatLiteral BuildFloatLiteral(FloatLiteralNode node)
    {
        var kind = node.Suffix switch
        {
            FloatKind.F32 => SemaFloatKind.F32,
            FloatKind.F64 => SemaFloatKind.F64,
            null => SemaFloatKind.F64, // Default
            _ => SemaFloatKind.F64
        };

        return new SemaFloatLiteral
        {
            Value = node.Value,
            Type = new SemaFloatType(kind),
            Location = ConvLoc(node.Location)
        };
    }

    private SemaBoolLiteral BuildBoolLiteral(BoolLiteralNode node)
    {
        return new SemaBoolLiteral
        {
            Value = node.Value,
            Type = new SemaBoolType(),
            Location = ConvLoc(node.Location)
        };
    }

    private SemaStringLiteral BuildStringLiteral(StringLiteralNode node)
    {
        return new SemaStringLiteral
        {
            Value = node.Value,
            Type = new SemaPointerType(new SemaIntType(SemaIntegerKind.U8)),
            Location = ConvLoc(node.Location)
        };
    }

    private SemaExpression? BuildBinary(BinaryExpressionNode node)
    {
        var left = BuildExpression(node.Left);
        var right = BuildExpression(node.Right);

        if (left == null || right == null)
            return null;

        var op = ConvertBinaryOp(node.Operator);

        // Determine result type
        var resultType = DetermineBinaryResultType(left.Type, right.Type, op);

        return new SemaBinaryExpr
        {
            Left = left,
            Operator = op,
            Right = right,
            Type = resultType,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaExpression? BuildUnary(UnaryExpressionNode node)
    {
        var operand = BuildExpression(node.Operand);
        if (operand == null)
            return null;

        var op = ConvertUnaryOp(node.Operator);

        return new SemaUnaryExpr
        {
            Operator = op,
            Operand = operand,
            Type = operand.Type,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaExpression? BuildCall(CallExpressionNode node)
    {
        var target = BuildExpression(node.Target);
        if (target == null)
            return null;

        var args = new List<SemaExpression>();
        foreach (var arg in node.Arguments)
        {
            var argExpr = BuildExpression(arg.Value);
            if (argExpr != null)
                args.Add(argExpr);
        }

        // Get return type from function type
        SemaType? returnType = null;
        if (target.Type is SemaFuncType funcType)
        {
            returnType = funcType.ReturnType;
        }
        else
        {
            returnType = new SemaVoidType();
        }

        return new SemaCallExpr
        {
            Target = target,
            Arguments = args,
            Type = returnType ?? new SemaVoidType(),
            Location = ConvLoc(node.Location)
        };
    }

    private SemaExpression? BuildIdentifier(IdentifierExpressionNode node)
    {
        // Check local variables first
        if (_localIndices.TryGetValue(node.Name, out var localIndex))
        {
            // Get the actual type of the local variable
            var localType = _localTypes.TryGetValue(node.Name, out var t) ? t : new SemaIntType(SemaIntegerKind.I32);

            var firstFunc = _functions.FirstOrDefault();
            if (firstFunc != null && localIndex < firstFunc.Parameters.Count)
            {
                var param = firstFunc.Parameters[localIndex];
                return new SemaParamRef
                {
                    Name = node.Name,
                    Index = localIndex,
                    Type = param.Type,
                    Location = ConvLoc(node.Location)
                };
            }

            return new SemaLocalRef
            {
                Name = node.Name,
                Index = localIndex,
                Type = localType,
                Location = ConvLoc(node.Location)
            };
        }

        // Check globals
        var global = _globals.FirstOrDefault(g => g.Name == node.Name);
        if (global != null)
        {
            return new SemaGlobalRef
            {
                Name = node.Name,
                MangledName = global.MangledName,
                Type = global.Type,
                Location = ConvLoc(node.Location)
            };
        }

        // Check functions
        var func = _functions.FirstOrDefault(f => f.Name == node.Name);
        if (func != null)
        {
            var paramTypes = func.Parameters.Select(p => p.Type).ToList();
            return new SemaFuncRef
            {
                Name = node.Name,
                MangledName = func.MangledName,
                Type = new SemaFuncType(paramTypes, func.ReturnType),
                Location = ConvLoc(node.Location)
            };
        }

        _errors.Add(new SemanticError($"Undefined identifier: {node.Name}", ConvLoc(node.Location)));
        return null;
    }

    private SemaExpression? BuildConditional(ConditionalExpressionNode node)
    {
        var condition = BuildExpression(node.Condition);
        var thenExpr = BuildExpression(node.ThenExpr);
        var elseExpr = BuildExpression(node.ElseExpr);

        if (condition == null || thenExpr == null || elseExpr == null)
            return null;

        return new SemaConditionalExpr
        {
            Condition = condition,
            ThenExpr = thenExpr,
            ElseExpr = elseExpr,
            Type = thenExpr.Type,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaExpression? BuildArrayLiteral(ArrayLiteralNode node)
    {
        var elements = new List<SemaExpression>();
        SemaType? elementType = null;

        if (node.Elements != null)
        {
            foreach (var elem in node.Elements)
            {
                var semaElem = BuildExpression(elem);
                if (semaElem == null)
                    return null;
                elements.Add(semaElem);

                // Infer element type from first element
                elementType ??= semaElem.Type;
            }
        }

        // Use explicit type if provided
        if (node.ElementType != null)
        {
            elementType = ResolveType(node.ElementType);
        }

        if (elementType == null)
        {
            _errors.Add(new SemanticError("Cannot infer array element type",
                ConvLoc(node.Location)));
            return null;
        }

        var arrayType = new SemaArrayType(elementType, (ulong)elements.Count);

        return new SemaArrayLiteral
        {
            Elements = elements,
            Type = arrayType,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaExpression? BuildStructLiteral(StructLiteralNode node)
    {
        // Find the struct type
        var structType = _structs.FirstOrDefault(s => s.Name == node.Type.Name);
        if (structType == null)
        {
            _errors.Add(new SemanticError($"Unknown struct type: {node.Type.Name}",
                ConvLoc(node.Location)));
            return null;
        }

        var fields = new List<SemaStructLiteralField>();
        if (node.Fields != null)
        {
            foreach (var field in node.Fields)
            {
                var value = BuildExpression(field.Value);
                if (value == null)
                    return null;

                fields.Add(new SemaStructLiteralField(field.Name, value));
            }
        }

        return new SemaStructLiteral
        {
            TypeName = node.Type.Name,
            Fields = fields,
            Type = structType,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaExpression? BuildEnumLiteral(EnumLiteralNode node)
    {
        // Find the enum type
        var enumType = _enums.FirstOrDefault(e => e.Name == node.Type.Name);
        if (enumType == null)
        {
            _errors.Add(new SemanticError($"Unknown enum type: {node.Type.Name}",
                ConvLoc(node.Location)));
            return null;
        }

        // Find the member
        var member = enumType.Members.FirstOrDefault(m => m.Name == node.MemberName);
        if (member == null)
        {
            _errors.Add(new SemanticError($"Unknown enum member: {node.Type.Name}::{node.MemberName}",
                ConvLoc(node.Location)));
            return null;
        }

        // Return the enum value as an integer literal
        return new SemaIntLiteral
        {
            Value = member.Value,
            Type = enumType,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaExpression? BuildFieldAccess(FieldAccessExpressionNode node)
    {
        var target = BuildExpression(node.Target);
        if (target == null)
            return null;

        // Get the struct type from the target
        SemaStructType? structType = null;
        if (target.Type is SemaStructType st)
        {
            structType = st;
        }
        else if (target.Type is SemaNamedType namedType && namedType.UnderlyingType is SemaStructType underlying)
        {
            structType = underlying;
        }

        if (structType == null)
        {
            _errors.Add(new SemanticError($"Cannot access field '{node.FieldName}' on non-struct type {target.Type}",
                ConvLoc(node.Location)));
            return null;
        }

        // Find the field
        var fieldIndex = -1;
        SemaStructField? field = null;
        for (int i = 0; i < structType.Fields.Count; i++)
        {
            if (structType.Fields[i].Name == node.FieldName)
            {
                fieldIndex = i;
                field = structType.Fields[i];
                break;
            }
        }

        if (field == null)
        {
            _errors.Add(new SemanticError($"Struct '{structType.Name}' has no field '{node.FieldName}'",
                ConvLoc(node.Location)));
            return null;
        }

        return new SemaFieldAccessExpr
        {
            Target = target,
            FieldName = node.FieldName,
            FieldIndex = fieldIndex,
            Type = field.Type,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaExpression? BuildIndexExpr(IndexExpressionNode node)
    {
        var target = BuildExpression(node.Target);
        var index = BuildExpression(node.Index);

        if (target == null || index == null)
            return null;

        // Get the element type from the target (must be array type)
        if (target.Type is not SemaArrayType arrayType)
        {
            _errors.Add(new SemanticError($"Cannot index non-array type {target.Type}",
                ConvLoc(node.Location)));
            return null;
        }

        return new SemaIndexExpr
        {
            Target = target,
            Index = index,
            Type = arrayType.ElementType,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaGlobal BuildGlobal(ConstDeclarationNode node)
    {
        var type = ResolveType(node.Type) ?? new SemaIntType(SemaIntegerKind.I32);
        var initializer = BuildExpression(node.Initializer);

        return new SemaGlobal
        {
            Name = node.Name,
            MangledName = $"_{node.Name}",
            Type = type,
            Initializer = initializer,
            IsConstant = true,
            IsPublic = false,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaGlobal BuildGlobal(StaticDeclarationNode node)
    {
        var type = ResolveType(node.Type) ?? new SemaIntType(SemaIntegerKind.I32);
        var initializer = BuildExpression(node.Initializer);

        return new SemaGlobal
        {
            Name = node.Name,
            MangledName = $"_{node.Name}",
            Type = type,
            Initializer = initializer,
            IsConstant = false,
            IsPublic = false,
            Location = ConvLoc(node.Location)
        };
    }

    private SemaStructType BuildStructType(StructDeclarationNode node)
    {
        var fields = new List<SemaStructField>();
        var offset = 0;

        foreach (var field in node.Fields ?? [])
        {
            var fieldType = ResolveType(field.Type) ?? new SemaIntType(SemaIntegerKind.I32);
            fields.Add(new SemaStructField(field.Name, fieldType, offset));
            offset += fieldType.SizeBytes;
        }

        return new SemaStructType(node.Name, fields);
    }

    private SemaEnumType BuildEnumType(EnumDeclarationNode node)
    {
        var underlyingType = node.UnderlyingType != null
            ? ResolveType(node.UnderlyingType) ?? new SemaIntType(SemaIntegerKind.I32)
            : new SemaIntType(SemaIntegerKind.I32);

        var members = new List<SemaEnumMember>();
        ulong currentValue = 0;

        foreach (var field in node.Fields ?? [])
        {
            if (field.Value is IntegerLiteralNode intLit)
                currentValue = intLit.Value;

            members.Add(new SemaEnumMember(field.Name, currentValue));
            currentValue++;
        }

        return new SemaEnumType
        {
            Name = node.Name,
            UnderlyingType = underlyingType,
            Members = members
        };
    }

    private SemaType? ResolveType(TypeNode node)
    {
        return node switch
        {
            IntegerTypeNode intType => new SemaIntType((SemaIntegerKind)intType.Kind),
            FloatTypeNode floatType => new SemaFloatType((SemaFloatKind)floatType.Kind),
            BoolTypeNode => new SemaBoolType(),
            PointerTypeNode ptrType => new SemaPointerType(ResolveType(ptrType.TargetType)!),
            RefTypeNode refType => new SemaRefType(ResolveType(refType.TargetType)!, refType.IsMutable),
            ArrayTypeNode arrType => new SemaArrayType(ResolveType(arrType.ElementType)!, arrType.Size),
            TupleTypeNode tupleType => new SemaTupleType(tupleType.Elements.Select(ResolveType).ToList()!),
            FuncTypeNode funcType => new SemaFuncType(
                funcType.ParameterTypes.Select(ResolveType).ToList()!,
                funcType.ReturnType != null ? ResolveType(funcType.ReturnType) : null
            ),
            NamedTypeNode namedType => ResolveNamedType(namedType),
            _ => null
        };
    }

    private SemaType? ResolveNamedType(NamedTypeNode node)
    {
        var structType = _structs.FirstOrDefault(s => s.Name == node.Name);
        if (structType != null)
            return structType;

        var enumType = _enums.FirstOrDefault(e => e.Name == node.Name);
        if (enumType != null)
            return enumType;

        return new SemaNamedType(node.FullyQualifiedName, null);
    }

    private string GetMangledName(FuncDeclarationNode node)
    {
        if (node.Name == "main")
            return "main";

        var ns = node.NamespacePrefix != null ? string.Join("_", node.NamespacePrefix) + "_" : "";
        return $"{ns}{node.Name}";
    }

    private SemaBinaryOp ConvertBinaryOp(BinaryOperator op) => op switch
    {
        BinaryOperator.Add => SemaBinaryOp.Add,
        BinaryOperator.Subtract => SemaBinaryOp.Subtract,
        BinaryOperator.Multiply => SemaBinaryOp.Multiply,
        BinaryOperator.Divide => SemaBinaryOp.Divide,
        BinaryOperator.Modulo => SemaBinaryOp.Modulo,
        BinaryOperator.Equal => SemaBinaryOp.Equal,
        BinaryOperator.NotEqual => SemaBinaryOp.NotEqual,
        BinaryOperator.Less => SemaBinaryOp.Less,
        BinaryOperator.Greater => SemaBinaryOp.Greater,
        BinaryOperator.LessEqual => SemaBinaryOp.LessEqual,
        BinaryOperator.GreaterEqual => SemaBinaryOp.GreaterEqual,
        BinaryOperator.LogicalAnd => SemaBinaryOp.LogicalAnd,
        BinaryOperator.LogicalOr => SemaBinaryOp.LogicalOr,
        BinaryOperator.BitwiseAnd => SemaBinaryOp.BitwiseAnd,
        BinaryOperator.BitwiseOr => SemaBinaryOp.BitwiseOr,
        BinaryOperator.BitwiseXor => SemaBinaryOp.BitwiseXor,
        BinaryOperator.ShiftLeft => SemaBinaryOp.ShiftLeft,
        BinaryOperator.ShiftRight => SemaBinaryOp.ShiftRight,
        _ => SemaBinaryOp.Add
    };

    private SemaUnaryOp ConvertUnaryOp(UnaryOperator op) => op switch
    {
        UnaryOperator.Negate => SemaUnaryOp.Negate,
        UnaryOperator.LogicalNot => SemaUnaryOp.LogicalNot,
        UnaryOperator.BitwiseNot => SemaUnaryOp.BitwiseNot,
        UnaryOperator.AddressOf => SemaUnaryOp.AddressOf,
        UnaryOperator.Dereference => SemaUnaryOp.Dereference,
        UnaryOperator.Ref => SemaUnaryOp.Ref,
        UnaryOperator.MutRef => SemaUnaryOp.MutRef,
        _ => SemaUnaryOp.Negate
    };

    private SemaAssignOp ConvertAssignOp(AssignmentOperator op) => op switch
    {
        AssignmentOperator.Assign => SemaAssignOp.Assign,
        AssignmentOperator.AddAssign => SemaAssignOp.AddAssign,
        AssignmentOperator.SubtractAssign => SemaAssignOp.SubtractAssign,
        AssignmentOperator.MultiplyAssign => SemaAssignOp.MultiplyAssign,
        AssignmentOperator.DivideAssign => SemaAssignOp.DivideAssign,
        AssignmentOperator.ModuloAssign => SemaAssignOp.ModuloAssign,
        AssignmentOperator.AndAssign => SemaAssignOp.AndAssign,
        AssignmentOperator.OrAssign => SemaAssignOp.OrAssign,
        AssignmentOperator.XorAssign => SemaAssignOp.XorAssign,
        AssignmentOperator.ShiftLeftAssign => SemaAssignOp.ShlAssign,
        AssignmentOperator.ShiftRightAssign => SemaAssignOp.ShrAssign,
        _ => SemaAssignOp.Assign
    };

    private SemaType DetermineBinaryResultType(SemaType left, SemaType right, SemaBinaryOp op)
    {
        if (op is SemaBinaryOp.Equal or SemaBinaryOp.NotEqual or
            SemaBinaryOp.Less or SemaBinaryOp.Greater or
            SemaBinaryOp.LessEqual or SemaBinaryOp.GreaterEqual)
        {
            return new SemaBoolType();
        }

        if (op is SemaBinaryOp.LogicalAnd or SemaBinaryOp.LogicalOr)
        {
            return new SemaBoolType();
        }

        if (left is SemaIntType leftInt && right is SemaIntType rightInt)
        {
            return leftInt.SizeBytes >= rightInt.SizeBytes ? left : right;
        }

        if (left is SemaFloatType || right is SemaFloatType)
        {
            return left is SemaFloatType leftFloat ? left : right;
        }

        return left;
    }
}
