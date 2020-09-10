# SimpleScript
## [0.0.7] - 2020-09-10
### update
- 更新下插件。开发调试时发现会在单步执行时无故卡死，不知为何。

## [0.0.6] - 2017-09-03
### mod
- 修改下语法，不支持`a:f()`，替换`self`语法糖为`this`。
  `this`参数为函数的第0个参数。`a.f()`里`this`是`a`，`f()`里`this`是`nil`。

## [0.0.5] - 2017-09-03
### doc
- 补充点说明

## [0.0.4] - 2017-09-03
### Fixed
- 使用反射导出c#功能的模块支持导出虚拟类，主要是里面的静态函数有用，如`System.IO.File.ReadAllText`。

## [0.0.3] - 2017-09-03
### Init
- 开始维护个修改日志，现在已经可以用vscode调试ss了。

# Change Log
All notable changes to the "simplescript" extension will be documented in this file.

Check [Keep a Changelog](http://keepachangelog.com/) for recommendations on how to structure this file.
