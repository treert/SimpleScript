//[[header
    > File Name: test.ss
    > Create Time: 2017-07-23 星期日 15时48分51秒
    > Athor: treertzhu
    > -----
    > Last Modified: 2017-09-22 星期五 22时26分13秒
    > Modified By: treertzhu
    > -----
]]


local int64 = import("System.Int64")
local int32 = import("System.Int32")
local DateTime = import("System.DateTime")
import("SimpleScript.Instruction","Instruction");
// print(Instruction);

a = Instruction.new(10,12);
a.SetBx(123);
print(a.GetOp(),a.GetBx());

local a = int32.new()
local b = int64.new()

// print(DateTime)
local date = DateTime.Now;
local date2 = DateTime.new();
date2 = date2.AddDays(12);
print(date, date2)

print(a,int64.MinValue);
//[[]]

// 这个语法不支持了
// (function() print("hello world") end)()

function generate_array()
    return 22,33
end

a = {11,generate_array(),}

a.xx = "xx"

print(a.xx)

foreach k,v in a do
    print("foreach k = ", k, "v = ",v)
end

for i = 1,3 do
    if i == 2 then
        print("for i=2 meet continue")
        continue
    end
    print("for i= ",i, " a[i] = ", a[i])
end

local function ipairs(table)
    return function(idx)
        idx = idx + 1
        if (this[idx] == nil) then
            return nil
        else
            return idx,this[idx]
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

print ("test coroutine")

producer = coroutine.create(function(x)
    x = coroutine.yield(1,x)
    x = coroutine.yield(2,x)
    x = coroutine.yield(3,x)
    return 4,x
end)

function consumer()

    print(coroutine.resume(producer,100))
    print(coroutine.resume(producer,200))
    print(coroutine.resume(producer,300))
    print(coroutine.resume(producer,400))
    print(coroutine.resume(producer,500))
end

if producer then
    consumer()
end

coroutine.resume(
coroutine.create(function()
    print("儿子1 睡4秒")
    print(coroutine.sleep(4000));
    print("儿子1 醒过来了");
end));

coroutine.resume(
coroutine.create(function()
    print("儿子2 睡2秒")
    print(coroutine.sleep(2000));
    print("儿子2 醒过来了");
end));

async do 
    print("儿子3 睡1秒")
    print(coroutine.sleep(1000));
    print("儿子3 醒过来了");
end

local t = function ()
    print("儿子4 睡0.5秒")
    print(coroutine.sleep(500));
    print("儿子4 醒过来了");
end

async t();

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

