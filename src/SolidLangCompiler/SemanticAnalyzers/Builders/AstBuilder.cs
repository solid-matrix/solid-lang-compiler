using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using SolidLangCompiler.AST;
using SolidLangCompiler.AST.Declarations;
using SolidLangCompiler.AST.Expressions;
using SolidLangCompiler.AST.Statements;
using SolidLangCompiler.AST.Types;
using SolidLangCompiler.Generated;

namespace SolidLangCompiler.SemanticAnalyzers.Builders;

/// <summary>
/// Builds AST from ANTLR parse tree.
/// </summary>
public class AstBuilder : SolidLangParserBaseVisitor<AstNode>
{
    private string _filePath = "<unknown>";

    private SourceLocation GetLocation(ParserRuleContext context)
    {
        return new SourceLocation(_filePath, context.Start.Line, context.Start.Column);
    }

    /// <summary>
    /// Builds an AST from a parse tree.
    /// </summary>
    public ProgramNode Build(SolidLangParser.ProgramContext context, string filePath)
    {
        _filePath = filePath;
        return (ProgramNode)VisitProgram(context);
    }

    // Program
    public override AstNode VisitProgram(SolidLangParser.ProgramContext context)
    {
        NamespaceDeclarationNode? ns = null;
        if (context.namespace_decl() is { } nsCtx)
        {
            ns = (NamespaceDeclarationNode)Visit(nsCtx);
        }

        var usings = new List<UsingDeclarationNode>();
        foreach (var usingCtx in context.using_decl())
        {
            usings.Add((UsingDeclarationNode)Visit(usingCtx));
        }

        var decls = new List<DeclarationNode>();
        foreach (var decl in context.const_decl())
            decls.Add((DeclarationNode)Visit(decl));
        foreach (var decl in context.static_decl())
            decls.Add((DeclarationNode)Visit(decl));
        foreach (var decl in context.struct_decl())
            decls.Add((DeclarationNode)Visit(decl));
        foreach (var decl in context.union_decl())
            decls.Add((DeclarationNode)Visit(decl));
        foreach (var decl in context.enum_decl())
            decls.Add((DeclarationNode)Visit(decl));
        foreach (var decl in context.variant_decl())
            decls.Add((DeclarationNode)Visit(decl));
        foreach (var decl in context.interface_decl())
            decls.Add((DeclarationNode)Visit(decl));
        foreach (var decl in context.func_decl())
            decls.Add((DeclarationNode)Visit(decl));

        return new ProgramNode(ns, usings.Count > 0 ? usings : null, decls.Count > 0 ? decls : null)
        {
            Location = GetLocation(context)
        };
    }

    // Namespace
    public override AstNode VisitNamespace_decl(SolidLangParser.Namespace_declContext context)
    {
        var path = context.namespace_path().ID().Select(id => id.GetText()).ToList();
        return new NamespaceDeclarationNode(path) { Location = GetLocation(context) };
    }

    // Using
    public override AstNode VisitUsing_decl(SolidLangParser.Using_declContext context)
    {
        var path = context.namespace_path().ID().Select(id => id.GetText()).ToList();
        return new UsingDeclarationNode(path) { Location = GetLocation(context) };
    }

    // Types
    public override AstNode VisitPrimitive_type(SolidLangParser.Primitive_typeContext context)
    {
        var text = context.GetText();
        return text switch
        {
            "i8" => new IntegerTypeNode(IntegerKind.I8),
            "i16" => new IntegerTypeNode(IntegerKind.I16),
            "i32" => new IntegerTypeNode(IntegerKind.I32),
            "i64" => new IntegerTypeNode(IntegerKind.I64),
            "isize" => new IntegerTypeNode(IntegerKind.ISize),
            "u8" => new IntegerTypeNode(IntegerKind.U8),
            "u16" => new IntegerTypeNode(IntegerKind.U16),
            "u32" => new IntegerTypeNode(IntegerKind.U32),
            "u64" => new IntegerTypeNode(IntegerKind.U64),
            "usize" => new IntegerTypeNode(IntegerKind.USize),
            "f32" => new FloatTypeNode(FloatKind.F32),
            "f64" => new FloatTypeNode(FloatKind.F64),
            "bool" => new BoolTypeNode(),
            _ => throw new InvalidOperationException($"Unknown primitive type: {text}")
        };
    }

    public override AstNode VisitArray_type(SolidLangParser.Array_typeContext context)
    {
        var elementType = (TypeNode)Visit(context.type());

        // Parse the size expression - it should be an integer literal
        ulong size = 0;
        var exprContext = context.expr();
        if (exprContext != null)
        {
            // Try to get the integer literal value
            var expr = Visit(exprContext);
            if (expr is IntegerLiteralNode intLit)
            {
                size = intLit.Value;
            }
        }

        return new ArrayTypeNode(elementType, size) { Location = GetLocation(context) };
    }

    public override AstNode VisitTuple_type(SolidLangParser.Tuple_typeContext context)
    {
        var elements = context.type().Select(t => (TypeNode)Visit(t)).ToList();
        return new TupleTypeNode(elements) { Location = GetLocation(context) };
    }

    public override AstNode VisitRef_type(SolidLangParser.Ref_typeContext context)
    {
        var targetType = (TypeNode)Visit(context.type());
        var isMutable = context.NOT() != null;
        return new RefTypeNode(targetType, isMutable) { Location = GetLocation(context) };
    }

    public override AstNode VisitPointer_type(SolidLangParser.Pointer_typeContext context)
    {
        var targetType = (TypeNode)Visit(context.type());
        return new PointerTypeNode(targetType) { Location = GetLocation(context) };
    }

    public override AstNode VisitFunc_type(SolidLangParser.Func_typeContext context)
    {
        var paramTypes = context.type() != null
            ? context.type().Take(context.type().Length - 1).Select(t => (TypeNode)Visit(t)).ToList()
            : new List<TypeNode>();

        TypeNode? returnType = context.type() != null && context.type().Length > 0
            ? (TypeNode)Visit(context.type().Last())
            : null;

        CallingConvention? conv = context.call_convention()?.GetText() switch
        {
            "cdecl" => CallingConvention.CDecl,
            "stdcall" => CallingConvention.StdCall,
            _ => null
        };

        return new FuncTypeNode(paramTypes, returnType, conv) { Location = GetLocation(context) };
    }

    public override AstNode VisitNamed_type(SolidLangParser.Named_typeContext context)
    {
        var ns = context.namespace_prefix()?.namespace_path().ID().Select(id => id.GetText()).ToList();
        var name = context.ID().GetText();
        var genArgs = context.type() != null
            ? context.type().Select(t => (TypeNode)Visit(t)).ToList()
            : null;

        return new NamedTypeNode(ns, name, genArgs) { Location = GetLocation(context) };
    }

    // Declarations
    public override AstNode VisitConst_decl(SolidLangParser.Const_declContext context)
    {
        var name = context.ID().GetText();
        var type = (TypeNode)Visit(context.type());
        var initializer = (ExpressionNode)Visit(context.expr());

        return new ConstDeclarationNode(null, name, type, initializer) { Location = GetLocation(context) };
    }

    public override AstNode VisitVar_decl(SolidLangParser.Var_declContext context)
    {
        var name = context.ID().GetText();
        var type = context.type() != null ? (TypeNode)Visit(context.type()) : null;
        var initializer = context.expr() != null ? (ExpressionNode)Visit(context.expr()) : null;

        // Check if this is in a statement context (for init) or declaration context
        // For now, always return VarDeclarationNode for top-level
        return new VarDeclarationNode(null, name, type, initializer) { Location = GetLocation(context) };
    }

    // For init statement context
    public override AstNode VisitFor_init(SolidLangParser.For_initContext context)
    {
        if (context.var_decl() is { } varDecl)
        {
            var name = varDecl.ID().GetText();
            var type = varDecl.type() != null ? (TypeNode)Visit(varDecl.type()) : null;
            var initializer = varDecl.expr() != null ? (ExpressionNode)Visit(varDecl.expr()) : null;
            return new VarDeclStatementNode(null, name, type, initializer) { Location = GetLocation(context) };
        }
        if (context.assign_stmt() is { } assignStmt)
            return Visit(assignStmt);
        throw new InvalidOperationException("Unknown for init type");
    }

    public override AstNode VisitStatic_decl(SolidLangParser.Static_declContext context)
    {
        var name = context.ID().GetText();
        var type = (TypeNode)Visit(context.type());
        var initializer = (ExpressionNode)Visit(context.expr());

        return new StaticDeclarationNode(null, name, type, initializer) { Location = GetLocation(context) };
    }

    public override AstNode VisitStruct_decl(SolidLangParser.Struct_declContext context)
    {
        var name = context.ID().GetText();
        var genParams = context.generic_params()?.generic_param().Select(p => p.ID().GetText()).ToList();
        var fields = context.struct_fields()?.struct_field().Select(f => new StructFieldNode(
            null,
            f.ID().GetText(),
            (TypeNode)Visit(f.type())
        )).ToList();

        return new StructDeclarationNode(null, name, genParams, null, fields) { Location = GetLocation(context) };
    }

    public override AstNode VisitEnum_decl(SolidLangParser.Enum_declContext context)
    {
        var name = context.ID().GetText();
        var underlyingType = context.type() != null ? (TypeNode)Visit(context.type()) : null;
        var fields = context.enum_fields()?.enum_field().Select(f => new EnumFieldNode(
            null,
            f.ID().GetText(),
            f.expr() != null ? (ExpressionNode)Visit(f.expr()) : null
        )).ToList();

        return new EnumDeclarationNode(null, name, underlyingType, fields) { Location = GetLocation(context) };
    }

    public override AstNode VisitUnion_decl(SolidLangParser.Union_declContext context)
    {
        var name = context.ID().GetText();
        var genParams = context.generic_params()?.generic_param().Select(p => p.ID().GetText()).ToList();
        var fields = context.union_fields()?.union_field().Select(f => new UnionFieldNode(
            null,
            f.ID().GetText(),
            (TypeNode)Visit(f.type())
        )).ToList();

        return new UnionDeclarationNode(null, name, genParams, null, fields) { Location = GetLocation(context) };
    }

    public override AstNode VisitVariant_decl(SolidLangParser.Variant_declContext context)
    {
        var name = context.ID().GetText();
        var genParams = context.generic_params()?.generic_param().Select(p => p.ID().GetText()).ToList();
        var tagType = context.type() != null ? (TypeNode)Visit(context.type()) : null;
        var fields = context.variant_fields()?.variant_field().Select(f => new VariantFieldNode(
            null,
            f.ID().GetText(),
            f.type() != null ? (TypeNode)Visit(f.type()) : null
        )).ToList();

        return new VariantDeclarationNode(null, name, genParams, tagType, null, fields) { Location = GetLocation(context) };
    }

    public override AstNode VisitInterface_decl(SolidLangParser.Interface_declContext context)
    {
        var name = context.ID().GetText();
        var genParams = context.generic_params()?.generic_param().Select(p => p.ID().GetText()).ToList();
        var fields = context.interface_fields()?.interface_field().Select(f => new InterfaceFieldNode(
            null,
            f.ID().GetText(),
            f.func_parameters()?.func_parameter().Select(p => new FuncParameterNode(
                null,
                p.ID().GetText(),
                (TypeNode)Visit(p.type())
            )).ToList(),
            f.type() != null ? (TypeNode)Visit(f.type()) : null
        )).ToList();

        return new InterfaceDeclarationNode(null, name, genParams, null, fields) { Location = GetLocation(context) };
    }

    public override AstNode VisitFunc_decl(SolidLangParser.Func_declContext context)
    {
        var ns = context.namespace_prefix()?.namespace_path().ID().Select(id => id.GetText()).ToList();
        var header = context.func_header();
        var name = header.ID().GetText();
        var genParams = header.generic_params()?.generic_param().Select(p => p.ID().GetText()).ToList();
        var parameters = header.func_parameters()?.func_parameter().Select(p => new FuncParameterNode(
            null,
            p.ID().GetText(),
            (TypeNode)Visit(p.type())
        )).ToList();

        CallingConvention? conv = header.call_convention()?.GetText() switch
        {
            "cdecl" => CallingConvention.CDecl,
            "stdcall" => CallingConvention.StdCall,
            _ => null
        };

        var returnType = header.type() != null ? (TypeNode)Visit(header.type()) : null;
        var body = context.body_stmt() != null ? (BlockStatementNode)Visit(context.body_stmt()) : null;

        return new FuncDeclarationNode(null, ns, name, genParams, parameters, conv, returnType, null, body)
        {
            Location = GetLocation(context)
        };
    }

    // Statements
    public override AstNode VisitEmpty_stmt(SolidLangParser.Empty_stmtContext context)
    {
        return new EmptyStatementNode() { Location = GetLocation(context) };
    }

    public override AstNode VisitBody_stmt(SolidLangParser.Body_stmtContext context)
    {
        var stmts = new List<StatementNode>();
        foreach (var s in context.stmt())
        {
            var node = Visit(s);
            if (node is StatementNode stmt)
            {
                stmts.Add(stmt);
            }
            else if (node is VarDeclarationNode varDecl)
            {
                // Convert var declaration to statement
                stmts.Add(new VarDeclStatementNode(
                    varDecl.Annotations,
                    varDecl.Name,
                    varDecl.Type,
                    varDecl.Initializer
                ) { Location = varDecl.Location });
            }
            else if (node is ConstDeclarationNode constDecl)
            {
                // const in statement context - create local const
                stmts.Add(new VarDeclStatementNode(
                    constDecl.Annotations,
                    constDecl.Name,
                    constDecl.Type,
                    constDecl.Initializer
                ) { Location = constDecl.Location });
            }
        }
        return new BlockStatementNode(stmts) { Location = GetLocation(context) };
    }

    public override AstNode VisitAssign_stmt(SolidLangParser.Assign_stmtContext context)
    {
        var exprs = context.expr();
        if (exprs.Length < 2)
        {
            return new EmptyStatementNode() { Location = GetLocation(context) };
        }

        var target = (ExpressionNode)Visit(exprs[0]);
        var value = (ExpressionNode)Visit(exprs[1]);

        var opText = context switch
        {
            _ when context.EQ() != null => "=",
            _ when context.PLUSEQ() != null => "+=",
            _ when context.MINUSEQ() != null => "-=",
            _ when context.STAREQ() != null => "*=",
            _ when context.SLASHEQ() != null => "/=",
            _ when context.PERCENTEQ() != null => "%=",
            _ when context.ANDEQ() != null => "&=",
            _ when context.OREQ() != null => "|=",
            _ when context.CARETEQ() != null => "^=",
            _ when context.SHLEQ() != null => "<<=",
            _ when context.SHREQ() != null => ">>=",
            _ => "="
        };

        var op = opText switch
        {
            "=" => AssignmentOperator.Assign,
            "+=" => AssignmentOperator.AddAssign,
            "-=" => AssignmentOperator.SubtractAssign,
            "*=" => AssignmentOperator.MultiplyAssign,
            "/=" => AssignmentOperator.DivideAssign,
            "%=" => AssignmentOperator.ModuloAssign,
            "&=" => AssignmentOperator.AndAssign,
            "|=" => AssignmentOperator.OrAssign,
            "^=" => AssignmentOperator.XorAssign,
            "<<=" => AssignmentOperator.ShiftLeftAssign,
            ">>=" => AssignmentOperator.ShiftRightAssign,
            _ => AssignmentOperator.Assign
        };

        return new AssignmentStatementNode(target, op, value) { Location = GetLocation(context) };
    }

    public override AstNode VisitExpr_stmt(SolidLangParser.Expr_stmtContext context)
    {
        var expr = (ExpressionNode)Visit(context.expr());
        return new ExpressionStatementNode(expr) { Location = GetLocation(context) };
    }

    public override AstNode VisitDefer_stmt(SolidLangParser.Defer_stmtContext context)
    {
        var expr = (ExpressionNode)Visit(context.expr());
        return new DeferStatementNode(expr) { Location = GetLocation(context) };
    }

    public override AstNode VisitIf_stmt(SolidLangParser.If_stmtContext context)
    {
        var condition = (ExpressionNode)Visit(context.expr());
        var bodyStmts = context.body_stmt();
        var thenBlock = (BlockStatementNode)Visit(bodyStmts[0]);

        StatementNode? elseBranch = null;
        if (context.ELSE() != null && bodyStmts.Length > 1)
        {
            elseBranch = (BlockStatementNode)Visit(bodyStmts[1]);
        }
        else if (context.if_stmt() != null)
        {
            elseBranch = (StatementNode)Visit(context.if_stmt());
        }

        return new IfStatementNode(condition, thenBlock, elseBranch) { Location = GetLocation(context) };
    }

    public override AstNode VisitBreak_stmt(SolidLangParser.Break_stmtContext context)
    {
        return new BreakStatementNode() { Location = GetLocation(context) };
    }

    public override AstNode VisitContinue_stmt(SolidLangParser.Continue_stmtContext context)
    {
        return new ContinueStatementNode() { Location = GetLocation(context) };
    }

    public override AstNode VisitReturn_stmt(SolidLangParser.Return_stmtContext context)
    {
        var value = context.expr() != null ? (ExpressionNode)Visit(context.expr()) : null;
        return new ReturnStatementNode(value) { Location = GetLocation(context) };
    }

    // Switch statement
    public override AstNode VisitSwitch_stmt(SolidLangParser.Switch_stmtContext context)
    {
        var expr = (ExpressionNode)Visit(context.expr());
        var arms = context.switch_arm().Select(a => (SwitchArmNode)Visit(a)).ToList();
        return new SwitchStatementNode(expr, arms) { Location = GetLocation(context) };
    }

    public override AstNode VisitSwitch_arm(SolidLangParser.Switch_armContext context)
    {
        PatternNode? pattern = null;
        if (context.pattern() is { } patternCtx)
        {
            pattern = (PatternNode)Visit(patternCtx);
        }
        // else branch (context.ELSE() != null)

        var stmt = (StatementNode)Visit(context.stmt());
        return new SwitchArmNode(pattern, stmt) { Location = GetLocation(context) };
    }

    public override AstNode VisitPattern(SolidLangParser.PatternContext context)
    {
        // Type::Member or Type::Member(binding) pattern - check this first
        // because enum_literal (Color::Red) also matches literal rule
        if (context.named_type() is { } namedType)
        {
            var type = (NamedTypeNode)Visit(namedType);
            // The ID after SCOPE is the member name
            var memberName = context.ID().GetText();
            IReadOnlyList<PatternNode>? bindings = null;
            if (context.pattern_binding() is { } bindingCtx)
            {
                bindings = bindingCtx.pattern().Select(p => (PatternNode)Visit(p)).ToList();
            }
            return new TypePatternNode(type, memberName, bindings) { Location = GetLocation(context) };
        }

        // literal pattern
        if (context.literal() is { } literal)
        {
            // Check if this is an enum_literal (Type::Member form)
            if (literal.enum_literal() is { } enumLit)
            {
                // Convert enum_literal to TypePattern
                var type = (NamedTypeNode)Visit(enumLit.named_type());
                var memberName = enumLit.ID().GetText();
                return new TypePatternNode(type, memberName, null) { Location = GetLocation(context) };
            }

            var litExpr = (LiteralExpressionNode)Visit(literal);
            return new LiteralPatternNode(litExpr) { Location = GetLocation(context) };
        }

        // Identifier pattern (variable binding) - just a single ID
        if (context.ID() is { } id)
        {
            return new IdentifierPatternNode(id.GetText()) { Location = GetLocation(context) };
        }

        throw new InvalidOperationException("Unknown pattern type");
    }

    // For statements
    public override AstNode VisitFor_stmt(SolidLangParser.For_stmtContext context)
    {
        if (context.for_infinite() is { } infinite)
            return VisitFor_infinite(infinite);
        if (context.for_cond() is { } cond)
            return VisitFor_cond(cond);
        if (context.for_cstyle() is { } cstyle)
            return VisitFor_cstyle(cstyle);
        if (context.@foreach() is { } each)
            return VisitForeach(each);

        throw new InvalidOperationException("Unknown for statement type");
    }

    public override AstNode VisitFor_infinite(SolidLangParser.For_infiniteContext context)
    {
        var body = (BlockStatementNode)Visit(context.body_stmt());
        return new InfiniteForNode(body) { Location = GetLocation(context) };
    }

    public override AstNode VisitFor_cond(SolidLangParser.For_condContext context)
    {
        var condition = (ExpressionNode)Visit(context.expr());
        var body = (BlockStatementNode)Visit(context.body_stmt());
        return new ConditionalForNode(condition, body) { Location = GetLocation(context) };
    }

    public override AstNode VisitFor_cstyle(SolidLangParser.For_cstyleContext context)
    {
        StatementNode? init = null;
        if (context.for_init() is { } initCtx)
        {
            init = (StatementNode)Visit(initCtx);
        }

        var condition = context.expr(0) != null ? (ExpressionNode)Visit(context.expr(0)) : null;
        var update = context.expr(1) != null ? (ExpressionNode)Visit(context.expr(1)) : null;
        var body = (BlockStatementNode)Visit(context.body_stmt());

        return new CStyleForNode(init, condition, update, body) { Location = GetLocation(context) };
    }

    public override AstNode VisitForeach(SolidLangParser.ForeachContext context)
    {
        var varName = context.ID().GetText();
        var iterable = (ExpressionNode)Visit(context.expr());
        var body = (BlockStatementNode)Visit(context.body_stmt());
        return new ForeachNode(varName, iterable, body) { Location = GetLocation(context) };
    }

    // Expressions
    public override AstNode VisitExpr(SolidLangParser.ExprContext context)
    {
        return Visit(context.conditional_expr());
    }

    public override AstNode VisitConditional_expr(SolidLangParser.Conditional_exprContext context)
    {
        var orExpr = (ExpressionNode)Visit(context.or_expr());

        if (context.QUESTION() != null)
        {
            var thenExpr = (ExpressionNode)Visit(context.expr());
            var elseExpr = (ExpressionNode)Visit(context.conditional_expr());
            return new ConditionalExpressionNode(orExpr, thenExpr, elseExpr) { Location = GetLocation(context) };
        }

        return orExpr;
    }

    public override AstNode VisitOr_expr(SolidLangParser.Or_exprContext context)
    {
        var andExprs = context.and_expr();
        var result = (ExpressionNode)Visit(andExprs[0]);

        for (int i = 1; i < andExprs.Length; i++)
        {
            var right = (ExpressionNode)Visit(andExprs[i]);
            result = new BinaryExpressionNode(result, BinaryOperator.LogicalOr, right)
            {
                Location = GetLocation(context)
            };
        }

        return result;
    }

    public override AstNode VisitAnd_expr(SolidLangParser.And_exprContext context)
    {
        var bitOrExprs = context.bit_or_expr();
        var result = (ExpressionNode)Visit(bitOrExprs[0]);

        for (int i = 1; i < bitOrExprs.Length; i++)
        {
            var right = (ExpressionNode)Visit(bitOrExprs[i]);
            result = new BinaryExpressionNode(result, BinaryOperator.LogicalAnd, right)
            {
                Location = GetLocation(context)
            };
        }

        return result;
    }

    public override AstNode VisitBit_or_expr(SolidLangParser.Bit_or_exprContext context)
    {
        var bitXorExprs = context.bit_xor_expr();
        var result = (ExpressionNode)Visit(bitXorExprs[0]);

        for (int i = 1; i < bitXorExprs.Length; i++)
        {
            var right = (ExpressionNode)Visit(bitXorExprs[i]);
            result = new BinaryExpressionNode(result, BinaryOperator.BitwiseOr, right)
            {
                Location = GetLocation(context)
            };
        }

        return result;
    }

    public override AstNode VisitBit_xor_expr(SolidLangParser.Bit_xor_exprContext context)
    {
        var bitAndExprs = context.bit_and_expr();
        var result = (ExpressionNode)Visit(bitAndExprs[0]);

        for (int i = 1; i < bitAndExprs.Length; i++)
        {
            var right = (ExpressionNode)Visit(bitAndExprs[i]);
            result = new BinaryExpressionNode(result, BinaryOperator.BitwiseXor, right)
            {
                Location = GetLocation(context)
            };
        }

        return result;
    }

    public override AstNode VisitBit_and_expr(SolidLangParser.Bit_and_exprContext context)
    {
        var eqExprs = context.eq_expr();
        var result = (ExpressionNode)Visit(eqExprs[0]);

        for (int i = 1; i < eqExprs.Length; i++)
        {
            var right = (ExpressionNode)Visit(eqExprs[i]);
            result = new BinaryExpressionNode(result, BinaryOperator.BitwiseAnd, right)
            {
                Location = GetLocation(context)
            };
        }

        return result;
    }

    public override AstNode VisitEq_expr(SolidLangParser.Eq_exprContext context)
    {
        var cmpExprs = context.cmp_expr();
        var result = (ExpressionNode)Visit(cmpExprs[0]);

        for (int i = 1; i < cmpExprs.Length; i++)
        {
            var right = (ExpressionNode)Visit(cmpExprs[i]);
            var op = context.EQEQ(i - 1) != null ? BinaryOperator.Equal : BinaryOperator.NotEqual;
            result = new BinaryExpressionNode(result, op, right)
            {
                Location = GetLocation(context)
            };
        }

        return result;
    }

    public override AstNode VisitCmp_expr(SolidLangParser.Cmp_exprContext context)
    {
        var shiftExprs = context.shift_expr();
        var result = (ExpressionNode)Visit(shiftExprs[0]);

        for (int i = 1; i < shiftExprs.Length; i++)
        {
            var right = (ExpressionNode)Visit(shiftExprs[i]);
            var op = GetComparisonOperator(context, i - 1);
            result = new BinaryExpressionNode(result, op, right)
            {
                Location = GetLocation(context)
            };
        }

        return result;
    }

    private static BinaryOperator GetComparisonOperator(SolidLangParser.Cmp_exprContext context, int index)
    {
        if (context.LT(index) != null) return BinaryOperator.Less;
        if (context.GT(index) != null) return BinaryOperator.Greater;
        if (context.LE(index) != null) return BinaryOperator.LessEqual;
        if (context.GE(index) != null) return BinaryOperator.GreaterEqual;
        return BinaryOperator.Less;
    }

    public override AstNode VisitShift_expr(SolidLangParser.Shift_exprContext context)
    {
        var addExprs = context.add_expr();
        var result = (ExpressionNode)Visit(addExprs[0]);

        for (int i = 1; i < addExprs.Length; i++)
        {
            var right = (ExpressionNode)Visit(addExprs[i]);
            var op = context.SHL(i - 1) != null ? BinaryOperator.ShiftLeft : BinaryOperator.ShiftRight;
            result = new BinaryExpressionNode(result, op, right)
            {
                Location = GetLocation(context)
            };
        }

        return result;
    }

    public override AstNode VisitAdd_expr(SolidLangParser.Add_exprContext context)
    {
        var mulExprs = context.mul_expr();
        var result = (ExpressionNode)Visit(mulExprs[0]);

        for (int i = 1; i < mulExprs.Length; i++)
        {
            var right = (ExpressionNode)Visit(mulExprs[i]);
            var op = context.PLUS(i - 1) != null ? BinaryOperator.Add : BinaryOperator.Subtract;
            result = new BinaryExpressionNode(result, op, right)
            {
                Location = GetLocation(context)
            };
        }

        return result;
    }

    public override AstNode VisitMul_expr(SolidLangParser.Mul_exprContext context)
    {
        var unaryExprs = context.unary_expr();
        var result = (ExpressionNode)Visit(unaryExprs[0]);

        for (int i = 1; i < unaryExprs.Length; i++)
        {
            var right = (ExpressionNode)Visit(unaryExprs[i]);
            var op = GetMulOperator(context, i - 1);
            result = new BinaryExpressionNode(result, op, right)
            {
                Location = GetLocation(context)
            };
        }

        return result;
    }

    private static BinaryOperator GetMulOperator(SolidLangParser.Mul_exprContext context, int index)
    {
        if (context.STAR(index) != null) return BinaryOperator.Multiply;
        if (context.SLASH(index) != null) return BinaryOperator.Divide;
        if (context.MOD(index) != null) return BinaryOperator.Modulo;
        return BinaryOperator.Multiply;
    }

    public override AstNode VisitUnary_expr(SolidLangParser.Unary_exprContext context)
    {
        // Check for unary operators
        if (context.MINUS() != null)
        {
            var operand = (ExpressionNode)Visit(context.unary_expr());
            return new UnaryExpressionNode(UnaryOperator.Negate, operand) { Location = GetLocation(context) };
        }
        if (context.NOT() != null)
        {
            var operand = (ExpressionNode)Visit(context.unary_expr());
            return new UnaryExpressionNode(UnaryOperator.LogicalNot, operand) { Location = GetLocation(context) };
        }
        if (context.TILDE() != null)
        {
            var operand = (ExpressionNode)Visit(context.unary_expr());
            return new UnaryExpressionNode(UnaryOperator.BitwiseNot, operand) { Location = GetLocation(context) };
        }
        if (context.AND() != null)
        {
            var operand = (ExpressionNode)Visit(context.unary_expr());
            return new UnaryExpressionNode(UnaryOperator.AddressOf, operand) { Location = GetLocation(context) };
        }
        if (context.STAR() != null)
        {
            var operand = (ExpressionNode)Visit(context.unary_expr());
            return new UnaryExpressionNode(UnaryOperator.Dereference, operand) { Location = GetLocation(context) };
        }
        // Check for ^ (ref) or ^! (mut ref)
        if (context.CARET() != null)
        {
            var operand = (ExpressionNode)Visit(context.unary_expr());
            // The lexer handles ^! as two separate tokens: CARET and NOT
            // We need to check if NOT follows CARET in the unary expression
            // For simplicity, just check if NOT exists in the context
            var op = context.NOT() != null ? UnaryOperator.MutRef : UnaryOperator.Ref;
            return new UnaryExpressionNode(op, operand) { Location = GetLocation(context) };
        }

        return Visit(context.postfix_expr());
    }

    public override AstNode VisitPostfix_expr(SolidLangParser.Postfix_exprContext context)
    {
        var result = (ExpressionNode)Visit(context.primary_expr());

        foreach (var suffix in context.postfix_suffix())
        {
            result = ApplyPostfixSuffix(result, suffix);
        }

        return result;
    }

    private ExpressionNode ApplyPostfixSuffix(ExpressionNode target, SolidLangParser.Postfix_suffixContext suffix)
    {
        if (suffix.DOT() != null)
        {
            var fieldName = suffix.ID().GetText();
            return new FieldAccessExpressionNode(target, fieldName) { Location = GetLocation(suffix) };
        }
        if (suffix.LBRACKET() != null)
        {
            var index = (ExpressionNode)Visit(suffix.expr());
            return new IndexExpressionNode(target, index) { Location = GetLocation(suffix) };
        }
        if (suffix.LPAREN() != null)
        {
            var args = suffix.call_args()?.call_arg().Select(a =>
            {
                if (a.expr() != null)
                    return new CallArgumentNode((ExpressionNode)Visit(a.expr()), null);
                var namedArg = a.ID()?.GetText();
                return new CallArgumentNode((ExpressionNode)Visit(a.expr()), namedArg);
            }).ToList() ?? new List<CallArgumentNode>();
            return new CallExpressionNode(target, args) { Location = GetLocation(suffix) };
        }

        return target;
    }

    public override AstNode VisitPrimary_expr(SolidLangParser.Primary_exprContext context)
    {
        if (context.literal() != null)
        {
            return Visit(context.literal());
        }
        if (context.ID() != null)
        {
            var name = context.ID().GetText();
            return new IdentifierExpressionNode(name) { Location = GetLocation(context) };
        }
        if (context.LPAREN() != null && context.expr() != null)
        {
            return Visit(context.expr());
        }
        if (context.tuple_literal() != null)
        {
            return Visit(context.tuple_literal());
        }
        if (context.struct_literal() != null)
        {
            return Visit(context.struct_literal());
        }
        if (context.array_literal() != null)
        {
            return Visit(context.array_literal());
        }
        if (context.meta_expr() != null)
        {
            // @id(args) - meta expression
            return Visit(context.meta_expr());
        }

        return new IntegerLiteralNode(0, null) { Location = GetLocation(context) };
    }

    // Literals
    public override AstNode VisitInteger_literal(SolidLangParser.Integer_literalContext context)
    {
        var text = context.INTEGER_LITERAL().GetText();

        IntegerKind? suffix = null;
        var lowered = text.ToLower();
        foreach (var kind in Enum.GetValues<IntegerKind>())
        {
            if (lowered.EndsWith(kind.ToString().ToLower()))
            {
                suffix = kind;
                text = text[..^kind.ToString().Length];
                break;
            }
        }

        ulong value = 0;
        if (text.StartsWith("0x"))
            value = Convert.ToUInt64(text[2..], 16);
        else if (text.StartsWith("0o"))
            value = Convert.ToUInt64(text[2..], 8);
        else if (text.StartsWith("0b"))
            value = Convert.ToUInt64(text[2..], 2);
        else
            value = ulong.Parse(text.Replace("_", ""));

        return new IntegerLiteralNode(value, suffix) { Location = GetLocation(context) };
    }

    public override AstNode VisitFloat_literal(SolidLangParser.Float_literalContext context)
    {
        var text = context.FLOAT_LITERAL().GetText();

        FloatKind? suffix = null;
        if (text.EndsWith("f32", StringComparison.OrdinalIgnoreCase))
        {
            suffix = FloatKind.F32;
            text = text[..^3];
        }
        else if (text.EndsWith("f64", StringComparison.OrdinalIgnoreCase))
        {
            suffix = FloatKind.F64;
            text = text[..^3];
        }

        var value = double.Parse(text.Replace("_", ""));
        return new FloatLiteralNode(value, suffix) { Location = GetLocation(context) };
    }

    public override AstNode VisitString_literal(SolidLangParser.String_literalContext context)
    {
        var text = context.STRING_LITERAL().GetText();
        text = text[1..^1];
        return new StringLiteralNode(text) { Location = GetLocation(context) };
    }

    public override AstNode VisitChar_literal(SolidLangParser.Char_literalContext context)
    {
        var text = context.CHAR_LITERAL().GetText();
        text = text[1..^1];
        return new CharLiteralNode(text) { Location = GetLocation(context) };
    }

    public override AstNode VisitBool_literal(SolidLangParser.Bool_literalContext context)
    {
        var value = context.BOOL_LITERAL().GetText() == "true";
        return new BoolLiteralNode(value) { Location = GetLocation(context) };
    }

    public override AstNode VisitNull_literal(SolidLangParser.Null_literalContext context)
    {
        return new NullLiteralNode() { Location = GetLocation(context) };
    }

    public override AstNode VisitArray_literal(SolidLangParser.Array_literalContext context)
    {
        var type = context.type() != null ? (TypeNode)Visit(context.type()) : null;
        var elements = context.expr() != null
            ? context.expr().Select(e => (ExpressionNode)Visit(e)).ToList()
            : null;
        return new ArrayLiteralNode(type, elements) { Location = GetLocation(context) };
    }

    public override AstNode VisitStruct_literal(SolidLangParser.Struct_literalContext context)
    {
        var type = (NamedTypeNode)Visit(context.named_type());
        var fields = context.struct_literal_field() != null
            ? context.struct_literal_field().Select(f => new StructLiteralFieldNode(
                f.ID().GetText(),
                (ExpressionNode)Visit(f.expr())
            )).ToList()
            : null;
        return new StructLiteralNode(type, fields) { Location = GetLocation(context) };
    }

    public override AstNode VisitEnum_literal(SolidLangParser.Enum_literalContext context)
    {
        var type = (NamedTypeNode)Visit(context.named_type());
        var memberName = context.ID().GetText();
        return new EnumLiteralNode(type, memberName) { Location = GetLocation(context) };
    }

    public override AstNode VisitUnion_literal(SolidLangParser.Union_literalContext context)
    {
        var type = (NamedTypeNode)Visit(context.named_type());
        var fieldName = context.ID().GetText();
        var value = (ExpressionNode)Visit(context.expr());
        return new UnionLiteralNode(type, fieldName, value) { Location = GetLocation(context) };
    }

    public override AstNode VisitVariant_literal(SolidLangParser.Variant_literalContext context)
    {
        var type = (NamedTypeNode)Visit(context.named_type());
        var memberName = context.ID().GetText();
        var value = context.expr() != null ? (ExpressionNode)Visit(context.expr()) : null;
        return new VariantLiteralNode(type, memberName, value) { Location = GetLocation(context) };
    }

    public override AstNode VisitTuple_literal(SolidLangParser.Tuple_literalContext context)
    {
        var elements = context.tuple_literal_elem().Select(e => new TupleLiteralElementNode(
            (ExpressionNode)Visit(e.expr()),
            (TypeNode)Visit(e.type())
        )).ToList();
        return new TupleLiteralNode(elements) { Location = GetLocation(context) };
    }

    // Default: Visit children
    protected override AstNode AggregateResult(AstNode aggregate, AstNode nextResult)
    {
        return nextResult ?? aggregate;
    }
}
