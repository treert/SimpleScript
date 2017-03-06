local a = 1+2;

print(a,1+2,"hello world");

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