# simplescript
SimpleScript是自己写的类似lua的脚本语言，简称ss。有些不同的地方

这个是ss的支持插件，包含语法高亮、sinppet和调试功能

## 调试功能

根目录下建`test.ss`。
F5使用vs的调试功能，里面找名字里带`SimpleScript`的，根据提示添加launch.json配置。

一个内容例子
```
// This is comment, test code
local DateTime = import("System.DateTime")

print("Today is ", DateTime.Now)

function generate_array()
	return 22,33
end

a = {11,generate_array(),}

print("test foreach")

foreach k,v in a do
	print("foreach k = ", k, "v = ",v)
end

print("test for")
for i = 1,3 do
	if i == 2 then 
		print ("for continue")
		continue
	end
	print("for i= ",i, " a[i] = ", a[i])
end

print("test for-in")
local function ipairs(table)
	return function(table, idx)
		idx = idx + 1
		if (table[idx] == nil) then
			return nil
		else
			return idx,table[idx]
		end
	end, table, 0
end

for k,v in ipairs(a) do
	print("for in k = ", k, "v = ",v)
end
```