﻿# MyScript
准备留下自己用，类似lua，runtime 寄生在 C# 上，开发个类似Emmylua的Intellij插件来作为IDE（也想在VSCode上支持来着，看情况吧）。 

## 语法
见[MyScript.bnf](MyScript.bnf)

一些选择：
- 在注释符号的选择上有些纠结，三个候选：`--`、`//`、`#`，现在选择的`--`。
  - 选择`--`的理由之一是输入方便，不受中英文切换影响。很想用`#`的说，但输入不方便。

一些取舍
- `++`，`--`不打算支持。并且`--`现在被用作注释符。
- 没有特殊的语法支持切片和多维数组，内置的容器类型就hashtable和一维array。
  - 像Python就有这种支持，想了想还是用扩展函数的方式支持吧。

一些未定的
- 如何处理对类的支持，想了想，做了些决定
  - 内置table就是简单的一层hashtable，不打算像lua或js那样搞个特殊支持。
    - 有一层缺点，不能监控全局表的读写。
  - 预留`class`和`object`，按现在的功能设计，也可以采用扩展的方式支持。
  - 等到真的用到时，再看吧。现在只是打算用作个人胶水脚本。
- API的命名规范有些个纠结，想了想，像python一样，先全部小写好了。等到具体实现时再说。
  - 有那么一刻，都想实现成大小写不敏感的了。

## 一些设计上的想法
- 保持简单易读，语法上近似lua
  - 算术运算的结果就是Number，逻辑运算的结果就是bool。**不搞运算符复用。**
  - 不搞太多运算符号（符号型语法糖）。
    - 像整除模除逻辑非用的是单词`div`/`mod`/`not`。感觉比`//`/`%`/`!`好读一些，而且符号的键盘输入不友好。
    - 【个人感觉：】语法糖多了后，可读性似乎下降了.难道年龄大了，接受能力变差了?
  - 关键字多用全拼
    - 主要的关键字里就一个`function`缩写成`fn`。
- 功能
  - **运行方式是在语法树上解释执行，限制了很多功能：调试、协程。**
    - 非常遗憾，做了这样的选择，完全抛弃了性能之类的。
  - 打算寄生在c#(.net core)上，方便扩展。
    - 许多功能不打算做语法支持，如：多维数组、切片、正则表达式等，但是用函数扩展的方式也能很好实现。
  - 原生支持BigInteger，作为计算器来说，功能强一点的好。这方便python也支持。
- 语法功能向
  - 语法上多是借鉴其他语言，难保以后又看到什么好的语法糖，又想加上。
    - 比如C#的`using(xxx){}`，就加上了一个`scope(xxx){}`。
      - c#还有个`async await yield`也挺好了，奈何MyScript打算直接在语法树上解释执行，加不上。
  - 关于语法扩展。
    - 开始时打算支持语法层面的扩展比如`#cmd-name{ cmds }`，想了想预留下来。这也是没选`#`做注释符的原因之一。
    - 想法感觉挺香的，不过一想到IDE也要同步支持，隐约觉得头大。
- 语法设计向
 - 语句和表达式严格分开。
   - 赋值语句是特殊语句，不是表达式，像lua一样。功能弱了很多，这也是不打算支持`++`、`--`的原因之一。
   - `return`/`throw`/`break`/`continue`可以混在`?`运算的后面。算是特殊语法，看到`kotlin`这么用的，感觉挺好的。
 - 执行block都是`{`/`}`开头结尾的，`if`语句也一样，算是强制区分代码块和单语句。