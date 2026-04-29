- 项目介绍

  - SolidLang是一个纯静态、强正交、系统级编程语言
  - 本项目旨在设计该语言并开发配套编译器
  - 将使用antlr4设计词法语法并生成对应的词法分析器和语法分析器
  - 需要自行开发语义分析器
  - 将使用llvm作为中后端

- 项目结构

  ```
  src/
      SolidLang.Compilers -> 编译器
  test/
      SolidLang.Compilers.Tests -> 单元测试
  example/
      SolidLang.Compilers.Examples -> 测试并输出结构
  grammar/
  	SolidLangLexer.g4 -> antlr4 g4 格式的词法定义
  	SolidLangParser.g4 -> antlr4 g4 格式的语法定义
  	solid-lang.md -> SolidLang 说明文档
  ```

- 配套工具文档

  - 几个类似语言的g4 `./knowledge/grammars`目录下
  - LLVMSharp 源码在 `./knowledge/LLVMSharp` 目录下
  - altlr4的文档在 `/knowledge/antlr4-doc` 目录下

- 要求

  - 需要调用llvm API来进行，不要中途生成 `LLVM IR`

