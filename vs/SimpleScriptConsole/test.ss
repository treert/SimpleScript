//[[header
    > File Name: test.ss
    > Create Time: 2017-07-13 星期四 11时05分43秒
    > Athor: treertzhu
]]

(function() print("hello world") end)()

function generate_array()
	return 22,33
end

a = {11,generate_array(),}

foreach k,v in a do
	print("foreach k = ", k, "v = ",v)
end

for i = 1,3 do
	if i == 2 then continue end
	print("for i= ",i, " a[i] = ", a[i])
end

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

do
	print("test ...")
	local test_var_arg = function(...)
		return "... = ",...
	end
	print(test_var_arg(1,2,3))
end

do
	print("test ...")
	local test_var_arg = function(...)
		return "... = ",...
	end
	print(test_var_arg(1,2,3))
end

do
	print("test += -= ++ --")
	a[0] = 4
	print("start a[0] = ", a[0]);
	a[0] ++
	print("++    a[0] = ", a[0]);
	a[0] --
	print("--    a[0] = ", a[0]);
	a[0] += 10.5
	print("+=    a[0] = ", a[0]);
	a[0] -= 10.5
	print("-=    a[0] = ", a[0]);
end

print("test module")
a = "global a"
print("a = ", a)
local print = print
module("name.space")
print("enter name.space")
print("a = ", a)
a = "module a"
print("a = ", a)

print("test closure")
a,b = (function()
	local a = 0
	return function()
		a = a+1
		print(a)
	end,function()
		a= a-1
		print(a)
	end
end)()

a()
a()
a()
b()
b()



