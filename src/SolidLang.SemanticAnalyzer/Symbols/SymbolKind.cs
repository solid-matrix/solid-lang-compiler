namespace SolidLang.SemanticAnalyzer;

public enum SymbolKind
{
    // Types
    Struct,
    Enum,
    Union,
    Variant,
    Interface,
    Primitive,

    // Variables
    VarVariable,
    ConstVariable,
    StaticVariable,
    Parameter,
    ForLoopVariable,

    // Functions
    Function,

    // Members
    StructField,
    UnionField,
    EnumField,
    VariantField,
    InterfaceMethod,

    // Other
    Namespace,
    GenericParam,
    Error,
}
