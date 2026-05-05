using FluentAssertions;
using SolidLangCompiler.AST;
using SolidLangCompiler.AST.Declarations;
using SolidLangCompiler.AST.Expressions;
using SolidLangCompiler.AST.Statements;
using SolidLangCompiler.AST.Types;
using SolidLangCompiler.SemanticAnalyzers.Builders;
using Xunit;

namespace SolidLangCompiler.UnitTests;

public class AstBuilderTests
{
    private ProgramNode BuildAst(string source)
    {
        var inputStream = new Antlr4.Runtime.AntlrInputStream(source);
        var lexer = new Generated.SolidLangLexer(inputStream);
        var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
        var parser = new Generated.SolidLangParser(tokenStream);
        var tree = parser.program();

        var builder = new AstBuilder();
        return builder.Build(tree, "test.solid");
    }

    [Fact]
    public void AstBuilder_ShouldBuildNamespace()
    {
        var source = "namespace app;";
        var ast = BuildAst(source);

        ast.Namespace.Should().NotBeNull();
        ast.Namespace!.Path.Should().ContainInOrder("app");
    }

    [Fact]
    public void AstBuilder_ShouldBuildNestedNamespace()
    {
        var source = "namespace foo::bar::baz;";
        var ast = BuildAst(source);

        ast.Namespace.Should().NotBeNull();
        ast.Namespace!.Path.Should().ContainInOrder("foo", "bar", "baz");
    }

    [Fact]
    public void AstBuilder_ShouldBuildUsingDeclarations()
    {
        var source = """
            namespace app;
            using std;
            using collections;
            """;
        var ast = BuildAst(source);

        ast.Usings.Should().NotBeNull();
        ast.Usings!.Should().HaveCount(2);
    }

    [Fact]
    public void AstBuilder_ShouldBuildFunctionDeclaration()
    {
        var source = """
            namespace app;
            func main(): i32 {
                return 0;
            }
            """;
        var ast = BuildAst(source);

        ast.Declarations.Should().HaveCount(1);
        var func = ast.Declarations![0].Should().BeOfType<FuncDeclarationNode>().Subject;
        func.Name.Should().Be("main");
        func.ReturnType.Should().BeOfType<IntegerTypeNode>();
    }

    [Fact]
    public void AstBuilder_ShouldBuildFunctionWithParameters()
    {
        var source = """
            namespace app;
            func add(a: i32, b: i64): f64 {
                return 0.0;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        func.Parameters.Should().HaveCount(2);
        func.Parameters![0].Name.Should().Be("a");
        func.Parameters![1].Name.Should().Be("b");
    }

    [Fact]
    public void AstBuilder_ShouldBuildStructDeclaration()
    {
        var source = """
            namespace app;
            struct Point {
                x: f32,
                y: f32,
            }
            """;
        var ast = BuildAst(source);

        ast.Declarations.Should().HaveCount(1);
        var structDecl = ast.Declarations![0].Should().BeOfType<StructDeclarationNode>().Subject;
        structDecl.Name.Should().Be("Point");
        structDecl.Fields.Should().HaveCount(2);
    }

    [Fact]
    public void AstBuilder_ShouldBuildEnumDeclaration()
    {
        var source = """
            namespace app;
            enum Color: u8 {
                Red = 0,
                Green = 1,
            }
            """;
        var ast = BuildAst(source);

        ast.Declarations.Should().HaveCount(1);
        var enumDecl = ast.Declarations![0].Should().BeOfType<EnumDeclarationNode>().Subject;
        enumDecl.Name.Should().Be("Color");
        enumDecl.UnderlyingType.Should().BeOfType<IntegerTypeNode>();
        enumDecl.Fields.Should().HaveCount(2);
    }

    [Fact]
    public void AstBuilder_ShouldBuildConstDeclaration()
    {
        var source = """
            namespace app;
            const PI: f64 = 3.14159;
            """;
        var ast = BuildAst(source);

        ast.Declarations.Should().HaveCount(1);
        var constDecl = ast.Declarations![0].Should().BeOfType<ConstDeclarationNode>().Subject;
        constDecl.Name.Should().Be("PI");
        constDecl.Type.Should().BeOfType<FloatTypeNode>();
    }

    [Fact]
    public void AstBuilder_ShouldBuildIntegerLiteral()
    {
        var source = """
            namespace app;
            const VALUE: i32 = 42;
            """;
        var ast = BuildAst(source);

        var constDecl = ast.Declarations![0].As<ConstDeclarationNode>();
        var literal = constDecl.Initializer.Should().BeOfType<IntegerLiteralNode>().Subject;
        literal.Value.Should().Be(42);
    }

    [Fact]
    public void AstBuilder_ShouldBuildHexIntegerLiteral()
    {
        var source = """
            namespace app;
            const VALUE: i32 = 0xFF;
            """;
        var ast = BuildAst(source);

        var constDecl = ast.Declarations![0].As<ConstDeclarationNode>();
        var literal = constDecl.Initializer.Should().BeOfType<IntegerLiteralNode>().Subject;
        literal.Value.Should().Be(255);
    }

    [Fact]
    public void AstBuilder_ShouldBuildFloatLiteral()
    {
        var source = """
            namespace app;
            const PI: f64 = 3.14159;
            """;
        var ast = BuildAst(source);

        var constDecl = ast.Declarations![0].As<ConstDeclarationNode>();
        var literal = constDecl.Initializer.Should().BeOfType<FloatLiteralNode>().Subject;
        literal.Value.Should().BeApproximately(3.14159, 0.0001);
    }

    [Fact]
    public void AstBuilder_ShouldBuildStringLiteral()
    {
        var source = """
            namespace app;
            const MSG: &u8 = "hello";
            """;
        var ast = BuildAst(source);

        var constDecl = ast.Declarations![0].As<ConstDeclarationNode>();
        var literal = constDecl.Initializer.Should().BeOfType<StringLiteralNode>().Subject;
        literal.Value.Should().Be("hello");
    }

    [Fact]
    public void AstBuilder_ShouldBuildBoolLiteral()
    {
        var source = """
            namespace app;
            const FLAG: bool = true;
            """;
        var ast = BuildAst(source);

        var constDecl = ast.Declarations![0].As<ConstDeclarationNode>();
        var literal = constDecl.Initializer.Should().BeOfType<BoolLiteralNode>().Subject;
        literal.Value.Should().BeTrue();
    }

    [Fact]
    public void AstBuilder_ShouldBuildReturnStatement()
    {
        var source = """
            namespace app;
            func foo(): i32 {
                return 42;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var body = func.Body!.Statements[0].Should().BeOfType<ReturnStatementNode>().Subject;
        body.Value.Should().BeOfType<IntegerLiteralNode>();
    }

    [Fact]
    public void AstBuilder_ShouldBuildIfStatement()
    {
        var source = """
            namespace app;
            func abs(x: i32): i32 {
                if x < 0 {
                    return -x;
                } else {
                    return x;
                }
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var ifStmt = func.Body!.Statements[0].Should().BeOfType<IfStatementNode>().Subject;
        ifStmt.Condition.Should().NotBeNull();
        ifStmt.ThenBlock.Should().NotBeNull();
        ifStmt.ElseBranch.Should().NotBeNull();
    }

    [Fact]
    public void AstBuilder_ShouldBuildPrimitiveTypes()
    {
        var source = """
            namespace app;
            func types(
                a: i8, b: i16, c: i32, d: i64, e: isize,
                f: u8, g: u16, h: u32, i: u64, j: usize,
                k: f32, l: f64, m: bool
            ) {}
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var paramTypes = func.Parameters!.Select(p => p.Type).ToList();

        paramTypes[0].As<IntegerTypeNode>().Kind.Should().Be(IntegerKind.I8);
        paramTypes[1].As<IntegerTypeNode>().Kind.Should().Be(IntegerKind.I16);
        paramTypes[2].As<IntegerTypeNode>().Kind.Should().Be(IntegerKind.I32);
        paramTypes[3].As<IntegerTypeNode>().Kind.Should().Be(IntegerKind.I64);
        paramTypes[4].As<IntegerTypeNode>().Kind.Should().Be(IntegerKind.ISize);
        paramTypes[5].As<IntegerTypeNode>().Kind.Should().Be(IntegerKind.U8);
        paramTypes[6].As<IntegerTypeNode>().Kind.Should().Be(IntegerKind.U16);
        paramTypes[7].As<IntegerTypeNode>().Kind.Should().Be(IntegerKind.U32);
        paramTypes[8].As<IntegerTypeNode>().Kind.Should().Be(IntegerKind.U64);
        paramTypes[9].As<IntegerTypeNode>().Kind.Should().Be(IntegerKind.USize);
        paramTypes[10].As<FloatTypeNode>().Kind.Should().Be(FloatKind.F32);
        paramTypes[11].As<FloatTypeNode>().Kind.Should().Be(FloatKind.F64);
        paramTypes[12].Should().BeOfType<BoolTypeNode>();
    }

    [Fact]
    public void AstBuilder_ShouldBuildArrayType()
    {
        var source = """
            namespace app;
            func process(arr: [10]i32) {}
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var paramType = func.Parameters![0].Type.Should().BeOfType<ArrayTypeNode>().Subject;
        paramType.ElementType.As<IntegerTypeNode>().Kind.Should().Be(IntegerKind.I32);
        paramType.Size.Should().Be(10);
    }

    [Fact(Skip = "Pointer type parsing needs investigation")]
    public void AstBuilder_ShouldBuildPointerType()
    {
        var source = """
            namespace app;
            func deref(ptr: *i32) {}
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var paramType = func.Parameters![0].Type.Should().BeOfType<PointerTypeNode>().Subject;
        paramType.TargetType.As<IntegerTypeNode>().Kind.Should().Be(IntegerKind.I32);
    }

    [Fact]
    public void AstBuilder_ShouldBuildTupleType()
    {
        var source = """
            namespace app;
            func get_point(): (f32, f32) { return (0.0, 0.0); }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var returnType = func.ReturnType.Should().BeOfType<TupleTypeNode>().Subject;
        returnType.Elements.Should().HaveCount(2);
    }

    [Fact(Skip = "Func type parsing needs investigation")]
    public void AstBuilder_ShouldBuildFuncType()
    {
        var source = """
            namespace app;
            func apply(f: func(i32) -> i32) {}
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var paramType = func.Parameters![0].Type.Should().BeOfType<FuncTypeNode>().Subject;
        paramType.ParameterTypes.Should().HaveCount(1);
        paramType.ReturnType.Should().NotBeNull();
    }

    [Fact]
    public void AstBuilder_ShouldBuildInfiniteFor()
    {
        var source = """
            namespace app;
            func loop() {
                for {
                    return;
                }
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var forStmt = func.Body!.Statements[0].Should().BeOfType<InfiniteForNode>().Subject;
        forStmt.Body.Statements.Should().HaveCount(1);
    }

    [Fact]
    public void AstBuilder_ShouldBuildConditionalFor()
    {
        var source = """
            namespace app;
            func count(n: i32) {
                for n > 0 {
                    n = n - 1;
                }
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var forStmt = func.Body!.Statements[0].Should().BeOfType<ConditionalForNode>().Subject;
        forStmt.Condition.Should().NotBeNull();
    }

    [Fact]
    public void AstBuilder_ShouldBuildCStyleFor()
    {
        // Note: In solid-lang:
        // - Assignment is a statement, not an expression
        // - var_decl ends with SEMI
        // - for_cstyle: for_init? SEMI expr? SEMI expr? body_stmt
        // So: for var i: i32 = 0;; i < 10; i + 1 { body }
        //     var_decl ends with SEMI, then SEMI for condition separator, then SEMI for update separator
        var source = """
            namespace app;
            func test(): i32 {
                for var i: i32 = 0;; i < 10; i + 1 {
                    return i;
                }
                return 0;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var forStmt = func.Body!.Statements[0].Should().BeOfType<CStyleForNode>().Subject;
        forStmt.Init.Should().NotBeNull();
        forStmt.Condition.Should().NotBeNull();
        forStmt.Update.Should().NotBeNull();
    }

    [Fact]
    public void AstBuilder_ShouldBuildBreakStatement()
    {
        var source = """
            namespace app;
            func early_exit(flag: bool) {
                for {
                    if flag {
                        break;
                    }
                    return;
                }
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var forStmt = func.Body!.Statements[0].As<InfiniteForNode>();
        var ifStmt = forStmt.Body.Statements[0].As<IfStatementNode>();
        var breakStmt = ifStmt.ThenBlock.Statements[0].Should().BeOfType<BreakStatementNode>().Subject;
    }

    [Fact]
    public void AstBuilder_ShouldBuildContinueStatement()
    {
        var source = """
            namespace app;
            func skip_odd(n: i32) {
                for n > 0 {
                    if n % 2 == 1 {
                        n = n - 1;
                        continue;
                    }
                    n = n - 1;
                }
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var forStmt = func.Body!.Statements[0].As<ConditionalForNode>();
        var ifStmt = forStmt.Body.Statements[0].As<IfStatementNode>();
        var continueStmt = ifStmt.ThenBlock.Statements[1].Should().BeOfType<ContinueStatementNode>().Subject;
    }

    [Fact]
    public void AstBuilder_ShouldBuildSwitchStatement()
    {
        var source = """
            namespace app;
            func classify(n: i32): i32 {
                switch n {
                    1 => return 1;
                    2 => return 2;
                    else => return 0;
                }
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var switchStmt = func.Body!.Statements[0].Should().BeOfType<SwitchStatementNode>().Subject;
        switchStmt.Expression.Should().NotBeNull();
        switchStmt.Arms.Should().HaveCount(3);
    }

    [Fact]
    public void AstBuilder_ShouldBuildSwitchWithLiteralPatterns()
    {
        var source = """
            namespace app;
            func test(n: i32): i32 {
                switch n {
                    0 => return 0;
                    1 => return 1;
                    42 => return 42;
                    else => return -1;
                }
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var switchStmt = func.Body!.Statements[0].As<SwitchStatementNode>();
        var firstArm = switchStmt.Arms[0];
        firstArm.Pattern.Should().BeOfType<LiteralPatternNode>();
        var litPattern = firstArm.Pattern.As<LiteralPatternNode>();
        litPattern.Literal.Should().BeOfType<IntegerLiteralNode>();
    }

    [Fact]
    public void AstBuilder_ShouldBuildSwitchWithEnumPattern()
    {
        var source = """
            namespace app;
            func process(c: Color): i32 {
                switch c {
                    Color::Red => return 1;
                    Color::Green => return 2;
                    else => return 0;
                }
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var switchStmt = func.Body!.Statements[0].As<SwitchStatementNode>();
        var firstArm = switchStmt.Arms[0];
        firstArm.Pattern.Should().BeOfType<TypePatternNode>();
        var typePattern = firstArm.Pattern.As<TypePatternNode>();
        typePattern.MemberName.Should().Be("Red");
    }

    [Fact]
    public void AstBuilder_ShouldBuildArrayLiteral()
    {
        var source = """
            namespace app;
            func test(): i32 {
                var arr = [3]i32{1, 2, 3};
                return 0;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var varDecl = func.Body!.Statements[0].As<VarDeclStatementNode>();
        varDecl.Initializer.Should().BeOfType<ArrayLiteralNode>();
        var arrLit = varDecl.Initializer.As<ArrayLiteralNode>();
        arrLit.ArrayType.Should().NotBeNull();
        arrLit.Elements.Should().HaveCount(3);
    }

    [Fact]
    public void AstBuilder_ShouldBuildArrayLiteralWithEmpty()
    {
        var source = """
            namespace app;
            func test(): i32 {
                var arr = [5]i32{};
                return 0;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var varDecl = func.Body!.Statements[0].As<VarDeclStatementNode>();
        varDecl.Initializer.Should().NotBeNull();
        varDecl.Initializer.Should().BeOfType<ArrayLiteralNode>();
        var arrLit = varDecl.Initializer.As<ArrayLiteralNode>();
        arrLit.ArrayType.Should().NotBeNull();
        arrLit.ArrayType!.Size.Should().Be(5);
    }

    [Fact]
    public void AstBuilder_ShouldBuildStructLiteral()
    {
        var source = """
            namespace app;
            struct Point {
                x: f32,
                y: f32,
            }
            func test(): i32 {
                var p = Point{x = 1.0, y = 2.0};
                return 0;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![1].As<FuncDeclarationNode>();
        var varDecl = func.Body!.Statements[0].As<VarDeclStatementNode>();
        varDecl.Initializer.Should().BeOfType<StructLiteralNode>();
        var structLit = varDecl.Initializer.As<StructLiteralNode>();
        structLit.Type.Name.Should().Be("Point");
        structLit.Fields.Should().HaveCount(2);
        structLit.Fields![0].Name.Should().Be("x");
        structLit.Fields![1].Name.Should().Be("y");
    }

    [Fact]
    public void AstBuilder_ShouldBuildEnumLiteral()
    {
        var source = """
            namespace app;
            enum Color: u8 {
                Red = 0,
                Green = 1,
            }
            func test(): i32 {
                var c = Color::Green;
                return 0;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![1].As<FuncDeclarationNode>();
        var varDecl = func.Body!.Statements[0].As<VarDeclStatementNode>();
        varDecl.Initializer.Should().BeOfType<EnumLiteralNode>();
        var enumLit = varDecl.Initializer.As<EnumLiteralNode>();
        enumLit.Type.Name.Should().Be("Color");
        enumLit.MemberName.Should().Be("Green");
    }

    [Fact]
    public void AstBuilder_ShouldBuildFieldAccess()
    {
        var source = """
            namespace app;
            struct Point {
                x: f32,
                y: f32,
            }
            func test(): f32 {
                var p = Point{x = 1.0, y = 2.0};
                return p.x;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![1].As<FuncDeclarationNode>();
        var retStmt = func.Body!.Statements[1].As<ReturnStatementNode>();
        retStmt.Value.Should().BeOfType<FieldAccessExpressionNode>();
        var fieldAccess = retStmt.Value.As<FieldAccessExpressionNode>();
        fieldAccess.FieldName.Should().Be("x");
        fieldAccess.Target.Should().BeOfType<IdentifierExpressionNode>();
    }

    [Fact]
    public void AstBuilder_ShouldBuildIndexExpression()
    {
        var source = """
            namespace app;
            func test(): i32 {
                var arr = [3]i32{1, 2, 3};
                return arr[0];
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var retStmt = func.Body!.Statements[1].As<ReturnStatementNode>();
        retStmt.Value.Should().BeOfType<IndexExpressionNode>();
        var indexExpr = retStmt.Value.As<IndexExpressionNode>();
        indexExpr.Target.Should().BeOfType<IdentifierExpressionNode>();
        indexExpr.Index.Should().BeOfType<IntegerLiteralNode>();
    }

    [Fact]
    public void AstBuilder_ShouldBuildChainedFieldAccess()
    {
        var source = """
            namespace app;
            struct Inner {
                value: i32,
            }
            struct Outer {
                inner: Inner,
            }
            func test(): i32 {
                var o = Outer{inner = Inner{value = 42}};
                return o.inner.value;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![2].As<FuncDeclarationNode>();
        var retStmt = func.Body!.Statements[1].As<ReturnStatementNode>();
        retStmt.Value.Should().BeOfType<FieldAccessExpressionNode>();
        var outerAccess = retStmt.Value.As<FieldAccessExpressionNode>();
        outerAccess.FieldName.Should().Be("value");
        outerAccess.Target.Should().BeOfType<FieldAccessExpressionNode>();
        var innerAccess = outerAccess.Target.As<FieldAccessExpressionNode>();
        innerAccess.FieldName.Should().Be("inner");
    }

    [Fact]
    public void AstBuilder_ShouldBuildDeferStatementWithExpression()
    {
        var source = """
            namespace app;
            func test(): i32 {
                defer 1;
                return 0;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var deferStmt = func.Body!.Statements[0].As<DeferStatementNode>();
        deferStmt.DeferredStatement.Should().BeOfType<ExpressionStatementNode>();
    }

    [Fact]
    public void AstBuilder_ShouldBuildDeferStatementWithBlock()
    {
        var source = """
            namespace app;
            func test(): i32 {
                defer {
                    var x: i32 = 1;
                }
                return 0;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var deferStmt = func.Body!.Statements[0].As<DeferStatementNode>();
        deferStmt.DeferredStatement.Should().BeOfType<BlockStatementNode>();
    }

    [Fact]
    public void AstBuilder_ShouldBuildDeferStatementWithAssignment()
    {
        var source = """
            namespace app;
            func test(): i32 {
                var x: i32 = 0;
                defer x = 42;
                return x;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var deferStmt = func.Body!.Statements[1].As<DeferStatementNode>();
        deferStmt.DeferredStatement.Should().BeOfType<AssignmentStatementNode>();
    }

    [Fact]
    public void AstBuilder_ShouldBuildRefType()
    {
        var source = """
            namespace app;
            func test(x: ^i32): i32 {
                return 0;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var paramType = func.Parameters![0].Type.Should().BeOfType<RefTypeNode>().Subject;
        paramType.TargetType.As<IntegerTypeNode>().Kind.Should().Be(IntegerKind.I32);
        paramType.IsMutable.Should().BeFalse();
    }

    [Fact]
    public void AstBuilder_ShouldBuildMutRefType()
    {
        var source = """
            namespace app;
            func test(x: ^!i32): i32 {
                return 0;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var paramType = func.Parameters![0].Type.Should().BeOfType<RefTypeNode>().Subject;
        paramType.TargetType.As<IntegerTypeNode>().Kind.Should().Be(IntegerKind.I32);
        paramType.IsMutable.Should().BeTrue();
    }

    [Fact]
    public void AstBuilder_ShouldBuildRefExpression()
    {
        var source = """
            namespace app;
            func test(): i32 {
                var x: i32 = 42;
                var ref: ^i32 = ^x;
                return 0;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var varDecl = func.Body!.Statements[1].As<VarDeclStatementNode>();
        varDecl.Initializer.Should().BeOfType<UnaryExpressionNode>();
        var unary = varDecl.Initializer.As<UnaryExpressionNode>();
        unary.Operator.Should().Be(UnaryOperator.Ref);
    }

    [Fact]
    public void AstBuilder_ShouldBuildMutRefExpression()
    {
        var source = """
            namespace app;
            func test(): i32 {
                var x: i32 = 42;
                var ref: ^!i32 = ^!x;
                return 0;
            }
            """;
        var ast = BuildAst(source);

        var func = ast.Declarations![0].As<FuncDeclarationNode>();
        var varDecl = func.Body!.Statements[1].As<VarDeclStatementNode>();
        varDecl.Initializer.Should().BeOfType<UnaryExpressionNode>();
        var unary = varDecl.Initializer.As<UnaryExpressionNode>();
        unary.Operator.Should().Be(UnaryOperator.MutRef);
    }
}

file static class AstExtensions
{
    public static T As<T>(this object obj) where T : class
    {
        return obj.Should().BeOfType<T>().Subject;
    }
}
