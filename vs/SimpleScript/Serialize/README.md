# 自定义的序列化工具库
记录下过程

## 使用说明
例子
```C#
ClassA data = new ClassA();// 实际还依赖X1,X2两个类，ClassA只知道接口IX

XSerializer serialzie = new XSerializer(
    typeof(ClassA), typeof(X1), Typeof(x2)
    );

Stream stream = new MemoryStream();
xx.Serialize(stream, data);

stream.Seek(0, SeekOrigin.Begin);
ClassA get_data = xx.Deserialize<ClassA>(stream);

```
使用限制：
1. 不支持类的静态字段（差不多所有的都不支持的）
2. 泛型容器
    1. 当前只实现了`List<>`,`Dictionary<,>`，实现新的很容易
    2. 通用的Class序列化一些泛型，因为泛型实例化后也是类
        1. `List<>`通用序列化会增加体积
        2. `Dictionary<,>`报错


方便的地方：
1. 不需要对类使用`[Serializable]`特性描述。
2. 支持循环引用。
3. 支持接口

使用注意的地方，**最好不要这么用**：
1. 字段使用`NotSerialized`，反序列化后是未初始化状态，也就是0或null。
2. 如果使用了多态引用，需要额外声明多态信息，**否则序列化时保存，类型不匹配**。

实现细节：
1. 引用类型额外多一个byte，表明：1. 是否是null 2. 是否复用了 3. 是否是新的
2. 多态引用类型多一个bype，表明当前是具体那个类型，**多态信息需要额外声明**
3. 序列化数据开头有个int的hash值。类型对应的多态信息，也参与了hash。

## 前因后果
SimpleScript需要序列化编译后的Function保存文件，找到c#标准库里的BinaryFomatter。
使用BinaryFomatter序列化对象时，发现体积增大到6倍，而luac只有1.5倍，不能接受。
网上和github上找到个序列化源码：https://github.com/tomba/netserializer 。
这个源码有些叼，细节都用到了`Microsoft 中间语言 (MSIL) 指令`，就不懂了。
于是决定参考netserializer，用反射写个可控的序列化工具库。

### ...
花了四天时间在这个工具上面。也不能浪费了。
保存成个人库，后续再加上xml的支持。

## 相关知识记录

特殊的函数：
1. FormatterServices.GetUninitializedObject 创建未初始化对象，**不调用构造函数**。
    - Activator.CreateInstance 会调用构造函数，也就要求类型有公开的构造函数
2. MethodInfo.MakeGenericMethod 调用泛型函数是绑定类型，**注意：这个返回新的MethodInfo，原来的不变**。
3. Type.IsArray 针对的是`int[]`这种类型的，`List<int>`返回`false`。跟诡异的是
    - `typeof(Array).IsArray` == `false`
    - `Array.CreateInstance(typeof(int),0).GetType().IsArray` == `true`
4. Array.IndexOf 数组类型用这个查询索引。
5. Array.CreateInstance 可以创建多维数组，但是需要强制转换。

零散的知识点：
1. Type.IsSerializable 类型是否具有`[Serializable]`特性。
2. FieldInfo.IsNotSerialized 字段是否具有`[NotSerialized]`特性。
3. 引用类型判断, !Type.IsValueType【注意IsByRef是用来判断函数参数的】
    > https://stackoverflow.com/questions/16578609/why-does-type-isbyref-for-type-string-return-false-if-string-is-a-reference-type/16578825 

4. 调用泛型方法有时候不需要指定模版参数类型，如Array.IndexOf，因为编译时就知道类型了。
    > https://stackoverflow.com/questions/4975364/c-sharp-generic-method-without-specifying-type 


需要区分的地方：
1. 字段`FieldInfo`和属性`PropertyInfo`的区别：
    1. 字段是实际存储的值，序列化和反序列化用这个
    2. 属性是对字段的读写封装，包含自定义的`set,get`。
2. `==`和`Equals`的区别：
    1. `==`是比较值类型的，两个类对象要引用同一个对象才为true。
        但是`string`类型不同，string的`==`会比较实际内容，等同`Equals`。
        **注意是直接定义成string类型的变量，而不是object类型的变量保存string的引用**。
    2. **相等性比较水有些深啊** http://www.cnblogs.com/souliid/p/5718968.html
3. `FieldInfo`的两个方法`SetValue`和`SetValueDirect`的区别：
    - SetValue不能给值类型赋值，因为参数类型是object，会有一次装箱，然后就与原来的值无关了。
    - 值类型修改需要其他方法
        1. 使用了`TypedReference`和`SetValueDirect`。
            1. http://bbs.csdn.net/topics/390345584 写法有些麻烦
            2. `TypedReference tf = __makeref(struct_obj); field.SetValueDirect(tf,"xx")`
                > https://stackoverflow.com/questions/1711393/practical-uses-of-typedreference 

        2. 值装箱成对象，修改对象后，将对象装好成值。
            http://bbs.csdn.net/topics/391011879 **写起来简单**


不懂，但目前没踩坑的地方，不需要关注：
1. SecurityException msdn上到处都在说这个异常，好像是访问资源的权限什么的。