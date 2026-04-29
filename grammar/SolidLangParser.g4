parser grammar SolidLangParser;

options{ tokenVocab = SolidLangLexer; }

program: namespace_decl_stmt using_decl_stmt* (
         const_decl_stmt |
         const_static_decl_stmt |
         struct_decl_stmt |
         union_decl_stmt |
         enum_decl_stmt |
         interface_decl_stmt |
         func_decl_stmt
         )* EOF;


// 编译期注解
// 示例：`@private`
// 用于在编译期改变一些编译行为，但是不影响程序的逻辑性
// 比如`@private` `@internal` `@public` 用于设定方法和类型在跨命名空间跨文件时的可见性
// 可以用于注解于常量声明、可变/不可变静态变量声明、变量声明、结构体声明、枚举声明、联合体声明、函数声明、接口声明；
// 也可用于注解 结构体字段、枚举字段、联合体字段、函数参数
annotations: annotation*;
annotation: AT ID (LPAREN (ID | BOOL_LITERAL |  INTEGER_LITERAL | FLOAT_LITERAL) RPAREN)?;


// 命名空间声明
// 示例：`namespace core::math;`
// 必须声明命名空间，如果可执行文件程序入口main函数所在的namespace必须声明当前namespace main
namespace_decl_stmt: NAMESPACE namespace_path SEMI;
namespace_path: ID (SCOPE ID)*;
namespace_prefix: ID (SCOPE ID)* SCOPE;

// 命名空间使用声明
// 示例：`using core::math;`
// 用于简化命名空间的使用
// 没有命名空间使用说明，每次都需要`core::math::Vector3`
// 有此声明后，该程序范围内，可直接`Vector3`
using_decl_stmt: USING namespace_path SEMI;


// 常量声明
// 示例：`@private const ENABLE_DEV_MODE: bool = false;`
// 示例：`const PI = 3.14159;` （类型推断）
// 类型标注可选，未指定时由初始值推断
// 初始值必须在编译期可计算
// 此值不可取地址，不存在运行时中
const_decl_stmt: annotations CONST (COLON type)? EQ expr SEMI;


// 不可变静态变量声明
// 示例：`@private const static VERSION: u32 = 0x01010101u;`
// 示例：`const static VERSION = 0x01010101u;` （类型推断）
// 类型标注可选，未指定时由初始值推断
// 初始值必须在编译期可计算
// 此值为只读，存在程序.rodata中，不可变，但是可取地址
const_static_decl_stmt: annotations CONST STATIC (COLON type)? EQ expr SEMI;


// 结构体声明
// 示例：`struct Vector3 { x: f32, y: f32, }`
// 空结构体示例：`struct None {}`, 不占内存空间
// 泛型示例：`struct Couple<T> where T:IADD<T,T,T> { a: T, b: T }`
struct_decl_stmt: annotations STRUCT ID generic_params? where_clauses? LBRACE struct_fields? RBRACE;
struct_fields: struct_field (COMMA struct_field)* COMMA?;
struct_field: annotations ID COLON type;


// 联合体声明
// 示例：`union Value { as_int: i32, as_float: f32, }`
// 空联合体示例：`union None {}`, 不占内存空间
// 泛型示例：`union Result<T, E> where T: Copy { ok: T, err: E }`
union_decl_stmt: annotations UNION ID generic_params? where_clauses? LBRACE union_fields? RBRACE;
union_fields: union_field (COMMA union_field)* COMMA?;
union_field: annotations ID COLON type;


// 枚举声明
// 示例： `enum WindowFlags { Hidden = 0, Minimized, Maximized}`
// 空枚举示例：`enum NoFlags{}`,具有大小，因为enum本质是一个整型
// 指定整型类型示例：`@flags enum WindowFlags: u32 {Hidden = 0x0001, Minimized = 0x0002, Maximized = 0x0004}`
// 使用示例：`var window_flags: WindowFlags = WindowFlags::Hidden | WindowFlags::Minimized`
// enum的本质是一个整型，默认是i32
enum_decl_stmt: annotations ENUM ID (COLON type)? LBRACE enum_fields? RBRACE;
enum_fields: enum_field (COMMA enum_field)* COMMA?; 
enum_field: annotations ID (EQ expr)?;


// 函数声明
// 示例：`func add(left: i32, right: i32):i32 { return left + right; }`
// 无实现函数示例: `@extern func external(a: i32, b: i32);`,声明外部函数
// 命名空间前缀示例：`func core::math::add(left: i32, right: i32): i32;`
func_decl_stmt: annotations namespace_prefix? func_decl_header (body_stmt | empty_stmt);
func_decl_header: FUNC ID generic_params? LPAREN func_parameters? RPAREN COLON type where_clauses?;
func_parameters: func_parameter (COMMA func_parameter)*;
func_parameter: annotations ID COLON type;

// 接口声明
// 示例：`interface IAdd<TLeft,TRight,TResult>{ func add(left: TLeft, right: TRight):TResult; }`
interface_decl_stmt: annotations INTERFACE ID generic_params? where_clauses? LBRACE interface_fields? RBRACE;
interface_fields: (annotations func_decl_header SEMI)+;

// 泛型参数
generic_params: LT (generic_param (COMMA generic_param)*)  GT;
generic_param: type;

// 泛型约束
where_clauses: where_clause+;
where_clause: WHERE type COLON type;

// 类型
type: namespace_prefix? (named_type | func_type | ref_type | pointer_type | tuple_type | array_type);
named_type: ID;
func_type: FUNC LPAREN (type (COMMA type)*)? RPAREN COLON type;
// 引用类型：^T 为可变引用，!^T 为不可变引用
ref_type: NOT? CARET type;
// 指针类型：*T 为可变指针，!*T 为不可变指针
pointer_type: NOT? STAR type;
tuple_type:  LPAREN (type (COMMA type)*) RPAREN;
array_type: LBRACKET expr RBRACKET type;


// 语句
stmt: empty_stmt |
      body_stmt |
      assign_stmt |
      expr_stmt |
      defer_stmt |
      if_stmt |
      while_stmt |
      for_stmt |
      foreach_stmt |
      switch_stmt |
      break_stmt |
      continue_stmt |
      return_stmt |
      var_decl_stmt |
      static_decl_stmt |
      const_decl_stmt |
      const_static_decl_stmt;

empty_stmt: SEMI;

body_stmt: LBRACE stmt* RBRACE;

assign_stmt: assign_pseudo_expr SEMI;
assign_pseudo_expr: expr (EQ|PLUSEQ|MINUSEQ|STAREQ|SLASHEQ|PERCENTEQ|ANDEQ|OREQ|CARETEQ|SHLEQ|SHREQ) expr;
expr_stmt: expr SEMI;
defer_stmt: DEFER expr SEMI;

if_stmt: IF expr (body_stmt | empty_stmt) (ELSE (body_stmt | empty_stmt) if_stmt*)?;
while_stmt: WHILE expr (body_stmt | empty_stmt);
for_stmt: FOR var_decl_pseudo_expr? SEMI expr? SEMI assign_pseudo_expr? (body_stmt | empty_stmt);
// foreach-in 循环
// 示例：`foreach item in items { process(item); }`
// 示例：`foreach i, item in items { print(i, item); }`
foreach_stmt: FOREACH foreach_vars IN expr (body_stmt | empty_stmt);
foreach_vars: ID (COMMA ID)?;
// switch 语句
// 示例：`switch(value) { 1 => print("one"); 2, 3 => print("two or three"); else => ; }`
switch_stmt: SWITCH LPAREN expr RPAREN LBRACE switch_cases? RBRACE;
switch_cases: switch_case+;
switch_case: switch_labels EQARROW stmt;
switch_labels: switch_label (COMMA switch_label)*;
switch_label: expr | ELSE;
break_stmt: BREAK SEMI;
continue_stmt: CONTINUE SEMI;
return_stmt: RETURN expr? SEMI;

var_decl_pseudo_expr: VAR ID COLON type EQ expr | VAR ID COLON type | VAR ID EQ expr;
var_decl_stmt: annotations VAR (ID COLON type EQ expr | ID COLON type | ID EQ expr) SEMI;
static_decl_stmt: annotations STATIC (ID COLON type EQ expr | ID COLON type | ID EQ expr) SEMI;


// 表达式（优先级从低到高）
expr: conditional_expr;

conditional_expr: or_expr (QUESTION expr COLON conditional_expr)?;

or_expr: and_expr (OROR and_expr)*;
and_expr: bit_or_expr (ANDAND bit_or_expr)*;
bit_or_expr: bit_xor_expr (OR bit_xor_expr)*;
bit_xor_expr: bit_and_expr (CARET bit_and_expr)*;
bit_and_expr: eq_expr (AND eq_expr)*;
eq_expr: cmp_expr ((EQEQ | NOTEQ) cmp_expr)*;
cmp_expr: shift_expr ((LT | GT | LE | GE) shift_expr)*;
shift_expr: add_expr ((SHL | SHR) add_expr)*;
add_expr: mul_expr ((PLUS | MINUS) mul_expr)*;
mul_expr: unary_expr ((STAR | SLASH | MOD) unary_expr)*;

unary_expr
    : MINUS unary_expr
    | NOT unary_expr
    | TILDE unary_expr
    | AND unary_expr
    | STAR unary_expr
    | CARET unary_expr
    | postfix_expr
    ;

postfix_expr: primary_expr (postfix_suffix)*;
postfix_suffix
    : DOT ID
    | MINUSRARROW ID
    | LBRACKET expr RBRACKET
    | LPAREN call_args? RPAREN
    ;

primary_expr
    : literal
    | ID
    | LPAREN expr RPAREN
    | tuple_literal
    | meta_expr
    | struct_literal
    | array_literal
    ;

// 编译期元操作符
// 示例：@sizeof(i32), @offsetof(Point, x), @alignof(f64)
meta_expr: AT ID LPAREN meta_args? RPAREN;
meta_args: meta_arg (COMMA meta_arg)*;
meta_arg: type | expr | ID;

// 元组字面量
// 示例：`()` 空元组
// 示例：`(42,)` 单元素元组（必须有逗号）
// 示例：`(1, 3.14)` 两元素元组
// 注意：`(expr)` 是括号表达式，不是元组
tuple_literal: LPAREN tuple_elements? RPAREN;
tuple_elements: expr COMMA (expr (COMMA expr)* COMMA?)?;

// 结构体字面量
// 示例：`Point { x = 1.0, y = 2.0 }`
// 示例：`core::math::Vector3 { x = 1, y = 2, z = 3 }`
struct_literal: namespace_prefix? ID LBRACE struct_literal_fields? RBRACE;
struct_literal_fields: struct_literal_field (COMMA struct_literal_field)* COMMA?;
struct_literal_field: ID EQ expr;

// 数组字面量
// 示例：`[1, 2, 3]`
// 示例：`[]`
// 示例：`["a", "b", "c"]`
array_literal: LBRACKET (expr (COMMA expr)* COMMA?)? RBRACKET;

literal
    : INTEGER_LITERAL
    | FLOAT_LITERAL
    | STRING_LITERAL
    | CHAR_LITERAL
    | BOOL_LITERAL
    | NULL
    ;

call_args: expr (COMMA expr)*;
