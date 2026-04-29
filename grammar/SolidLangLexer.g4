lexer grammar SolidLangLexer;


// Keywords
NAMESPACE: 'namespace';

USING: 'using';

FUNC: 'func';

INTERFACE: 'interface';

STRUCT: 'struct';

ENUM: 'enum';

UNION: 'union';

VAR: 'var';

CONST: 'const';

STATIC: 'static';

IF: 'if';

ELSE: 'else';

WHILE: 'while';

FOR: 'for';

FOREACH: 'foreach';

IN: 'in';

MATCH: 'match';

SWITCH: 'switch';

BREAK: 'break';

CONTINUE: 'continue';

RETURN: 'return';

DEFER: 'defer';

WHERE: 'where';

ASYNC: 'async';

AWAIT: 'await';


// Operators
PLUS: '+';

MINUS: '-';

NOT: '!';

TILDE: '~';

STAR: '*';

SLASH: '/';

MOD: '%';

EQEQ: '==';

NOTEQ: '!=';

LT: '<';

GT: '>';

LE: '<=';

GE: '>=';

OROR: '||';

ANDAND: '&&';

AND: '&';

OR: '|';

CARET: '^';

SHL: '<<';

SHR: '>>';

EQ: '=';

PLUSEQ: '+=';

MINUSEQ: '-=';

STAREQ: '*=';

SLASHEQ: '/=';

PERCENTEQ: '%=';

ANDEQ: '&=';

OREQ: '|=';

CARETEQ: '^=';

SHLEQ: '<<=';

SHREQ: '>>=';


// Seperators
SCOPE: '::';

COLON: ':';

QUESTION: '?';

COMMA: ',';

DOT: '.';

SEMI: ';';

EQARROW: '=>';

MINUSRARROW: '->';

AT: '@'; 

LBRACE: '{';

RBRACE: '}';

LBRACKET: '[';

RBRACKET: ']';

LPAREN: '(';

RPAREN: ')';


// Literals
STRING_LITERAL: '"' (~["\\] | ESCAPE_SEQ)* '"';

CHAR_LITERAL: '\'' (~['\\] | ESCAPE_SEQ)+ '\'';

BOOL_LITERAL: 'true' | 'false';

NULL: 'null';

INTEGER_LITERAL: ( DEC_LITERAL | BIN_LITERAL | OCT_LITERAL | HEX_LITERAL) INTEGER_SUFFIX?;

FLOAT_LITERAL: ((DEC_LITERAL+ '.' DEC_LITERAL+ FLOAT_EXPONENT?)
             | (DEC_LITERAL+ FLOAT_EXPONENT)) FLOAT_SUFFIX?;

fragment DEC_LITERAL:      '0' | [1-9] ('_'? DEC_DIGIT)*;

fragment HEX_LITERAL: '0x' HEX_DIGIT ('_'? HEX_DIGIT)*;

fragment OCT_LITERAL: '0o' OCT_DIGIT ('_'? OCT_DIGIT)*;

fragment BIN_LITERAL: '0b' BIN_DIGIT ('_'? BIN_DIGIT)*;

fragment FLOAT_EXPONENT: [eE] [+-]? DEC_LITERAL;

fragment ESCAPE_SEQ: '\\' [nrt"'\\];

fragment DEC_DIGIT: [0-9];

fragment HEX_DIGIT: [0-9a-fA-F];

fragment OCT_DIGIT: [0-7];

fragment BIN_DIGIT: [01];

fragment INTEGER_SUFFIX:'u8' | 'u16' | 'u32' | 'u64' | 'u128' | 'usize'
                       |'i8' | 'i16' | 'i32' | 'i64' | 'i128' | 'isize';

fragment FLOAT_SUFFIX: 'f16' | 'f32' | 'f64' | 'f128';


// Identifier
ID  : [a-zA-Z_][a-zA-Z0-9_]*;


// Whitespaces & Comments
WHITESPACE: [ \t\r\n]+ -> channel (HIDDEN);

Doc_comment: '///' ~[\n\r]*;

Line_comment: '//' ~[\n\r]* -> channel (HIDDEN);


