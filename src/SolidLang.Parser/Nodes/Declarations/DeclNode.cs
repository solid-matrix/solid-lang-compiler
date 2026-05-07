using SolidLang.Parser.Nodes.Statements;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Base class for all declaration nodes.
/// Declarations can also be statements (e.g., local var/const/static declarations).
/// </summary>
public abstract class DeclNode : StmtNode
{
}
