## 自己维护个脚本语言

1. 手写词法解析语法解析
2. 使用C#编写，不用处理GC
3. 语法参考lua，实现参考luna和unilua

## BNF
```
chunk ::= block

block ::= {stat [";"]}

stat ::=
     "do" block "end" |
     "while" exp "do" block "end" |
     "if" exp "then" block {"elseif" exp "then" block} ["else" block] "end" |
     "for" Name "=" exp "," exp ["," exp] "do" block "end" |
     "foreach" Name ["," Name] "in" exp "do" block "end" |
     "for" namelist "in" explist "do" block "end" |
     "function" funcname funcbody |
     "local" "function" Name funcbody |
     "local" namelist ["=" explist] |
     "return" [explist] |
     "break" |
     "continue" |
     varlist "=" explist |
     Name {tableindex | funccall} funccall

namelist ::= Name {"," Name}

varlist ::= var {"," var}

var ::= Name [{tableindex | funccall} tableindex]

funcname ::= Name {"." Name} [":" Name]

funcbody ::= "(" [parlist] ")" block "end"

parlist ::= Name {"," Name} ["," "..."] | "..."

explist ::= {exp ","} exp

tableconstructor ::= "{" [fieldlist] "}"

fieldlist ::= field {fieldsep field} [fieldsep]

field ::= "[" exp "]" "=" exp | Name "=" exp | exp

fieldsep ::= "," | ";"

exp ::= mainexp | exp binop exp

mainexp ::= nil | false | true | Number | String |
     "..." | function | tableconstructor |
     prefixexp |
     unop exp|

function ::= "function" funcbody

prefixexp ::= (Name | "(" exp ")") {tableindex | funccall}

tableindex ::= "[" exp "]" | "." Name

funccall ::= args | ":" Name args

args ::=  "(" [explist] ")" | tableconstructor

binop ::= "+" | "-" | "*" | "/" | "^" | "%" | ".." |
     "<" | "<=" | ">" | ">=" | "==" | "~=" |
     "and" | "or"

unop ::= "-" | "not" | "#"
```
