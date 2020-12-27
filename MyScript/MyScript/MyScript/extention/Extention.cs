/*
 * 支持扩展
 * 1. 往 SS 里注入Api
 * 
 * 一些实现细节：
 * 注意的api的名字最好不要用 __ 开头，__ 开头的预留特殊用途，现有的在ExtUtils 里看。
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace MyScript
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class ExtFuncAttribute : Attribute
    {
        public string name;
        public string tip;// 帮助文档
    }

    // 增加这个的原因是，一个函数可以即导出成
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class ExtGlobalFuncAttribute : ExtFuncAttribute
    {
        //public string name;
        //public string tip;// 帮助文档
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public class ExtFieldAttribute : Attribute
    {
        public string name;
        public string tip;// 帮助文档
        public bool only_read = false;
    }
    
    public static class ExtConfig
    {
        public const string key_tostring = "__tostring";// 格式化成字符串，带一个字符串参数
    }


    public static class ExtSomeApi
    {
        public static T GetValueOrDefault<T>(this List<T> ls, int idx)
        {
            if(ls != null && idx < ls.Count)
            {
                return ls[idx];
            }
            return default(T);
        }
        public static V GetValueOrDefault<K,V>(this Dictionary<K,V> dict, K key)
        {
            V val;
            dict.TryGetValue(key, out val);
            return val;
        }

        //public static object GetValueOrDefault(this IDictionary dict, object key)
        //{
        //    return dict[key];
        //}

        public static object GetByIdx(this IList ls, int idx)
        {
            if (ls.Count == 0) return null;// 没得办法
            // 对索引做循环处理
            idx = ((idx % ls.Count) + ls.Count) % ls.Count;
            return ls[idx];
        }

        public static void SetByIdx(this IList ls, int idx, object val)
        {
            if(Math.Abs(idx) >= ls.Count)
            {
                throw new Exception($"IList.SetByIdx out of range, Count={ls.Count}, idx={idx}");
            }
            idx = (idx + ls.Count) % ls.Count;
            ls[idx] = val;
        }

        public static object GetByIdx(this Array arr, int idx)
        {
            if (arr.Length == 0) return null;// 没得办法
            // 对索引做循环处理
            idx = ((idx % arr.Length) + arr.Length) % arr.Length;
            return arr.GetValue(idx);// arr.Rank == 1 or will throw exception
        }

        public static void SetByIdx(this Array arr, int idx, object val)
        {
            if (Math.Abs(idx) >= arr.Length)
            {
                throw new Exception($"Array.SetByIdx out of range, Length={arr.Length}, idx={idx}");
            }
            idx = (idx + arr.Length) % arr.Length;
            arr.SetValue(val, idx);
        }

        public static object GetByIdx(this string str, int idx)
        {
            if (str.Length == 0) return null;
            idx = ((idx % str.Length) + str.Length) % str.Length;
            return str[idx];
        }

        public static object CallWithThisAndReturnOne(this ICall func, object that, params object[] objs)
        {
            Args args = new Args(objs);
            args.that = that;
            var ret = func.Call(args);
            return ret.GetValueOrDefault(0);
        }
    }

    // todo 这个大概要做VM级别的隔离
    public static class ExtUtils
    {
        static Dictionary<Type, ExtContain> ext_types = new Dictionary<Type, ExtContain>();

        public static void Register(VM vm, ExtFuncWrap func_wrap)
        {
            var that_type = func_wrap.that_type;
            if(that_type == null)
            {
                // global, todo split name
                vm.global_table[func_wrap.name] = func_wrap;
            }
            else
            {
                ExtContain contain;
                if (ext_types.TryGetValue(func_wrap.that_type, out contain) == false)
                {
                    contain = new ExtContain(func_wrap.that_type);
                    ext_types.Add(func_wrap.that_type, contain);
                }
                contain.Register(func_wrap.name, func_wrap);
            }


        }

        // 注册类里面的函数
        public static void Import(VM vm, Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
            foreach(var method in methods)
            {
                foreach(var attr in method.GetCustomAttributes<ExtFuncAttribute>())
                {
                    var f = new ExtFuncWrap(method, attr);
                    Register(vm, f);
                }
            }
            // todo 
        }

        public static string ToString(object obj, string format, int len)
        {
            string ret;
            if(obj == null)
            {
                ret = "";
            }
            if(obj is string)
            {
                ret = obj as string;// string 保持不变
            }
            else
            {
                // 尝试调用扩展的tostring
                ExtContain ext;
                if(ext_types.TryGetValue(obj.GetType(), out ext))
                {
                    var func = ext.Get(obj, ExtConfig.key_tostring) as ExtFuncWrap;
                    if(func != null)
                    {
                        ret = func.CallWithThisAndReturnOne(obj) as string;
                        // @om 不做检查，如果返回null，下面会挂掉，不要那么搞。
                    }
                    else
                    {
                        ret = obj.ToString();
                    }
                }
                else
                {
                    ret = obj.ToString();
                }
            }

            // padding
            if(len > ret.Length)
            {
                ret = ret.PadLeft(len - ret.Length);
            }
            else if(len < -ret.Length)
            {
                ret = ret.PadRight(-len - ret.Length);
            }
            return ret;
        }

        public static object Get(object obj, object key)
        {
            if (obj == null || key == null) return null;

            if(obj is IGetSet)
            {
                return (obj as IGetSet).Get(key);
            }
            else if(obj is IDictionary)
            {
                return (obj as IDictionary)[key];
            }
            else if (obj is IList)
            {
                var ls = obj as IList;
                double f = Utils.ConvertToPriciseDouble(key);
                int d = (int)f;
                if (d == f)// 这样的判断能兼容 double.NAN
                {
                    return ls.GetByIdx(d);
                }
            }
            else if(obj is Array)
            {
                var arr = obj as Array;
                if(arr.Rank == 1 && arr.GetLowerBound(0) == 0)
                {
                    double f = Utils.ConvertToPriciseDouble(key);
                    int d = (int)f;
                    if (d == f)// 这样的判断能兼容 double.NAN
                    {
                        return arr.GetByIdx(d);
                    }
                }
            }
            else if(obj is string)
            {
                double f = Utils.ConvertToPriciseDouble(key);
                int d = (int)f;
                if (d == f)// 这样的判断能兼容 double.NAN
                {
                    return (obj as string).GetByIdx(d);
                }
            }
            else if(obj is ExtContain)
            {
                return (obj as ExtContain).Get(null, key);
            }
            else{
                ExtContain ext;
                ext_types.TryGetValue(obj.GetType(), out ext);
                if(ext != null)
                {
                    return ext.Get(obj, key);
                }
            }
            return null;
        }

        public static void Set(object obj, object key, object val)
        {
            if (obj == null || key == null) return;

            if(obj is Table)
            {
                (obj as Table).Set(key, val);
            }
            else if(obj is IDictionary)
            {
                (obj as IDictionary)[key] = val;// 类型不对，会抛异常
            }
            else if(obj is IList)
            {
                var ls = obj as IList;
                double f = Utils.ConvertToPriciseDouble(key);
                int d = (int)f;
                if (d == f)// 这样的判断能兼容 double.NAN
                {
                    ls.SetByIdx(d, val);
                }
            }
            else if (obj is Array)
            {
                var arr = obj as Array;
                if (arr.Rank == 1 && arr.GetLowerBound(0) == 0)
                {
                    double f = Utils.ConvertToPriciseDouble(key);
                    int d = (int)f;
                    if (d == f)// 这样的判断能兼容 double.NAN
                    {
                        arr.SetByIdx(d, val);
                    }
                }
            }
            else if(obj is ExtContain)
            {
                (obj as ExtContain).Set(null, key, val);
            }
            else
            {
                ExtContain ext;
                ext_types.TryGetValue(obj.GetType(), out ext);
                if (ext != null)
                {
                    ext.Set(obj, key, val);
                }
                else
                {
                    throw new Exception($"{obj.GetType().Name} does not have any wrap for write");
                }
            }
        }
    }

    /// <summary>
    /// ExtWrap的容器，以类为单位，静态和对象的分离开（分离开，也意味着静态和对象的名字可以重复）。
    /// - 静态的部分直接注入ExtWrap到全局里好了，相当于与类脱离了。
    /// 这儿使用扁平结构，不使用类的继承结构。@om 后续版本可以考虑要不要改改
    /// </summary>
    public class ExtContain
    {
        Type type;
        Dictionary<string, ExtWrap> wraps = new Dictionary<string, ExtWrap>();

        public ExtContain(Type type)
        {
            this.type = type;
        }

        public void Register(string name, ExtWrap wrap)
        {
            // todo check name
            wraps[name] = wrap;
        }

        public object Get(object obj,object key)
        {
            Debug.Assert(key != null);
            // @om 这儿做些特殊处理，支持类似 __index 之类的操作。2.0再看看吧。

            string name = key as string;
            if(name == null)
            {
                throw new Exception("wrap only support string key now");
            }
            ExtWrap wrap;
            wraps.TryGetValue(name, out wrap);
            if(wrap == null)
            {
                throw new Exception($"{type.Name} does not has a wrap called '{name}'");
            }
            return wrap.Get(obj);
        }

        public void Set(object obj, object key, object value)
        {
            Debug.Assert(key != null);

            string name = key as string;
            if (name == null)
            {
                // @om 这儿做些特殊处理，支持类似 __newindex 之类的操作。
                throw new Exception("wrap only support string key now");
            }
            ExtWrap wrap;
            wraps.TryGetValue(name, out wrap);
            if (wrap == null)
            {
                throw new Exception($"{type.Name} does not has a wrap called '{name}'");
            }
            wrap.Set(obj, value);
        }
    }


    /// <summary>
    /// 扩展包装
    /// 需要支持的情况
    /// 1. 对象属性读写【特别是读的时候，需要执行返回结果】
    /// 2. 对象方法调用
    /// 3. 静态属性读写
    /// 4. 静态方法调用
    /// </summary>
    public abstract class ExtWrap
    {
        public virtual object Get(object obj)
        {
            return this;// 对于方法而言，返回自己，等待被调用
        }

        public virtual bool Set(object obj, object value)
        {
            // 只有属性支持这个
            throw new Exception("I am not a field, can not be set.");
        }

        // 方法被调用
        public virtual List<object> Call(Args args)
        {
            throw new Exception("I am not a function, can not be call.");
        }

        public string name;// 用途：错误提示，文档生成。如果是全局函数，name的格式可以是 {Name.}*Name
        public string tip;// 用途：文档生成

        public static object CheckAndConvertFromSSToSS(object obj, Type target_type)
        {
            if (obj == null)
            {
                if (target_type.IsValueType)
                {
                    throw new Exception($"{target_type} is ValueType, can not assign from null");
                }
                return null;
            }
            
            if (target_type.IsInstanceOfType(obj))
            {
                return obj;
            }

            // @om 一些特殊处理
            // Enum
            if (target_type.IsEnum)
            {
                if(obj is double)
                {
                    var f = (double)obj;
                    int d = (int)f;
                    if(d == f)
                    {
                        obj = d;// convert to int
                    }
                    else
                    {
                        throw new Exception($"Only support int32 when assign double to Enum, {f} is not valid");
                    }
                }
                if (Enum.IsDefined(target_type, obj) == false)// IsDefined 本身也会抛异常
                {
                    throw new Exception($"{obj} out of Enum {target_type}");
                }
                return Enum.ToObject(target_type, obj);
            }
            // 对数字的相互转换做特殊处理
            if (obj is double)
            {
                double f = (double)obj;
                if(target_type == typeof(float))
                {
                    return (float)f;
                }
                else
                {
                    long d = (long)f;
                    if(d == f)
                    {
                        // @om 这儿要不要做什么扩展呢？
                        return Convert.ChangeType(d, target_type);
                    }
                    else
                    {
                        throw new Exception($"{f} is not Integer,{target_type} can not covert from integer");
                    }
                }
            }

            throw new Exception($"{obj.GetType()} can not assgin to {target_type}");
        }
        
    }

    /// <summary>
    /// 扩展C#方法，支持对象方法和静态方法,对应ss里也有两种形式：对象方法，全局方法。
    /// 2对2，总共4中情况
    /// </summary>
    public class ExtFuncWrap: ExtWrap, ICall
    {
        MethodInfo method;

        public Type that_type;// ss 对象方法要用
        ParameterInfo[] param_arr;

        public ExtFuncWrap(MethodInfo method, ExtFuncAttribute attr)
        {
            this.name = attr.name ?? method.Name;
            this.tip = attr.tip ?? "";

            if (method.ContainsGenericParameters)
            {
                throw new Exception($"{method.DeclaringType}.{method.Name} contain generic parameter");
            }

            this.method = method;
            param_arr = method.GetParameters();

            that_type = null;
            if (attr is ExtGlobalFuncAttribute == false)
            {
                if (method.IsStatic)
                {
                    if (param_arr.Length == 0)
                    {
                        throw new Exception($"{method.DeclaringType}.{method.Name} is static and has no param, can not be export as object func");
                    }
                    that_type = param_arr[0].ParameterType;
                }
                else
                {
                    that_type = method.DeclaringType;
                }
            }
        }

        public override List<object> Call(Args args)
        {
            object that = null;
            object[] target_args = new object[param_arr.Length];
            int target_base = 0;
            int arg_base = 0;
            if (that_type != null)
            {
                // ss 对象方法
                that = args[Utils.MAGIC_THIS] ?? args.that;
                if (!that_type.IsInstanceOfType(that))
                {
                    throw new Exception($"this does not match, expect {that_type} got {that?.GetType()}. ExtFunc:{method.DeclaringType}.{method.Name} is_static:{method.IsStatic}");
                }
                // 静态方法 to ss对象方法需要特殊处理，将this放到第一个参数的位置
                if (method.IsStatic)
                {
                    target_args[target_base++] = that;
                    that = null;
                }
            }
            else
            {
                // ss 全局方法
                // c#对象方法 to ss全局方法需要特殊处理，从参数列表里提取this
                if (!method.IsStatic)
                {
                    that = args[Utils.MAGIC_THIS] ?? args[0];
                    arg_base = 1;// 不管怎么样，第一个参数都当成this，被吃掉
                    if (!method.DeclaringType.IsInstanceOfType(that))
                    {
                        throw new Exception($"fisrt arg does not match, expect {method.DeclaringType} got {that?.GetType()}. ExtFunc:{method.DeclaringType}.{method.Name} is_static:False");
                    }
                }
            }
            
            for(int idx = target_base; idx < param_arr.Length; idx++)
            {
                var param = param_arr[idx];
                object arg;
                if (!args.name_args.TryGetValue(param.Name, out arg))
                {

                    if(idx - target_base + arg_base>= args.args.Count)
                    {
                        if (param.HasDefaultValue)
                        {
                            arg = param.DefaultValue;
                        }
                        else
                        {
                            throw new Exception($"miss {idx - target_base + arg_base} arg. ExtFunc:{method.DeclaringType}.{method.Name} is_static:{method.IsStatic}");
                        }
                    }
                    else
                    {
                        arg = args.args[idx - target_base + arg_base];
                    }
                }
                target_args[idx] = ExtWrap.CheckAndConvertFromSSToSS(arg, param.ParameterType);
            }

            var ret = method.Invoke(that, target_args);
            List<object> ls = new List<object>() { ret };
            for(int idx = 0; idx < param_arr.Length; idx++)
            {
                var param = param_arr[idx];
                if(param.IsOut || param.ParameterType.IsByRef)
                {
                    ls.Add(target_args[idx]);
                }
            }
            
            return ls;
        }
    }

    public class ExtConstructorWrap : ExtWrap, ICall
    {
        ConstructorInfo ctor;
        ParameterInfo[] param_arr;
        public override List<object> Call(Args args)
        {
            object[] target_args = new object[param_arr.Length];
            for (int idx = 0; idx < param_arr.Length; idx++)
            {
                var param = param_arr[idx];
                object arg;
                if (!args.name_args.TryGetValue(param.Name, out arg))
                {
                    if (idx >= args.args.Count)
                    {
                        if (param.HasDefaultValue)
                        {
                            arg = param.DefaultValue;
                        }
                        else
                        {
                            throw new Exception($"{ctor.DeclaringType}.ctor miss {idx} arg");
                        }
                    }
                    else
                    {
                        arg = args.args[idx];
                    }
                }
                target_args[idx] = ExtWrap.CheckAndConvertFromSSToSS(arg, param.ParameterType);
            }

            var ret = ctor.Invoke(target_args);
            return new List<object>(){ ret};
        }
    }

    public class ExtFieldWrap: ExtWrap
    {
        FieldInfo field_info;
        bool only_read = false;

        public override bool Set(object obj, object value)
        {
            if (only_read)
            {
                throw new Exception($"field {name} is readonly, type={field_info.DeclaringType} is_static={field_info.IsStatic}");
            }
            value = CheckAndConvertFromSSToSS(value, field_info.FieldType);
            field_info.SetValue(obj, value);
            return true;
        }

        public override object Get(object obj)
        {
            return field_info.GetValue(obj);
        }
    }

    public class ExtPropertyWrap : ExtWrap
    {
        PropertyInfo property_info;
        bool only_read = false;

        public override bool Set(object obj, object value)
        {
            if (only_read)
            {
                throw new Exception($"property {name} is readonly, type={property_info.DeclaringType}");
            }
            value = CheckAndConvertFromSSToSS(value, property_info.PropertyType);
            property_info.SetValue(obj, value);
            return true;
        }

        public override object Get(object obj)
        {
            return property_info.GetValue(obj);
        }
    }

    // @om 2.0 再考虑吧
    public class ExtIndexPropertyWrap : ExtWrap
    {

    }


}
