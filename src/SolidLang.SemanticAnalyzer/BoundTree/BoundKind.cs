namespace SolidLang.SemanticAnalyzer;

public enum BoundKind
{
    // Root
    Program,

    // Declarations
    FunctionDecl,
    StructDecl,
    EnumDecl,
    UnionDecl,
    VariantDecl,
    InterfaceDecl,
    VariableDecl,
    FieldDecl,

    // Statements
    Block,
    ExprStmt,
    AssignStmt,
    VariableStmt,
    IfStmt,
    WhileStmt,
    ForCStyleStmt,
    SwitchStmt,
    SwitchArm,
    ReturnStmt,
    BreakStmt,
    ContinueStmt,
    DeferStmt,

    // Expressions
    LiteralExpr,
    VarExpr,
    CallExpr,
    BinaryExpr,
    UnaryExpr,
    ConditionalExpr,
    MemberAccessExpr,
    IndexAccessExpr,
    StructLiteralExpr,
    UnionLiteralExpr,
    EnumLiteralExpr,
    VariantLiteralExpr,
    SwitchPattern,
}
