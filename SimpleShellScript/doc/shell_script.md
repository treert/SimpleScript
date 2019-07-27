## 又一个简单脚本语言
鉴于之前的脚本语言写好后，一直闲置，这个以使用为目标。目前最大的应用，是写些本地执行的脚本。
大致的设计倾向
1. 类似lua，语法简洁明了，好上手。
	- 不追求语法糖什么的
2. 核心是慢慢积累些好用的标准库。
	- 这个自己逐渐加，功能还是建立在C#之上的，不过接口自己整理下。
	- 主要是常见的文件操作呀，字符串处理呀，简单计算呀，啥的。
3. 集成bash
	- 提供方便的语法，和bash之类的shell做交互。
		- 其实也可以就简单的`bash "ls -l"`这种。
4. 讨巧实现
	- 不打算用字节码虚拟机了，直接执行SyntaxTree。
		- 产生的问题时，栈依赖C#，不能实现协程之类。
	- 基本值类型，还是继续使用成c#的Object，不追求性能啥的。
	
## BNF
```
module ::= block

block ::= {stat [";"]}

stat ::=
     "do" block "end" |
     "while" exp "do" block "end" |
     "if" exp "then" block {"elseif" exp "then" block} ["else" block] "end" |
     "for" Name "=" exp "," exp ["," exp] "do" block "end" |
     "foreach" Name ["," Name] "in" exp "do" block "end" |
     "function" funcname funcbody |
     "local" "function" Name funcbody |
     "local" namelist ["=" explist] |
     "return" [explist] |
     "break" |
     "continue" |
     varlist "=" explist |
     funccall |
     var += exp |
     var ++ |
     var -= exp |
     var --

namelist ::= Name {"," Name}

varlist ::= var {"," var}

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
     prefixexp | unop exp

function ::= "function" funcbody

tableindex ::= "[" exp "]" | "." Name

prefixexp ::= (Name | "(" exp ")") {tableindex | args }

var ::= Name [{tableindex | args } tableindex]

funccall ::= Name {tableindex | args} [String]

args ::=  "(" [explist] ")"

binop ::= "+" | "-" | "*" | "/" | "^" | "%" | ".." |
     "<" | "<=" | ">" | ">=" | "==" | "!=" |
     "and" | "or"

unop ::= "-" | "not" | "#"

```