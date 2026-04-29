# SolidLang Compiler

A static, strongly-typed, system-level programming language compiler targeting LLVM IR.

## Project Overview

SolidLang is a systems programming language designed for performance and safety. The compiler is written in C# and uses:
- **ANTLR 4** for lexer/parser generation
- **LLVMSharp** for LLVM IR code generation

## Architecture

### Compiler Pipeline

1. **Lexing/Parsing**: ANTLR-generated lexer and parser convert source code to AST
2. **Semantic Analysis**: `SemanticAnalyzer` walks the AST, building symbol tables and type information
3. **Code Generation**: `LLVMCodeGenerator` produces LLVM IR from the analyzed AST

### Key Components

#### Types (`src/SolidLang.Compilers/Types/`)
- `SolidType.cs`: Type hierarchy (I8Type, I32Type, StructType, PointerType, etc.)
- `TypeRegistry.cs`: Centralized type-to-LLVM type mapping

#### Symbols (`src/SolidLang.Compilers/Symbols/`)
- `Symbol.cs`: Symbol definitions (VariableSymbol, FunctionSymbol, StructSymbol, etc.)
- `SymbolTable.cs`: Scoped symbol table implementation

#### Semantic Analysis (`src/SolidLang.Compilers/Semantic/`)
- `SemanticAnalyzer.cs`: ANTLR listener that collects symbols and types

#### Code Generation (`src/SolidLang.Compilers/CodeGen/`)
- `LLVMCodeGenerator.cs`: Generates LLVM IR, organized into regions:
  - Fields
  - Constructor and Public API
  - Module Generation
  - Type Declarations
  - Function Generation
  - Statement Generation
  - Variable Declarations
  - Expression Generation
  - Type Conversion
  - Output

## Building and Testing

```bash
# Build
dotnet build

# Run all examples
cd example/SolidLang.Compilers.Examples
for f in example*.solid; do dotnet run -- "$f"; done

# Run tests
dotnet test
```

## Adding New Features

### Adding a New Type

1. Add type record in `Types/SolidType.cs`
2. Add type name constant in `Types/TypeRegistry.cs`
3. Update `GetLLVMType` in both `TypeRegistry.cs` and `LLVMCodeGenerator.cs`

### Adding a New Statement

1. Add grammar rule in `grammar/SolidLangParser.g4`
2. Regenerate parser
3. Add handler in `SemanticAnalyzer.cs`
4. Add generation method in `LLVMCodeGenerator.cs` (Statement Generation region)

## Code Style

- Use regions to organize large files
- Prefer pattern matching over if-else chains
- Use `SolidType` pattern matching for type dispatch
- Keep methods focused and single-purpose

## Known Limitations

- No generics implementation yet
- No interface implementation yet
- No error recovery in parser
- Limited type inference
