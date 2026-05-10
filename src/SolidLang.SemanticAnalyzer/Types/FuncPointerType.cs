namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Represents a function pointer type: *func(params) callconv: returnType.
/// </summary>
public sealed class FuncPointerType : SolidType
{
    public IReadOnlyList<SolidType> ParameterTypes { get; }
    public SolidType ReturnType { get; }
    public string? CallingConvention { get; }  // "cdecl", "stdcall", or null for default

    public override SolidTypeKind Kind => SolidTypeKind.FuncPointer;
    public override string DisplayName =>
        $"*func({string.Join(", ", ParameterTypes.Select(t => t.DisplayName))}): {ReturnType.DisplayName}";

    public FuncPointerType(IReadOnlyList<SolidType> parameterTypes, SolidType returnType, string? callingConvention = null)
    {
        ParameterTypes = parameterTypes;
        ReturnType = returnType;
        CallingConvention = callingConvention;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FuncPointerType other) return false;
        if (other.CallingConvention != CallingConvention) return false;
        if (!other.ReturnType.Equals(ReturnType)) return false;
        if (other.ParameterTypes.Count != ParameterTypes.Count) return false;
        for (int i = 0; i < ParameterTypes.Count; i++)
            if (!other.ParameterTypes[i].Equals(ParameterTypes[i])) return false;
        return true;
    }

    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.Add(ReturnType);
        hc.Add(CallingConvention);
        foreach (var p in ParameterTypes) hc.Add(p);
        return hc.ToHashCode();
    }
}
