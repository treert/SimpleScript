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

## 一些记录
### 2020-01-08
本来打算使用赋值null，来当成删除的。然而发觉有些儿问题，有的时候就是要设置成null，比如module级变量初始值null。
提供个函数好了`delete(table, key) or delete(name)`

### 2020-01-17
想了想，还是要支持一套反射系统，总不至于对Dictionary,List之类一直特判。

## BNF
```
module ::= stats

stats ::= {stat [";"]}

block ::= "{" stats "}"

stat ::=
     block |
     "while" exp block |
     "if" exp block {"elseif" exp block} ["else" block]|
     "for" Name "=" exp "," exp ["," exp] block |
     "foreach" Name ["," Name] "in" exp block |
	 "for" block |
	 "try" block ["catch" [Name] block] | 
     "fn" funcname funcbody |
     ("global"|"local") "fn" Name funcbody |
     ("global"|"local") namelist ["=" explist] |
     "return" [explist] |
     "break" |
     "continue" |
     varlist "=" explist |
     funccall |
     var self_assign exp|
     var self_op

self_assign ::= "+=" | "-=" | ".="

self_op ::= "++" | "--"

namelist ::= Name {"," Name}

varlist ::= var {"," var}

funcname ::= Name {"." Name} [":" Name]

funcbody ::= "(" [parlist] ")" block

parlist ::=  {Name ","} ["*" [Name]]

explist ::= {exp ","} exp

arrayconstructor ::= "[" {exp ","}"]"

tableconstructor ::= "{" {field ","} "}"

field ::= "[" exp "]" "=" exp | Name "=" exp | exp

exp ::= mainexp | exp binop exp | unop exp| ( exp ) | exp ? exp [: exp]

mainexp ::= nil | false | true | Number | String |
     "..." | function | tableconstructor |
     mainexp {tableindex | args }

function ::= "fn" funcbody

tableindex ::= "[" exp "]" | "." Name

tailexp ::= mainexp {tableindex | args }

var ::= Name [{tableindex | args } tableindex]

funccall ::= Name {tableindex | args} [String]

args ::=  "(" {exp ","} ["*" Name ","] {Name ":" exp ","} ")"

binop ::= "+" | "-" | "*" | "/" | "^" | "%" | ".." |
     "<" | "<=" | ">" | ">=" | "==" | "!=" |
     "and" | "or"

unop ::= "-" | "not"

```