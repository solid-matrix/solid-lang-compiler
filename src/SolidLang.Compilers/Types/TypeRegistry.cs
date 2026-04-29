using LLVMSharp.Interop;
using SolidLang.Compilers.Symbols;

namespace SolidLang.Compilers.Types;

/// <summary>
/// Centralized type registry for mapping SolidLang types to LLVM types.
/// Provides a single source of truth for all type conversions.
/// </summary>
public sealed class TypeRegistry
{
    // Primitive type name constants
    public const string I8 = "i8";
    public const string I16 = "i16";
    public const string I32 = "i32";
    public const string I64 = "i64";
    public const string I128 = "i128";
    public const string Isize = "isize";

    public const string U8 = "u8";
    public const string U16 = "u16";
    public const string U32 = "u32";
    public const string U64 = "u64";
    public const string U128 = "u128";
    public const string Usize = "usize";

    public const string F16 = "f16";
    public const string F32 = "f32";
    public const string F64 = "f64";
    public const string F128 = "f128";

    public const string Bool = "bool";
    public const string Void = "void";

    // Cache for LLVM types
    private readonly Dictionary<string, LLVMTypeRef> _llvmTypeCache = new();
    private readonly Dictionary<string, SolidType> _userTypes = new();

    /// <summary>
    /// Registers a user-defined type (struct, union, enum).
    /// </summary>
    public void RegisterUserType(string name, SolidType type, LLVMTypeRef llvmType)
    {
        _userTypes[name] = type;
        _llvmTypeCache[name] = llvmType;
    }

    /// <summary>
    /// Gets the LLVM type for a SolidLang type.
    /// </summary>
    public LLVMTypeRef GetLLVMType(SolidType type)
    {
        return type switch
        {
            // Signed integers
            I8Type => LLVMTypeRef.Int8,
            I16Type => LLVMTypeRef.Int16,
            I32Type => LLVMTypeRef.Int32,
            I64Type => LLVMTypeRef.Int64,
            I128Type => LLVMContextRef.Global.GetIntType(128),
            IsizeType => LLVMTypeRef.Int64,

            // Unsigned integers (same LLVM types, semantics differ)
            U8Type => LLVMTypeRef.Int8,
            U16Type => LLVMTypeRef.Int16,
            U32Type => LLVMTypeRef.Int32,
            U64Type => LLVMTypeRef.Int64,
            U128Type => LLVMContextRef.Global.GetIntType(128),
            UsizeType => LLVMTypeRef.Int64,

            // Floating-point
            F16Type => LLVMTypeRef.Half,
            F32Type => LLVMTypeRef.Float,
            F64Type => LLVMTypeRef.Double,
            F128Type => LLVMTypeRef.FP128,

            // Other
            BoolType => LLVMTypeRef.Int1,
            VoidType => LLVMTypeRef.Void,

            // User-defined types
            StructType structType => GetUserLLVMType(structType.Name),
            EnumType enumType => GetUserLLVMType(enumType.Name),
            UnionType unionType => GetUserLLVMType(unionType.Name),

            // Composite types
            TupleType tupleType => LLVMTypeRef.CreateStruct(
                tupleType.Elements.Select(GetLLVMType).ToArray(), false),
            PointerType pointerType => LLVMTypeRef.CreatePointer(
                GetLLVMType(pointerType.ElementType), 0),

            _ => throw new NotSupportedException($"Unknown type: {type.Name}")
        };
    }

    /// <summary>
    /// Gets the LLVM type from a type name string.
    /// </summary>
    public LLVMTypeRef GetLLVMTypeFromName(string typeName)
    {
        // Handle pointer types
        if (typeName.StartsWith("*"))
        {
            var elementType = GetLLVMTypeFromName(typeName.Substring(1));
            return LLVMTypeRef.CreatePointer(elementType, 0);
        }

        // Check cache first
        if (_llvmTypeCache.TryGetValue(typeName, out var cachedType))
            return cachedType;

        // Check user-defined types
        if (_userTypes.ContainsKey(typeName))
            return _llvmTypeCache[typeName];

        // Primitive types
        return typeName switch
        {
            I8 => LLVMTypeRef.Int8,
            I16 => LLVMTypeRef.Int16,
            I32 => LLVMTypeRef.Int32,
            I64 => LLVMTypeRef.Int64,
            I128 => LLVMContextRef.Global.GetIntType(128),
            Isize => LLVMTypeRef.Int64,

            U8 => LLVMTypeRef.Int8,
            U16 => LLVMTypeRef.Int16,
            U32 => LLVMTypeRef.Int32,
            U64 => LLVMTypeRef.Int64,
            U128 => LLVMContextRef.Global.GetIntType(128),
            Usize => LLVMTypeRef.Int64,

            F16 => LLVMTypeRef.Half,
            F32 => LLVMTypeRef.Float,
            F64 => LLVMTypeRef.Double,
            F128 => LLVMTypeRef.FP128,

            Bool => LLVMTypeRef.Int1,
            Void => LLVMTypeRef.Void,

            _ => LLVMTypeRef.Int32 // Default fallback
        };
    }

    /// <summary>
    /// Gets the type name string from a SolidLang type.
    /// </summary>
    public string GetTypeName(SolidType type)
    {
        return type switch
        {
            // Signed integers
            I8Type => I8,
            I16Type => I16,
            I32Type => I32,
            I64Type => I64,
            I128Type => I128,
            IsizeType => Isize,

            // Unsigned integers
            U8Type => U8,
            U16Type => U16,
            U32Type => U32,
            U64Type => U64,
            U128Type => U128,
            UsizeType => Usize,

            // Floating-point
            F16Type => F16,
            F32Type => F32,
            F64Type => F64,
            F128Type => F128,

            // Other
            BoolType => Bool,
            VoidType => Void,

            // User-defined types
            StructType st => st.Name,
            UnionType ut => ut.Name,
            EnumType et => et.Name,

            // Composite types
            PointerType pt => "*" + GetTypeName(pt.ElementType),
            TupleType tt => $"({string.Join(", ", tt.Elements.Select(GetTypeName))})",

            _ => type.Name
        };
    }

    /// <summary>
    /// Creates a SolidType from a type name string.
    /// </summary>
    public SolidType CreateSolidType(string typeName)
    {
        // Check user-defined types
        if (_userTypes.TryGetValue(typeName, out var userType))
            return userType;

        // Handle pointer types
        if (typeName.StartsWith("*"))
        {
            var elementType = CreateSolidType(typeName.Substring(1));
            return new PointerType(elementType, true);
        }

        // Primitive types
        return typeName switch
        {
            I8 => new I8Type(),
            I16 => new I16Type(),
            I32 => new I32Type(),
            I64 => new I64Type(),
            I128 => new I128Type(),
            Isize => new IsizeType(),

            U8 => new U8Type(),
            U16 => new U16Type(),
            U32 => new U32Type(),
            U64 => new U64Type(),
            U128 => new U128Type(),
            Usize => new UsizeType(),

            F16 => new F16Type(),
            F32 => new F32Type(),
            F64 => new F64Type(),
            F128 => new F128Type(),

            Bool => new BoolType(),
            Void => new VoidType(),

            _ => throw new NotSupportedException($"Unknown type: {typeName}")
        };
    }

    /// <summary>
    /// Checks if a type name is a primitive type.
    /// </summary>
    public static bool IsPrimitiveType(string typeName)
    {
        return typeName switch
        {
            I8 or I16 or I32 or I64 or I128 or Isize => true,
            U8 or U16 or U32 or U64 or U128 or Usize => true,
            F16 or F32 or F64 or F128 => true,
            Bool or Void => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if a type name is an integer type.
    /// </summary>
    public static bool IsIntegerType(string typeName)
    {
        return typeName switch
        {
            I8 or I16 or I32 or I64 or I128 or Isize => true,
            U8 or U16 or U32 or U64 or U128 or Usize => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if a type name is a floating-point type.
    /// </summary>
    public static bool IsFloatType(string typeName)
    {
        return typeName is F16 or F32 or F64 or F128;
    }

    private LLVMTypeRef GetUserLLVMType(string name)
    {
        if (_llvmTypeCache.TryGetValue(name, out var type))
            return type;
        throw new InvalidOperationException($"User type not registered: {name}");
    }
}
