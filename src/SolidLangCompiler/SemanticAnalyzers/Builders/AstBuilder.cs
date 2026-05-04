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
        return new ArrayTypeNode(elementType, 0) { Location = GetLocation(context) };
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

        return new VarDeclarationNode(null, name, type, initializer) { Location = GetLocation(context) };
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
        var stmts = context.stmt().Select(s => (StatementNode)Visit(s)).ToList();
        return new BlockStatementNode(stmts) { Location = GetLocation(context) };
    }

    public override AstNode VisitAssign_stmt(SolidLangParser.Assign_stmtContext context)
    {
        var target = (ExpressionNode)Visit(context.expr(0));
        var value = (ExpressionNode)Visit(context.expr(1));

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
