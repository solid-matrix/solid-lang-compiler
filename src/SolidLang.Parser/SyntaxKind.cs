namespace SolidLang.Parser;

/// <summary>
/// Represents the kind of syntax element (token or node).
/// </summary>
public enum SyntaxKind
{
    // ========================================
    // Tokens (词法单元)
    // ========================================

    // None
    None = 0,

    // Keywords (关键字)
    NamespaceKeyword,
    UsingKeyword,
    FuncKeyword,
    InterfaceKeyword,
    StructKeyword,
    EnumKeyword,
    UnionKeyword,
    VariantKeyword,
    VarKeyword,
    ConstKeyword,
    StaticKeyword,
    IfKeyword,
    ElseKeyword,
    ForKeyword,
    SwitchKeyword,
    BreakKeyword,
    ContinueKeyword,
    ReturnKeyword,
    DeferKeyword,
    WhereKeyword,
    NullKeyword,
    TrueKeyword,
    FalseKeyword,
    CDeclKeyword,
    StdCallKeyword,

    // Primitive Type Keywords (原始类型关键字)
    U8Keyword,
    U16Keyword,
    U32Keyword,
    U64Keyword,
    USizeKeyword,
    I8Keyword,
    I16Keyword,
    I32Keyword,
    I64Keyword,
    ISizeKeyword,
    F32Keyword,
    F64Keyword,
    BoolKeyword,

    // Punctuation (标点符号)
    OpenBraceToken,        // {
    CloseBraceToken,       // }
    OpenBracketToken,      // [
    CloseBracketToken,     // ]
    OpenParenToken,        // (
    CloseParenToken,       // )
    DotToken,              // .
    CommaToken,            // ,
    ColonToken,            // :
    SemicolonToken,        // ;
    QuestionToken,         // ?
    AtToken,               // @

    // Two-Character Operators/Punctuation (双字符运算符/标点)
    ColonColonToken,       // ::
    EqualsArrowToken,      // =>
    MinusArrowToken,       // ->

    // Assignment Operators (赋值运算符)
    EqualsToken,           // =
    PlusEqualsToken,       // +=
    MinusEqualsToken,      // -=
    StarEqualsToken,       // *=
    SlashEqualsToken,      // /=
    PercentEqualsToken,    // %=
    AmpersandEqualsToken,  // &=
    PipeEqualsToken,       // |=
    CaretEqualsToken,      // ^=
    LessLessEqualsToken,   // <<=
    GreaterGreaterEqualsToken, // >>=

    // Comparison Operators (比较运算符)
    EqualsEqualsToken,     // ==
    BangEqualsToken,       // !=
    LessToken,             // <
    GreaterToken,          // >
    LessEqualsToken,       // <=
    GreaterEqualsToken,    // >=

    // Logical Operators (逻辑运算符)
    AmpersandAmpersandToken, // &&
    PipePipeToken,         // ||

    // Bitwise Operators (位运算符)
    AmpersandToken,        // &
    PipeToken,             // |
    CaretToken,            // ^
    LessLessToken,         // <<
    GreaterGreaterToken,   // >>

    // Arithmetic Operators (算术运算符)
    PlusToken,             // +
    MinusToken,            // -
    StarToken,             // *
    SlashToken,            // /
    PercentToken,          // %

    // Unary Operators (一元运算符)
    BangToken,             // !
    TildeToken,            // ~

    // Literals (字面量)
    IntegerLiteralToken,
    FloatLiteralToken,
    StringLiteralToken,
    CharLiteralToken,

    // Identifier (标识符)
    IdentifierToken,

    // Special (特殊)
    EndOfFileToken,
    BadToken,

    // ========================================
    // Trivia (空白和注释)
    // ========================================
    WhitespaceTrivia,
    SingleLineCommentTrivia,
    MultiLineCommentTrivia,
    DocumentationCommentTrivia,

    // ========================================
    // AST Nodes (语法节点)
    // ========================================

    // Program
    ProgramNode,

    // Declarations (声明节点)
    NamespaceDeclNode,
    UsingDeclNode,
    ConstDeclNode,
    StaticDeclNode,
    VarDeclNode,
    StructDeclNode,
    UnionDeclNode,
    EnumDeclNode,
    VariantDeclNode,
    InterfaceDeclNode,
    FunctionDeclNode,

    // Declaration Components
    NamespacePathNode,
    NamespacePrefixNode,
    GenericParamsNode,
    GenericParamNode,
    WhereClausesNode,
    WhereClauseNode,
    CtAnnotatesNode,
    CtAnnotateNode,
    CtAnnotateArgsNode,
    CtAnnotateArgNode,
    CtOperatorExprNode,
    CtOperatorArgsNode,
    CtOperatorArgNode,
    CallConventionNode,
    FuncParametersNode,
    FuncParameterNode,

    // Struct/Union/Enum/Variant Fields
    StructFieldsNode,
    StructFieldNode,
    UnionFieldsNode,
    UnionFieldNode,
    EnumFieldsNode,
    EnumFieldNode,
    VariantFieldsNode,
    VariantFieldNode,
    InterfaceFieldsNode,
    InterfaceFieldNode,

    // Statements (语句节点)
    EmptyStmtNode,
    BodyStmtNode,
    AssignStmtNode,
    ExprStmtNode,
    DeferStmtNode,
    IfStmtNode,
    ForStmtNode,
    SwitchStmtNode,
    SwitchArmNode,
    SwitchPatternNode,
    SwitchPatternBindingNode,
    BreakStmtNode,
    ContinueStmtNode,
    ReturnStmtNode,

    // For Statement Components
    ForInfiniteNode,
    ForCondNode,
    ForCStyleNode,
    ForInitNode,
    ForVarDeclNode,
    ForAssignNode,

    // Expressions (表达式节点)
    ConditionalExprNode,
    OrExprNode,
    AndExprNode,
    BitOrExprNode,
    BitXorExprNode,
    BitAndExprNode,
    EqualityExprNode,
    ComparisonExprNode,
    ShiftExprNode,
    AddExprNode,
    MulExprNode,
    UnaryExprNode,
    PostfixExprNode,
    PrimaryExprNode,

    // Postfix Components
    PostfixSuffixNode,
    DotAccessNode,
    IndexAccessNode,
    CallExprNode,
    ScopeAccessNode,
    CallArgsNode,
    CallArgNode,

    // Types (类型节点)
    TypeNode,
    PrimitiveTypeNode,
    IntegerTypeNode,
    FloatTypeNode,
    BoolTypeNode,
    ArrayTypeNode,
    PointerTypeNode,
    FuncPointerTypeNode,
    NamedTypeNode,
    TypeArgumentListNode,

    // Literals (字面量节点)
    IntegerLiteralNode,
    FloatLiteralNode,
    StringLiteralNode,
    CharLiteralNode,
    BoolLiteralNode,
    NullLiteralNode,
    ArrayLiteralNode,
    StructLiteralNode,
    StructLiteralFieldNode,
    UnionLiteralNode,
    EnumLiteralNode,
    VariantLiteralNode,

    // Error Recovery
    BadDeclNode,
    BadStmtNode,
    BadExprNode,
    BadTypeNode,
}
