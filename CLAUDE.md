# solid-lang 编译器开发

## 新机上手

```bash
# 1. 前置依赖
#   - .NET 10.0 SDK  (https://dotnet.microsoft.com/download)
#   - clang           (PATH 中可用，用于链接生成可执行文件)
#   - git             (用于 submodule)
#   Windows 用户: clang 可通过 `scoop install llvm` 或从 LLVM 官网安装

# 2. 克隆并拉取子模块
git clone <repo-url> solid-lang-compiler
cd solid-lang-compiler
git submodule update --init --recursive    # spec/ 和 std/

# 3. 还原、构建、测试
dotnet restore
dotnet build
dotnet test                                 # 应全部通过

# 4. 验证端到端
dotnet run --project src/SolidLang.Compiler -- example/3-slim-main/main.solid
./example/3-slim-main/.build/main.exe       # 输出 "hello, world!"，退出码 0
```

### 常见问题

| 现象 | 原因 | 解决 |
|------|------|------|
| `clang: command not found` | 链接器未安装/不在 PATH | Windows: `scoop install llvm`；Linux: `apt install clang`；macOS: 自带 clang |
| `git submodule` 报错 | SSH key 未配置 | 改用 HTTPS URL：`git config submodule.spec.url https://github.com/solid-matrix/solid-lang-spec.git` |
| `NETSDK1045: 当前 .NET SDK 不支持面向 .NET 10.0` | SDK 版本过低 | 安装 .NET 10.0 SDK |
| 编译 example 时链接失败 | std 子模块未拉取 | `git submodule update --init` |
| `dotnet test` 崩溃 | libLLVM runtime 未还原 | `dotnet restore`，确保 NuGet 源可访问 |

## 概览

solid-lang 是一门系统级程序设计语言。

项目使用 C# 开发，目标框架 net10.0，以 LLVM 作为后端（LLVMSharp 20.1.2 + libLLVM 21.1.8），xUnit 作为测试框架。

**当前状态**：编译管线已全线打通 — Parse → Semantic Analysis → IR → Object → Link → Executable。81 测试全通过。

## 项目结构

```
src/SolidLang.Parser/             类库 — 手写递归下降解析器（scannerless），AST 节点定义
src/SolidLang.SemanticAnalyzer/   类库 — 语义分析（两遍）：SymbolBuilderPass + BoundTreeBuilder
src/SolidLang.IrGenerator/        类库 — LLVM IR 代码生成（LLVMSharp）
src/SolidLang.Compiler/           可执行文件 — 编译驱动（管线编排、文件 I/O、链接）
test/SolidLang.Parser.UnitTests/      xUnit 测试，cases/*.solid 驱动（68 tests）
test/SolidLang.Compiler.UnitTests/    xUnit 测试，IR 快照 + 端到端编译运行（13 tests）
spec/                             Git 子模块 — 语言规范与 ANTLR 语法文件（仅供参考）
std/                              Git 子模块 — 标准库源码，编译时自动纳入（std::predeclare 隐式 using）
example/                          示例项目（每个目录一个独立程序）
```

### 项目依赖关系

```
SolidLang.Parser          (无外部依赖)
SolidLang.SemanticAnalyzer → SolidLang.Parser
SolidLang.IrGenerator     → SolidLang.Parser + SolidLang.SemanticAnalyzer + libLLVM/LLVMSharp
SolidLang.Compiler        → 以上三个项目
```

### NuGet 包

| 包 | 版本 | 用途 |
|----|------|------|
| `libLLVM` | 21.1.8 | LLVM 原生库 |
| `LLVMSharp` | 20.1.2 | LLVM C API 的 C# 绑定 |
| `libLLVM.runtime.*` | 21.1.8 | 各平台 LLVM runtime（Debug 配置） |
| `xunit` / `xunit.runner.visualstudio` | 2.9.x | 测试框架 |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | 测试运行器 |
| `coverlet.collector` | 6.0.4 | 代码覆盖率 |

## 架构

### 解析器（SolidLang.Parser）

- 手写 scannerless 递归下降解析器，词法分析与语法分析合一
- 通过 partial class 拆分：`Parser.cs`、`Parser.Declarations.cs`、`Parser.Expressions.cs`、`Parser.Literals.cs`、`Parser.Statements.cs`、`Parser.Types.cs`、`ParserDiagnostics.cs`
- AST 节点位于 `Nodes/` 下，按 Declarations / Expressions / Literals / Statements / Types 分类
- 错误恢复：BadDeclNode、BadStmtNode、BadExprNode、BadTypeNode
- `>>` 歧义通过 `_genericDepth` 跟踪消解

### 语义分析器（SolidLang.SemanticAnalyzer）

- **Pass 1**（`SymbolBuilderPass`）：构建作用域树、注册符号（VariableSymbol / FunctionSymbol / TypeSymbol / MemberSymbol）
- **Pass 2**（`BoundTreeBuilder`）：类型解析、表达式绑定、生成 BoundNode 树
- 作用域查找 `Scope.LookupRecursive` 沿 parent 链 + imported scopes 递归，有循环检测
- `@import(name)` 指定 FFI linker symbol，`@if_msvc`/`@if_not_msvc` 平台条件编译（两遍均需过滤）
- `std::predeclare` 隐式全局 using（`String` struct、`opaque` 等基础类型）
- `func main()` 无返回类型时默认返回 `i32`，无 return 语句时隐式 `ret i32 0`

### IR 生成器（SolidLang.IrGenerator）

- 通过 partial class 拆分：`CodeGenerator.cs`（顶层驱动、类型映射）、`CodeGenerator.Declarations.cs`、`CodeGenerator.Statements.cs`、`CodeGenerator.Expressions.cs`
- 支持的 BoundNode：FunctionDecl、Block、Return、VariableDecl/VariableStmt、Binary/Unary、Call、Conditional、MemberAccess（struct 字段 GEP）、AddressOf（`&`）
- 字符串字面量编译为 `String` struct `{ ptr: *u8, len: usize }`
- `NamedType` → LLVM struct type（从 TypeScope 字段按源码顺序构建）
- **关键注意**：`GetLLVMType(null)` 返回 `Int32Type`（fallback），**不要**返回 `VoidType`（会导致 `BuildAlloca` 崩溃）。void 返回类型仅在 `DeclareFunction` 中特殊处理

## 常用命令

```bash
dotnet build                                                    # 构建全部项目
dotnet test                                                     # 运行全部测试（当前 81 pass / 0 fail）
dotnet run --project src/SolidLang.Compiler -- example/<name>/main.solid   # 编译示例
./example/<name>/.build/main.exe                                # 运行编译产物
```

### 编译输出（位于 `<源文件目录>/.build/`）

- `main.ll` — LLVM IR（文本）
- `main.obj` — 目标文件
- `main.exe` — 可执行文件（通过 clang 链接）
- `main.solid.ast` — 解析 AST 文本表示
- `main.bound.ast` — Bound tree 文本表示

## 编码约定

- C# / .NET 10.0，启用 ImplicitUsings 与 Nullable
- 提交消息风格：时间戳格式（`YYMMDD-HH:MM`），粗粒度保存点
- 新增特性后编译对应 example 验证，然后跑 `dotnet test` 确认无回归

## 语言规范

- `spec/spec.md`（v0.1.1）— 稳定参考版本
