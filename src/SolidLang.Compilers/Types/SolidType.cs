namespace SolidLang.Compilers.Types;

public abstract record SolidType(string Name);

public sealed record I32Type() : SolidType("i32");

public sealed record VoidType() : SolidType("void");

public sealed record BoolType() : SolidType("bool");
