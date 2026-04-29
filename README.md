# SolidLang Compiler

A static, strongly-typed, system-level programming language compiler targeting LLVM IR.

## Features

- **Static typing** with type inference
- **System-level programming** with pointers, structs, unions, and enums
- **LLVM backend** for native code generation
- **Multiple integer types**: i8, i16, i32, i64, i128, isize, u8, u16, u32, u64, u128, usize
- **Floating-point types**: f16, f32, f64, f128
- **Pointer operations**: `*` (dereference), `&` (address-of), `->` (arrow operator)
- **Control flow**: if/else, while, for, switch, break, continue
- **User-defined types**: struct, union, enum
- **Namespaces** and using declarations
- **Const and static declarations**

## Building

```bash
dotnet build
```

## Running Examples

```bash
cd example/SolidLang.Compilers.Examples
dotnet run -- example1.solid
```

## Project Structure

```
src/SolidLang.Compilers/
├── CodeGen/
│   └── LLVMCodeGenerator.cs    # LLVM IR code generation
├── Semantic/
│   └── SemanticAnalyzer.cs     # Semantic analysis
├── Symbols/
│   ├── Symbol.cs               # Symbol definitions
│   └── SymbolTable.cs          # Symbol table implementation
├── Types/
│   ├── SolidType.cs            # Type system
│   └── TypeRegistry.cs         # Type registry for LLVM mapping
├── Compiler.cs                 # Main compiler entry point
├── SolidLangLexer.cs           # Generated lexer
├── SolidLangParser.cs          # Generated parser
└── SolidLang.Compilers.csproj

grammar/
├── SolidLangLexer.g4           # Lexer grammar
├── SolidLangParser.g4          # Parser grammar
└── gen/                        # Generated .tokens/.interp files

example/SolidLang.Compilers.Examples/
└── example*.solid              # Example programs

test/SolidLang.Compilers.Tests/
└── Tests.cs                    # Unit tests
```

## Language Syntax

### Variable Declaration
```solid
var x: i32 = 10;
var y = 20;  // Type inference
```

### Functions
```solid
func add(a: i32, b: i32): i32 {
    return a + b;
}
```

### Structs
```solid
struct Point {
    x: f32,
    y: f32
}
```

### Pointers
```solid
var num: i32 = 10;
var ptr: *i32 = &num;
var value: i32 = *ptr;
```

### Control Flow
```solid
if x > 0 {
    // ...
} else {
    // ...
}

while x < 10 {
    x = x + 1;
}

for var i: i32 = 0; i < 10; i = i + 1 {
    // ...
}

switch value {
    1 => // case 1
    2, 3 => // case 2 or 3
    else => // default
}
```

## Development

### Regenerating Parser

After modifying grammar files:

```bash
java -jar /path/to/antlr-4.13.2-complete.jar -Dlanguage=CSharp -o src/SolidLang.Compilers grammar/SolidLangParser.g4 grammar/SolidLangLexer.g4
```

### Running Tests

```bash
dotnet test
```

## License

MIT License
