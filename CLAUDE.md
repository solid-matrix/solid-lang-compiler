# solid-lang 编译器开发

## 概览

solid-lang 是一门纯静态无隐式的系统级程序设计语言。

项目旨在完成对solid-lang的编译器开发。

- 项目使用C#开发
- 采用ANTLR 4 作为词语语法的生成器；
- 使用 LLVMSharp库，以LLVM作为后端；

## 架构

- 词法与语法分析：使用ANTLR 4 生成词法分析器与语法分析器，把源代码转换成为AST
- 语义分析：使用SemanticAnalyzer遍历AST，生成SemanticTree
- 目标文件生成：使用CodeGenerator，遍历SemanticTree，编译成为目标文件

## 目录

- solid-lang 设计项目在`./spec` 目录下
  - `./spec/spec.md` 为solid-lang的设计规范
  - `./spec/SolidLangLexer.g4` 为solid-lang的ANTLR 4 G4格式的词法文件
  - `./spec/SolidLangParser.g4` 为solid-lang的ANTLR 4 G4格式的语法文件
  - 该目录下其他文件为开发中的遗留代码，不用参考
  - `./spec` 为git 子模块，请勿修改其中的文件
- `./src` 为编译器源码项目所在目录
- `./test` 为编译器测试项目所在目录

## 可用工具

- antlr4工具命令：`java org.antlr.v4.Tool`