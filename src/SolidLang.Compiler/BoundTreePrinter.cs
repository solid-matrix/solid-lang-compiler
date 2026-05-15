using System.Text;
using SolidLang.Parser;
using SolidLang.SemanticAnalyzer;

/// <summary>
/// Prints a BoundProgram as a tree for debugging and snapshot purposes.
/// </summary>
internal static class BoundTreePrinter
{
    public static string Print(BoundProgram program)
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        PrintNode(writer, "Program", program.Declarations, "");
        return sb.ToString();
    }

    private static void PrintNode(TextWriter writer, string label, object? node, string indent, bool isLast = true)
    {
        var marker = isLast ? "└──" : "├──";
        writer.Write(indent);
        writer.Write(marker);
        writer.WriteLine(label);

        var children = GetChildren(node);
        for (int i = 0; i < children.Count; i++)
        {
            var (childLabel, childNode) = children[i];
            var childIsLast = i == children.Count - 1;
            var childIndent = indent + (isLast ? "   " : "│  ");
            PrintNode(writer, childLabel, childNode, childIndent, childIsLast);
        }
    }

    private static void PrintNode(TextWriter writer, string label, IReadOnlyList<BoundNode> nodes, string indent)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var isLast = i == nodes.Count - 1;
            PrintNode(writer, NodeLabel(node), node, indent, isLast);
        }
    }

    private static string NodeLabel(BoundNode node) => node switch
    {
        BoundFunctionDecl f => $"FunctionDecl [{f.Symbol.Name}] -> {TypeName(f.ReturnType)}",
        BoundStructDecl s => $"StructDecl [{s.Symbol.Name}]",
        BoundEnumDecl e => $"EnumDecl [{e.Symbol.Name}]",
        BoundUnionDecl u => $"UnionDecl [{u.Symbol.Name}]",
        BoundVariantDecl v => $"VariantDecl [{v.Symbol.Name}]",
        BoundInterfaceDecl i => $"InterfaceDecl [{i.Symbol.Name}]",
        BoundVariableDecl v => VarLabel(v),
        BoundFieldDecl f => $"FieldDecl [{f.Symbol.Name}] : {TypeName(f.FieldType)}",
        BoundBlock => "Block",
        BoundVariableStmt v => VarLabel(v.Declaration),
        BoundExprStmt => "ExprStmt",
        BoundAssignStmt a => $"AssignStmt [{SyntaxFacts.GetTokenText(a.Operator)}]",
        BoundIfStmt => "IfStmt",
        BoundWhileStmt w => w.Condition == null ? "WhileStmt [infinite]" : "WhileStmt",
        BoundForCStyleStmt => "ForCStyleStmt",
        BoundSwitchStmt => "SwitchStmt",
        BoundSwitchArm a => a.IsElse ? "SwitchArm [else]" : "SwitchArm",
        BoundReturnStmt r => r.Expression == null ? "ReturnStmt" : "ReturnStmt",
        BoundBreakStmt => "BreakStmt",
        BoundContinueStmt => "ContinueStmt",
        BoundDeferStmt => "DeferStmt",
        BoundLiteralExpr l => $"Literal [{l.Value}] : {TypeName(l.Type)}",
        BoundVarExpr v => $"VarExpr [{(v.Symbol != null ? v.Symbol.Name : "?")}]",
        BoundCallExpr c => $"CallExpr [{(c.Function != null ? c.Function.Name : "?")}]",
        BoundBinaryExpr b => $"BinaryExpr [{SyntaxFacts.GetTokenText(b.Operator)}]",
        BoundUnaryExpr u => $"UnaryExpr [{SyntaxFacts.GetTokenText(u.Operator)}]",
        BoundConditionalExpr => "ConditionalExpr",
        BoundMemberAccessExpr m => $"MemberAccessExpr [{(m.Member != null ? m.Member.Name : "?")}]",
        BoundIndexAccessExpr => "IndexAccessExpr",
        BoundStructLiteralExpr s => $"StructLiteral [{s.StructType.Name}]",
        BoundUnionLiteralExpr u => $"UnionLiteral [{u.UnionType.Name}::{u.Member.Name}]",
        BoundEnumLiteralExpr e => $"EnumLiteral [{e.EnumType.Name}::{e.Member.Name}]",
        BoundVariantLiteralExpr v => $"VariantLiteral [{v.VariantType.Name}::{v.Member.Name}]",
        BoundSwitchPattern p => $"SwitchPattern [{p.PatternKind}]",
        BoundBuiltinCallExpr b => $"BuiltinCall [{b.MethodKind}] -> {TypeName(b.Type)}",
        BoundCtOperatorExpr c => $"CtOperator [{c.OperatorKind}] -> {TypeName(c.Type)}",
        _ => node.Kind.ToString()
    };

    private static string VarLabel(BoundVariableDecl v) =>
        $"VariableDecl [{v.Symbol.Name}] : {TypeName(v.DeclaredType)}";

    private static string TypeName(SolidType? type) => type?.DisplayName ?? "?";

    private static List<(string, object?)> GetChildren(object? node)
    {
        if (node is IReadOnlyList<BoundNode> list)
        {
            return list.Select(n => ((string, object?))(NodeLabel(n), n)).ToList();
        }

        return node switch
        {
            BoundFunctionDecl f => MakeChildren(
                ("params", f.Parameters),
                ("body", f.Body is not null ? new[] { f.Body } : null)
            ),
            BoundStructDecl s => MakeChildren(("fields", s.Fields)),
            BoundEnumDecl e => MakeChildren(("fields", e.Fields)),
            BoundUnionDecl u => MakeChildren(("fields", u.Fields)),
            BoundVariantDecl v => MakeChildren(("fields", v.Fields)),
            BoundInterfaceDecl i => MakeChildren(("methods", i.Methods)),
            BoundVariableDecl v => v.Initializer != null
                ? MakeChildren(("init", new BoundNode[] { v.Initializer }))
                : new(),
            BoundBlock b => MakeChildren(("stmts", b.Statements)),
            BoundVariableStmt v => MakeChildren(("decl", new[] { v.Declaration })),
            BoundExprStmt e => MakeChildren(("expr", new[] { e.Expression })),
            BoundAssignStmt a => MakeChildren(
                ("target", new[] { a.Target }),
                ("value", new[] { a.Value })
            ),
            BoundIfStmt i => MakeChildren(
                ("cond", new[] { i.Condition }),
                ("then", new[] { i.ThenBody }),
                ("else", i.ElseBody is not null ? new[] { i.ElseBody } : null)
            ),
            BoundWhileStmt w => MakeChildren(
                ("cond", w.Condition is not null ? new[] { w.Condition } : null),
                ("body", new[] { w.Body })
            ),
            BoundForCStyleStmt f => MakeChildren(
                ("initVar", f.InitVariable is not null ? new[] { f.InitVariable } : null),
                ("initExpr", f.InitExpression is not null ? new[] { f.InitExpression } : null),
                ("cond", f.Condition is not null ? new[] { f.Condition } : null),
                ("update", f.Update is not null ? new[] { f.Update } : null),
                ("body", new[] { f.Body })
            ),
            BoundSwitchStmt s => MakeChildren(
                ("expr", new[] { s.Expression }),
                ("arms", s.Arms)
            ),
            BoundSwitchArm a => MakeChildren(
                ("patterns", a.Patterns),
                ("body", new[] { a.Body })
            ),
            BoundReturnStmt r => r.Expression != null
                ? MakeChildren(("expr", new[] { r.Expression }))
                : new(),
            BoundDeferStmt d => MakeChildren(("deferred", new[] { d.DeferredStatement })),
            BoundCallExpr c => MakeChildren(("args", c.Arguments)),
            BoundBinaryExpr b => MakeChildren(
                ("left", new[] { b.Left }),
                ("right", new[] { b.Right })
            ),
            BoundUnaryExpr u => MakeChildren(("operand", new[] { u.Operand })),
            BoundConditionalExpr c => MakeChildren(
                ("cond", new[] { c.Condition }),
                ("then", new[] { c.ThenExpr }),
                ("else", new[] { c.ElseExpr })
            ),
            BoundMemberAccessExpr m => MakeChildren(("receiver", new[] { m.Receiver })),
            BoundIndexAccessExpr i => MakeChildren(
                ("receiver", new[] { i.Receiver }),
                ("index", new[] { i.Index })
            ),
            BoundStructLiteralExpr s => MakeChildren(
                s.Fields.Select(f => ($"field [{f.Field.Name}]", (object?)new[] { f.Value })).ToArray()
            ),
            BoundUnionLiteralExpr u => u.Value != null
                ? MakeChildren(("value", new[] { u.Value }))
                : new(),
            BoundVariantLiteralExpr v => v.Value != null
                ? MakeChildren(("value", new[] { v.Value }))
                : new(),
            BoundBuiltinCallExpr b => MakeChildren(("receiver", new[] { b.Receiver })),
            _ => new()
        };
    }

    private static List<(string, object?)> MakeChildren(params (string label, IReadOnlyList<BoundNode>? nodes)[] groups)
    {
        var result = new List<(string, object?)>();
        foreach (var (label, nodes) in groups)
        {
            if (nodes != null && nodes.Count > 0)
                result.Add((label, nodes));
        }
        return result;
    }

    private static List<(string, object?)> MakeChildren(params (string, object?)[] items)
    {
        var result = new List<(string, object?)>();
        foreach (var (label, node) in items)
        {
            if (node != null)
                result.Add((label, node));
        }
        return result;
    }
}
