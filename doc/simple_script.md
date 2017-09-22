## 简单的脚本语言
维护个简单的脚本，完善设定的功能，自己平时用用。
比lua弱不少，不支持协程，元表弱化成原型。
1. 基本类型：nil,bool,double,string,table,function,cfunction,userdata,closure
2. 作用域：支持局部作用域、闭包作用域，支持函数上下文。
3. 元表：只支持`__index`操作，只支持table，本身类型为table。
4. userdata：支持`__index`和`__newindex`操作，userdata需要实现这两个接口。
5. 闭包
6. 垃圾回收：看具体实现了，用c#之类的脚本实现，就不用实现了。

一些设计思路：
1. 相比lua，简化实现逻辑，方便维护
    1. 简化功能，主要是元表和gc
2. 扩展
    1. 注册cfunction回调函数
    2. 实现IUserData接口
    3. 利用反射自动注入C#的功能，具体实现类ImportTypeHandler
3. 后续修改
    1. 看情况加些语法糖，如python的数组切片。
    2. vm层面不做大的改动了

### 应用场景
如果只是玩具，有些可惜，有些浪费。
决定当成c#的一个自定义解释器扩展，可用于Unity热更新...

## 注释
把注释方式从`--`改成`//`，然后在vs code上加上`*.ss`的语法高亮支持。

## BNF
```
chunk ::= block

block ::= {stat [";"]}

stat ::=
     "do" block "end" |
     "while" exp "do" block "end" |
     "if" exp "then" block ["elseif" exp "then" block] ["else" block] "end" |
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
     prefixexp |
     "(" exp ")" | unop exp

function ::= "function" funcbody

tableindex ::= "[" exp "]" | "." Name

prefixexp ::= Name {tableindex | args }

var ::= Name [{tableindex | args } tableindex]

funccall ::= Name {tableindex | args } args

args ::=  "(" [explist] ")" | tableconstructor

binop ::= "+" | "-" | "*" | "/" | "^" | "%" | ".." |
     "<" | "<=" | ">" | ">=" | "==" | "!=" |
     "and" | "or"

unop ::= "-" | "not" | "#"
```