namespace SolidLang.SemanticAnalyzer;

public enum ScopeKind
{
    Global,
    Namespace,
    Type,
    Function,
    Block,
    SwitchArm,
}
