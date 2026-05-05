# Solid-Lang 编译器开发迭代规划

## 当前状态

- 测试: 69 单元测试 + 26 集成测试通过
- 基础管道: 词法分析 → 语法分析 → AST → SemanticTree → LLVM IR/目标文件
- 已支持: 基本类型、函数、if/while、算术/比较运算、return/赋值、for循环、break/continue、switch语句、数组字面量、结构体字面量、枚举字面量、字段访问、数组索引、defer语句

---

## 迭代规划

### Iteration 1: 循环控制流完善 ✅ 已完成
**目标**: 完善 for 循环和 break/continue 支持

**任务**:
1. SemaBuilder 支持 `for` 无限循环 (`for { ... }`)
2. SemaBuilder 支持 `for` 条件循环 (`for cond { ... }`)
3. SemaBuilder 支持 C 风格 for 循环 (`for init; cond; step { ... }`)
4. 实现 `break` 和 `continue` 语句
5. CodeGenerator 生成正确的跳转指令
6. 添加单元测试和集成测试

**验证**: 能编译运行包含 break/continue 的循环程序

---

### Iteration 2: switch 语句 ✅ 已完成
**目标**: 实现 switch/match 模式匹配

**任务**:
1. AST 添加 SwitchStatementNode, SwitchArmNode, PatternNode
2. SemanticTree 添加 SemaSwitch, SemaSwitchArm, SemaPattern
3. SemaBuilder 处理 switch 语句和模式匹配
4. CodeGenerator 生成 switch 的 LLVM IR (使用 switch 指令或条件跳转)
5. 支持字面量模式、枚举模式、else 默认分支
6. 添加测试

**验证**: 能编译运行包含 switch 的程序

---

### Iteration 3: 复合类型字面量 ✅ 已完成
**目标**: 支持数组字面量和结构体字面量

**任务**:
1. AST 添加 ArrayLiteralNode, StructLiteralNode ✅ (已存在)
2. SemanticTree 已有 SemaArrayType，需添加构造逻辑 ✅
3. SemaBuilder 处理数组字面量 `[T}{...}` ✅
4. SemaBuilder 处理结构体字面量 `TypeName{field = value}` ✅
5. CodeGenerator 生成正确的初始化代码 ✅
6. 添加测试 ✅

**验证**: 能声明和初始化数组、结构体

---

### Iteration 4: 字段访问与索引操作 ✅ 已完成
**目标**: 完善成员访问表达式

**任务**:
1. 完善 SemaFieldAccessExpr 处理 (obj.field) ✅
2. 完善 SemaIndexExpr 处理 (arr[index]) ✅
3. CodeGenerator 生成 GEP 指令访问结构体字段 ✅
4. CodeGenerator 生成数组索引访问代码 ✅
5. 支持链式访问 (obj.field1.field2, arr[i][j]) ✅
6. 添加测试 ✅

**验证**: 能访问结构体字段和数组元素

---

### Iteration 5: defer 语句 ✅ 已完成
**目标**: 实现 defer 延迟执行

**任务**:
1. AST 添加 DeferStatementNode ✅ (已存在)
2. SemanticTree 添加 SemaDefer ✅
3. SemaBuilder 收集 defer 语句 ✅
4. CodeGenerator 在函数返回前插入 defer 代码 ✅
5. 处理多层 defer 的执行顺序 (LIFO) ✅
6. 添加测试 ✅

**验证**: defer 在 return 前正确执行

---

### Iteration 6: 引用类型完善 ✅ 已完成
**目标**: 完善 `^T` 和 `^!T` 引用类型

**任务**:
1. 实现 `^expr` 取引用操作 ✅
2. 实现引用类型的自动解引用 ✅
3. 可变引用 `^!T` 的写权限检查 ✅ (类型解析已修复)
4. CodeGenerator 正确处理引用 (作为指针) ✅
5. 添加借用检查基础 (可选，可作为后续迭代)
6. 添加测试 ✅

**验证**: 能正确使用引用传递参数 ✅

---

### Iteration 7: 指针与 unsafe 块
**目标**: 实现原始指针操作和 unsafe 注解

**任务**:
1. AST 添加 UnsafeBlockNode 或 @unsafe 注解处理
2. 实现 `&expr` 取地址操作
3. 实现 `*ptr` 解引用操作
4. CodeGenerator 生成指针操作指令
5. 语义检查: 指针操作必须在 unsafe 块内
6. 添加测试

**验证**: 能在 unsafe 块中使用指针

---

### Iteration 8: 联合体 (union) ✅ 已完成
**目标**: 实现 union 类型

**任务**:
1. AST 添加 UnionDeclarationNode, UnionLiteralNode ✅ (已存在)
2. SemanticTree 添加 SemaUnionType ✅
3. SemaBuilder 处理 union 声明 ✅
4. SemaBuilder 处理 union 字面量 `TypeName::field(value)` ✅
5. CodeGenerator 生成 union 类型 (使用足够大的存储) ✅
6. 添加测试 ✅

**验证**: 能声明和使用 union 类型 ✅

---

### Iteration 9: Variant 类型
**目标**: 实现 variant (和类型/枚举变体)

**任务**:
1. AST 添加 VariantDeclarationNode, VariantLiteralNode
2. SemanticTree 添加 SemaVariantType
3. SemaBuilder 处理 variant 声明
4. SemaBuilder 处理 variant 字面量
5. 在 switch 中支持 variant 模式匹配
6. CodeGenerator 生成正确的类型布局 (tag + payload)
7. 添加测试

**验证**: 能使用 variant 表示和类型

---

### Iteration 10: 元组类型完善
**目标**: 完善元组的声明和使用

**任务**:
1. AST 添加 TupleLiteralNode (已有 TupleTypeNode)
2. SemaBuilder 处理元组字面量 `(a: T1, b: T2)`
3. 完善元组字段访问 `tuple.0`, `tuple.1`
4. CodeGenerator 生成元组类型 (作为匿名结构体)
5. 添加测试

**验证**: 能创建和使用元组

---

### Iteration 11: 命名空间与模块系统
**目标**: 完善命名空间和 using 声明

**任务**:
1. 实现完整命名空间路径解析 `ns::module::func()`
2. 实现 using 声明的符号导入
3. 符号可见性检查
4. 正确的名称修饰 (mangling)
5. 添加测试

**验证**: 能跨命名空间调用函数

---

### Iteration 12: 函数调用约定与 FFI
**目标**: 支持不同调用约定和外部函数接口

**任务**:
1. 解析 cdecl, stdcall 调用约定
2. 在函数类型中存储调用约定
3. CodeGenerator 使用正确的 LLVM 调用约定属性
4. 支持 extern 函数声明
5. 添加测试

**验证**: 能调用 C 库函数

---

## 后续迭代 (高级特性)

- **Iteration 13-14**: 泛型基础
- **Iteration 15-16**: 接口与多态
- **Iteration 17-18**: 编译时注解与反射
- **Iteration 19-20**: 标准库基础

---

## 迭代执行原则

1. **每个迭代独立可测试**: 完成后有明确的验证标准
2. **向后兼容**: 不破坏已有功能
3. **测试驱动**: 先写测试，再实现
4. **文档同步**: 更新相关注释和文档
