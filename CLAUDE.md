# solid-lang 编译器开发

## 概览

solid-lang 是一门系统级程序设计语言，设计 philosophy 正从"纯静态无隐式"向"safe implicit"演进。

项目使用 C# 开发，目标框架 net10.0，以 LLVM 作为后端（LLVMSharp + libLLVM），xUnit 作为测试框架。

## 项目结构

```
src/SolidLang.Parser/      类库 — 手写递归下降解析器（scannerless），AST 节点定义
src/SolidLang.Compiler/    可执行文件 — LLVM 代码生成，依赖 Parser 项目
test/SolidLang.Parser.UnitTests/     xUnit 测试，cases/*.solid 驱动
test/SolidLang.Compiler.UnitTests/   xUnit 测试（尚无测试用例）
spec/                      Git 子模块 — 语言规范与 ANTLR 语法文件（仅供参考，编译器使用手写解析器）
```

## 架构

### 解析器（SolidLang.Parser）

- 手写 scannerless 递归下降解析器，词法分析与语法分析合一
- 通过 partial class 拆分：`Parser.cs`（主逻辑）、`Parser.Declarations.cs`、`Parser.Expressions.cs`、`Parser.Literals.cs`、`Parser.Statements.cs`、`Parser.Types.cs`、`ParserDiagnostics.cs`
- AST 节点位于 `Nodes/` 下，按 Declarations / Expressions / Literals / Statements / Types 分类
- 错误恢复：BadDeclNode、BadStmtNode、BadExprNode、BadTypeNode 允许解析在遇到错误后继续
- `>>` 歧义（右移 vs 两层闭合泛型）通过 `_genericDepth` 跟踪消解

### 编译器后端（SolidLang.Compiler）

- 通过 LLVMSharp 20.1.2 + libLLVM 21.1.8 生成 LLVM IR
- 当前阶段：解析器重写后，后端代码为占位/stub 状态

## 常用命令

```bash
dotnet build                                  # 构建全部项目
dotnet test                                   # 运行全部测试
dotnet run --project src/SolidLang.Compiler   # 运行编译器
```

## 编码约定

- C# / .NET 10.0，启用 ImplicitUsings 与 Nullable
- 提交消息风格：时间戳格式（`YYMMDD-HH:MM`），粗粒度保存点

## 语言规范

- `spec/spec.md`（v0.1.1）— 稳定参考版本
