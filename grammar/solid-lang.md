# Solid 语言语法参考

## 目录

1. [概述](#概述)
2. [词法元素](#词法元素)
   - [关键字](#关键字)
   - [标识符](#标识符)
   - [字面量](#字面量)
   - [运算符](#运算符)
   - [分隔符](#分隔符)
   - [注释](#注释)
3. [程序结构](#程序结构)
4. [声明](#声明)
   - [命名空间声明](#命名空间声明)
   - [命名空间使用声明](#命名空间使用声明)
   - [常量声明](#常量声明)
   - [不可变静态变量声明](#不可变静态变量声明)
   - [变量声明](#变量声明)
   - [静态变量声明](#静态变量声明)
5. [类型定义](#类型定义)
   - [结构体](#结构体)
   - [联合体](#联合体)
   - [枚举](#枚举)
   - [接口](#接口)
6. [函数](#函数)
   - [函数声明](#函数声明)
   - [函数类型](#函数类型)
7. [类型系统](#类型系统)
   - [命名类型](#命名类型)
   - [引用类型](#引用类型)
   - [指针类型](#指针类型)
   - [元组类型](#元组类型)
   - [数组类型](#数组类型)
8. [泛型](#泛型)
   - [泛型参数](#泛型参数)
   - [泛型约束](#泛型约束)
9. [语句](#语句)
   - [空语句](#空语句)
   - [块语句](#块语句)
   - [表达式语句](#表达式语句)
   - [赋值语句](#赋值语句)
   - [条件语句](#条件语句)
   - [循环语句](#循环语句)
   - [跳转语句](#跳转语句)
   - [延迟执行语句](#延迟执行语句)
10. [表达式](#表达式)
    - [字面量表达式](#字面量表达式)
    - [标识符表达式](#标识符表达式)
    - [一元表达式](#一元表达式)
    - [二元表达式](#二元表达式)
    - [条件表达式](#条件表达式)
    - [后缀表达式](#后缀表达式)
11. [注解](#注解)

---

## 概述

Solid 是一门系统级编程语言，设计目标是：
- **安全性**：通过引用类型系统提供内存安全保障
- **性能**：零成本抽象，与 C 语言兼容
- **表达力**：支持泛型、接口等现代语言特性

---

## 词法元素

### 关键字

| 关键字 | 用途 |
|--------|------|
| `namespace` | 命名空间声明 |
| `using` | 命名空间引入 |
| `func` | 函数声明 |
| `interface` | 接口声明 |
| `struct` | 结构体声明 |
| `enum` | 枚举声明 |
| `union` | 联合体声明 |
| `var` | 变量声明 |
| `const` | 常量声明 |
| `static` | 静态变量声明 |
| `if` | 条件语句 |
| `else` | 条件分支 |
| `while` | while 循环 |
| `for` | for 循环 |
| `in` | for-in 循环（保留） |
| `match` | 模式匹配（保留） |
| `break` | 跳出循环 |
| `continue` | 继续循环 |
| `return` | 函数返回 |
| `defer` | 延迟执行 |
| `where` | 泛型约束 |

### 标识符

标识符用于命名变量、函数、类型等。

**语法规则**：
```
标识符 = 字母 | '_' , { 字母 | 数字 | '_' }
```

**示例**：
```solid
foo
_bar
MyType
value1
```

### 字面量

#### 整数字面量

支持十进制、十六进制、八进制、二进制表示：

```solid
42              // 十进制
0xFF            // 十六进制
0o755           // 八进制
0b1010          // 二进制
```

支持下划线分隔符：
```solid
1_000_000       // 十进制
0xFF_FF_FF      // 十六进制
```

支持类型后缀：
```solid
42i8            // 8位有符号整数
42u32           // 32位无符号整数
42isize         // 指针大小有符号整数
```

| 后缀 | 类型 |
|------|------|
| `i8`, `u8` | 8位整数 |
| `i16`, `u16` | 16位整数 |
| `i32`, `u32` | 32位整数 |
| `i64`, `u64` | 64位整数 |
| `i128`, `u128` | 128位整数 |
| `isize`, `usize` | 指针大小整数 |

#### 浮点字面量

```solid
3.14            // 浮点数
1.0e10          // 科学计数法
1e-5            // 负指数
```

支持类型后缀：
```solid
3.14f32         // 32位浮点
3.14f64         // 64位浮点
```

| 后缀 | 类型 |
|------|------|
| `f16` | 16位浮点 |
| `f32` | 32位浮点 |
| `f64` | 64位浮点 |
| `f128` | 128位浮点 |

#### 字符字面量

```solid
'a'             // 普通字符
'\n'            // 换行符
'\t'            // 制表符
'\''             // 单引号
'\\'            // 反斜杠
```

支持的转义序列：
| 转义 | 含义 |
|------|------|
| `\n` | 换行 |
| `\r` | 回车 |
| `\t` | 制表符 |
| `\"` | 双引号 |
| `\'` | 单引号 |
| `\\` | 反斜杠 |

#### 字符串字面量

```solid
"Hello, World!"
"Line 1\nLine 2"
"Path: C:\\Users"
```

#### 布尔字面量

```solid
true
false
```

### 运算符

#### 算术运算符

| 运算符 | 描述 | 示例 |
|--------|------|------|
| `+` | 加法 | `a + b` |
| `-` | 减法 | `a - b` |
| `*` | 乘法 | `a * b` |
| `/` | 除法 | `a / b` |
| `%` | 取模 | `a % b` |

#### 比较运算符

| 运算符 | 描述 | 示例 |
|--------|------|------|
| `==` | 等于 | `a == b` |
| `!=` | 不等于 | `a != b` |
| `<` | 小于 | `a < b` |
| `>` | 大于 | `a > b` |
| `<=` | 小于等于 | `a <= b` |
| `>=` | 大于等于 | `a >= b` |

#### 逻辑运算符

| 运算符 | 描述 | 示例 |
|--------|------|------|
| `&&` | 逻辑与 | `a && b` |
| `||` | 逻辑或 | `a || b` |
| `!` | 逻辑非 | `!a` |

#### 位运算符

| 运算符 | 描述 | 示例 |
|--------|------|------|
| `&` | 位与 | `a & b` |
| `|` | 位或 | `a | b` |
| `^` | 位异或 | `a ^ b` |
| `~` | 位取反 | `~a` |
| `<<` | 左移 | `a << n` |
| `>>` | 右移 | `a >> n` |

#### 赋值运算符

| 运算符 | 描述 | 等价形式 |
|--------|------|----------|
| `=` | 赋值 | `a = b` |
| `+=` | 加赋值 | `a = a + b` |
| `-=` | 减赋值 | `a = a - b` |
| `*=` | 乘赋值 | `a = a * b` |
| `/=` | 除赋值 | `a = a / b` |
| `%=` | 模赋值 | `a = a % b` |
| `&=` | 位与赋值 | `a = a & b` |
| `|=` | 位或赋值 | `a = a | b` |
| `^=` | 异或赋值 | `a = a ^ b` |
| `<<=` | 左移赋值 | `a = a << b` |
| `>>=` | 右移赋值 | `a = a >> b` |

#### 指针与引用运算符

| 运算符 | 描述 | 环境 | 示例 |
|--------|------|------|------|
| `&` | 取地址 | unsafe | `&value` |
| `*` | 解引用 | unsafe | `*ptr` |
| `^` | 取引用 | safe | `^value` |
| `->` | 指针成员访问 | unsafe | `ptr->field` |

### 分隔符

| 分隔符 | 描述 |
|--------|------|
| `::` | 命名空间/作用域 |
| `:` | 类型标注 |
| `,` | 列表分隔 |
| `.` | 成员访问 |
| `;` | 语句结束 |
| `=>` | 箭头（保留） |
| `->` | 指针成员访问 |
| `@` | 注解前缀 |
| `?` | 三元条件 |
| `{ }` | 代码块 |
| `[ ]` | 数组/索引 |
| `( )` | 分组/参数 |

### 注释

```solid
// 单行注释（隐藏）

/// 文档注释（保留，用于生成文档）

/// 多行文档注释示例
/// 可以跨越多行
```

---

## 程序结构

Solid 程序由以下部分组成：

1. **命名空间声明**（可选，但推荐）
2. **命名空间引入**（零个或多个）
3. **顶层声明**（零个或多个）

**示例**：
```solid
namespace core::math;

using core::types;

const PI: f64 = 3.14159265359;

struct Vector3 {
    x: f32,
    y: f32,
    z: f32,
}

func length(v: Vector3): f32 {
    return (v.x * v.x + v.y * v.y + v.z * v.z).sqrt();
}
```

---

## 声明

### 命名空间声明

每个源文件可以声明一个命名空间。

**语法**：
```solid
namespace <命名空间路径>;
```

**示例**：
```solid
namespace core;
namespace core::math;
namespace core::math::geometry;
```

**规则**：
- 程序入口 `main` 函数必须位于 `main` 命名空间

### 命名空间使用声明

引入其他命名空间，简化类型和函数的使用。

**语法**：
```solid
using <命名空间路径>;
```

**示例**：
```solid
using core::math;
using core::io;
```

### 常量声明

声明编译期常量，不存在于运行时。

**语法**：
```solid
[<注解>] const <名称>: <类型> = <表达式>;
```

**示例**：
```solid
const MAX_SIZE: u32 = 1024;
@private const ENABLE_DEBUG: bool = false;
```

**特点**：
- 必须显式指定类型
- 初始值必须在编译期可计算
- 不可取地址，不存在于运行时

### 不可变静态变量声明

声明存储在 `.rodata` 段的只读静态变量。

**语法**：
```solid
[<注解>] const static <名称>: <类型> = <表达式>;
```

**示例**：
```solid
const static VERSION: u32 = 0x01010101u;
@public const static APP_NAME: string = "MyApp";
```

**特点**：
- 必须显式指定类型
- 初始值必须在编译期可计算
- 存在于 `.rodata` 段，可取地址，只读

### 变量声明

声明局部变量。

**语法**：
```solid
[<注解>] var <名称>: <类型> = <表达式>;
[<注解>] var <名称>: <类型>;
[<注解>] var <名称> = <表达式>;
```

**示例**：
```solid
var count: i32 = 0;          // 完整声明
var count: i32;              // 仅类型（后续赋值）
var count = 0;               // 类型推断
```

### 静态变量声明

声明静态生命周期变量。

**语法**：
```solid
[<注解>] static <名称>: <类型> = <表达式>;
[<注解>] static <名称>: <类型>;
[<注解>] static <名称> = <表达式>;
```

**示例**：
```solid
static counter: i32 = 0;
static initialized: bool;
```

---

## 类型定义

### 结构体

定义复合数据类型。

**语法**：
```solid
[<注解>] struct <名称> [<泛型参数>] [where <泛型约束>] {
    [<字段列表>]
}
```

**示例**：
```solid
// 基本结构体
struct Point {
    x: f32,
    y: f32,
}

// 空结构体（零大小类型）
struct None {}

// 带注解的结构体
@packed struct Data {
    header: u32,
    payload: [256]u8,
}

// 泛型结构体
struct Couple<T> where T: Add<T, T> {
    first: T,
    second: T,
}
```

**字段语法**：
```solid
[<注解>] <名称>: <类型>
```

### 联合体

定义共享内存的复合类型，与 C 兼容。

**语法**：
```solid
[<注解>] union <名称> [<泛型参数>] [where <泛型约束>] {
    [<字段列表>]
}
```

**示例**：
```solid
// 基本联合体
union Value {
    as_int: i32,
    as_float: f32,
}

// 空联合体（零大小类型）
union None {}

// 泛型联合体
union Result<T, E> where T: Copy {
    ok: T,
    err: E,
}
```

**特点**：
- 所有字段共享同一内存空间
- 大小等于最大字段的大小
- 与 C 语言二进制兼容

### 枚举

定义枚举类型。

**语法**：
```solid
[<注解>] enum <名称> [: <底层类型>] {
    [<字段列表>]
}
```

**示例**：
```solid
// 基本枚举（默认 i32）
enum Color {
    Red,
    Green,
    Blue,
}

// 指定底层类型
enum Flags: u32 {
    None = 0,
    Read = 0x01,
    Write = 0x02,
    Execute = 0x04,
}

// 显式值
enum Status {
    Ok = 0,
    Error = -1,
    Pending,       // 自动递增：-1 + 1 = 0（注意：建议显式指定避免歧义）
}
```

**字段语法**：
```solid
[<注解>] <名称> [= <表达式>]
```

**使用**：
```solid
var color: Color = Color::Red;
var flags: Flags = Flags::Read | Flags::Write;
```

### 接口

定义行为契约。

**语法**：
```solid
[<注解>] interface <名称> [<泛型参数>] [where <泛型约束>] {
    [<函数签名列表>]
}
```

**示例**：
```solid
// 基本接口
interface Drawable {
    func draw(self: ^Self): void;
}

// 泛型接口
interface Add<TLeft, TRight, TResult> {
    func add(left: TLeft, right: TRight): TResult;
}

// 带约束的接口
interface Comparable<T> where T: Eq<T> {
    func compare(self: ^Self, other: ^T): i32;
}
```

---

## 函数

### 函数声明

**语法**：
```solid
[<注解>] [<命名空间前缀>] func <名称> [<泛型参数>] (<参数列表>): <返回类型> [where <泛型约束>] <函数体>
```

**示例**：
```solid
// 基本函数
func add(a: i32, b: i32): i32 {
    return a + b;
}

// 无返回值
func greet(name: string): void {
    print("Hello, ", name);
}

// 外部函数声明
@extern func malloc(size: usize): ^void;

// 带命名空间的函数
func core::math::sqrt(x: f64): f64;

// 泛型函数
func identity<T>(value: T): T where T: Copy {
    return value;
}
```

**参数语法**：
```solid
[<注解>] <名称>: <类型>
```

### 函数类型

函数类型用于声明函数指针。

**语法**：
```solid
func (<参数类型列表>): <返回类型>
```

**示例**：
```solid
var callback: func(i32, i32): i32;
callback = add;
```

---

## 类型系统

### 命名类型

通过名称引用的类型。

```solid
i32
f64
bool
string
MyStruct
core::math::Vector3
```

### 引用类型

安全环境的引用。

| 类型 | 描述 |
|------|------|
| `^T` | 可变引用 |
| `!^T` | 不可变引用 |

**示例**：
```solid
var ref: ^i32 = ^value;       // 可变引用
var cref: !^i32 = ^value;     // 不可变引用
```

**特点**：
- 编译器保证内存安全
- 自动解引用
- 适用于 safe 环境

### 指针类型

unsafe 环境的原始指针，与 C 兼容。

| 类型 | 描述 |
|------|------|
| `*T` | 可变指针 |
| `!*T` | 不可变指针 |

**示例**：
```solid
var ptr: *i32 = &value;       // 可变指针
var cptr: !*i32 = &value;     // 不可变指针
```

**特点**：
- 与 C 语言兼容
- 需要 unsafe 块操作
- 手动管理生命周期

**操作**：
```solid
var value: i32 = 42;
var ptr: *i32 = &value;       // 取地址
var deref: i32 = *ptr;        // 解引用
var member: i32 = ptr->field; // 指针成员访问（等效于 (*ptr).field）
```

### 元组类型

固定大小的异构集合。

**语法**：
```solid
(<类型1>, <类型2>, ...)
```

**示例**：
```solid
var pair: (i32, f64) = (1, 3.14);
var triple: (i32, i32, i32) = (1, 2, 3);
```

### 数组类型

固定大小的同构集合。

**语法**：
```solid
[<大小>]<元素类型>
```

**示例**：
```solid
var buffer: [256]u8;
var matrix: [3][3]f32;        // 二维数组
```

---

## 泛型

### 泛型参数

**语法**：
```solid
<<参数列表>>
```

**示例**：
```solid
struct Box<T> {
    value: T,
}

func identity<T>(value: T): T {
    return value;
}
```

### 泛型约束

使用 `where` 子句约束泛型参数。

**语法**：
```solid
where <类型>: <接口>
```

**示例**：
```solid
struct Pair<T> where T: Comparable<T> {
    first: T,
    second: T,
}

func sort<T>(arr: ^[T], len: usize): void where T: Comparable<T> {
    // ...
}

// 多约束
func process<T, U>(a: T, b: U): void
    where T: Add<T, T>,
          U: Convertible<T>
{
    // ...
}
```

---

## 语句

### 空语句

```solid
;
```

用于占位或分隔。

### 块语句

```solid
{
    <语句列表>
}
```

**示例**：
```solid
{
    var x: i32 = 1;
    var y: i32 = 2;
    var sum: i32 = x + y;
}
```

### 表达式语句

```solid
<表达式>;
```

**示例**：
```solid
foo();
count += 1;
```

### 赋值语句

```solid
<表达式> <赋值运算符> <表达式>;
```

**示例**：
```solid
x = 10;
x += 5;
x <<= 2;
```

### 条件语句

**语法**：
```solid
if <表达式> <语句> [else <语句>]
```

**示例**：
```solid
if x > 0 {
    print("positive");
}

if x > 0 {
    print("positive");
} else {
    print("non-positive");
}

if x > 0 {
    print("positive");
} else if x < 0 {
    print("negative");
} else {
    print("zero");
}
```

### 循环语句

#### while 循环

**语法**：
```solid
while <表达式> <语句>
```

**示例**：
```solid
var i: i32 = 0;
while i < 10 {
    print(i);
    i += 1;
}
```

#### for 循环

**语法**：
```solid
for [<初始化>]; [<条件>]; [<更新>] <语句>
```

**示例**：
```solid
for var i: i32 = 0; i < 10; i += 1 {
    print(i);
}

// 无限循环
for ; ; {
    if should_exit() {
        break;
    }
}
```

### 跳转语句

#### break

跳出循环。

```solid
break;
```

#### continue

继续下一次循环迭代。

```solid
continue;
```

#### return

从函数返回。

```solid
return;                    // 无返回值
return <表达式>;           // 有返回值
```

**示例**：
```solid
func find(arr: ^[i32], len: usize, target: i32): i32 {
    for var i: usize = 0; i < len; i += 1 {
        if arr[i] == target {
            return i;
        }
    }
    return -1;
}
```

### 延迟执行语句

在作用域结束时执行。

**语法**：
```solid
defer <表达式>;
```

**示例**：
```solid
func process_file(path: string): void {
    var file: ^File = fopen(path, "r");
    defer fclose(file);

    // 处理文件...
    // 函数返回时自动调用 fclose(file)
}
```

---

## 表达式

### 字面量表达式

```solid
42          // 整数
3.14        // 浮点数
'a'         // 字符
"hello"     // 字符串
true        // 布尔值
```

### 标识符表达式

```solid
foo
core::math::PI
Color::Red
```

### 一元表达式

| 运算符 | 描述 | 示例 |
|--------|------|------|
| `-` | 负号 | `-x` |
| `!` | 逻辑非 | `!flag` |
| `~` | 位取反 | `~bits` |
| `&` | 取地址（unsafe） | `&value` |
| `*` | 解引用（unsafe） | `*ptr` |
| `^` | 取引用（safe） | `^value` |

### 二元表达式

按优先级从低到高：

| 优先级 | 运算符 | 描述 |
|--------|--------|------|
| 1 | `||` | 逻辑或 |
| 2 | `&&` | 逻辑与 |
| 3 | `|` | 位或 |
| 4 | `^` | 位异或 |
| 5 | `&` | 位与 |
| 6 | `==`, `!=` | 相等性 |
| 7 | `<`, `>`, `<=`, `>=` | 比较 |
| 8 | `<<`, `>>` | 移位 |
| 9 | `+`, `-` | 加减 |
| 10 | `*`, `/`, `%` | 乘除模 |

### 条件表达式

三元条件运算符。

**语法**：
```solid
<条件> ? <真值> : <假值>
```

**示例**：
```solid
var max: i32 = a > b ? a : b;
var sign: i32 = x > 0 ? 1 : (x < 0 ? -1 : 0);
```

### 后缀表达式

#### 成员访问

```solid
obj.field
```

#### 指针成员访问（unsafe）

```solid
ptr->field    // 等效于 (*ptr).field
```

#### 索引访问

```solid
arr[index]
```

#### 函数调用

```solid
func_name(arg1, arg2, arg3)
```

**示例**：
```solid
var point: Point = Point { x: 1.0, y: 2.0 };
var x: f32 = point.x;
var ptr: *Point = &point;
var y: f32 = ptr->y;
var first: i32 = arr[0];
var result: i32 = add(1, 2);
```

---

## 注解

注解用于在编译期改变编译行为。

**语法**：
```solid
@<名称>
@<名称>(<参数>)
```

**示例**：
```solid
@public
@private
@internal
@extern
@packed
@flags

// 带参数的注解
@deprecated("Use new_func instead")
@align(16)
```

**可注解的元素**：
- 常量声明
- 静态变量声明
- 变量声明
- 结构体声明
- 联合体声明
- 枚举声明
- 函数声明
- 接口声明
- 结构体字段
- 联合体字段
- 枚举字段
- 函数参数

---

## 附录：完整示例

```solid
namespace example::geometry;

using core::math;

// 常量
const PI: f64 = 3.14159265359;

// 接口
interface Shape {
    func area(self: ^Self): f64;
    func perimeter(self: ^Self): f64;
}

// 结构体
struct Point {
    x: f64,
    y: f64,
}

struct Circle {
    center: Point,
    radius: f64,
}

// 实现
func Circle::area(self: ^Circle): f64 {
    return PI * self.radius * self.radius;
}

// 泛型结构体
struct Rectangle<T> where T: Add<T, T> {
    width: T,
    height: T,
}

// 泛型函数
func area<T>(rect: ^Rectangle<T>): T where T: Mul<T, T> {
    return rect.width * rect.height;
}

// 主函数
func main(): i32 {
    var center: Point = Point { x: 0.0, y: 0.0 };
    var circle: Circle = Circle { center: center, radius: 1.0 };

    var a: f64 = circle.area();

    defer print("Done\n");

    if a > 0.0 {
        print("Area is positive\n");
    }

    for var i: i32 = 0; i < 10; i += 1 {
        print(i);
    }

    return 0;
}
```
