## 自己维护个脚本语言

1. 手写词法解析语法解析
2. 使用C#编写，不用处理GC
3. 语法参考lua，实现参考luna和unilua

## BNF

chunk ::= block

block ::= {stat [";"]} 

stat ::=  varlist "=" explist | 
     exp | 
     "do" block "end" | 
     "while" exp "do" block "end" | 
     "repeat" block "until" exp | 
     "if" exp "then" block {"elseif" exp "then" block} ["else" block] "end" | 
     "for" Name "=" exp "," exp ["," exp] "do" block "end" | 
     "for" namelist "in" explist "do" block "end" | 
     "function" funcname funcbody |
     "local" "function" Name funcbody | 
     "local" namelist ["=" explist] |
     "return" [explist] |
     "break" |
     "continue"

namelist ::= Name {"," Name}

varlist ::= var {"," var}

global ::= $"["exp"]" | $Name

var ::= (global | Name) | 
     (global | Name) {"[" exp "]" | "." Name | args | ":" Name args} ( "[" exp "]" | "." Name )  

args ::=  "(" [explist] ")" | tableconstructor 

funcname ::= Name {"." Name} [":" Name]

function ::= "function" funcbody

funcbody ::= "(" [parlist] ")" block "end"

parlist ::= namelist ["," "..."] | "..."

explist ::= {exp ","} exp

exp ::= mainexp | exp binop exp 

mainexp ::= nil | false | true | Number | String | 
     "..." | function | tableconstructor | 
     prefixexp |
     unop exp|

prefixexp ::= var |
     (global | Name) {"[" exp "]" | "." Name | args | ":" Name args} |
     (exp) {"[" exp "]" | "." Name | args | ":" Name args}

tableconstructor ::= "{" [fieldlist] "}"

fieldlist ::= field {fieldsep field} [fieldsep]

field ::= "[" exp "]" "=" exp | Name "=" exp | exp

fieldsep ::= "," | ";"

binop ::= "+" | "-" | "*" | "/" | "^" | "%" | ".." | 
     "<" | "<=" | ">" | ">=" | "==" | "~=" | 
     "and" | "or"

unop ::= "-" | "not" | "#"