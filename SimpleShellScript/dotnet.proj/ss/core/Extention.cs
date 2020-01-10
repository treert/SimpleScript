/*
 * 支持扩展
 * 1. 往 SS 里注入Api
 * 
 * 实现的一些细节
 * 1. 如果没有设置name，会
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace SScript
{

    public static class ExtSomeApi
    {
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
    }

    public static class ExtUtils
    {
        static Dictionary<Type, ExtContain> ext_types = new Dictionary<Type, ExtContain>();

        public static object Get(object obj, string key)
        {
            if (obj == null || key == null) return null;

            if(obj is Table)
            {
                return (obj as Table).Get(key);
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

        public static void Set(object obj, string key, object val)
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
        string name;
        Type type;
        Dictionary<string, ExtWrap> wraps = new Dictionary<string, ExtWrap>();
        bool is_static;

        public object Get(object obj,object key)
        {
            Debug.Assert(key != null);
            Debug.Assert((obj != null) ^ is_static);

            string name = key as string;
            if(name == null)
            {
                // @om 这儿做些特殊处理，支持类似 __index 之类的操作。
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
            throw new Exception("I am not a function, can not be call.");
        }

        // 方法被调用
        public virtual List<object> Call(Args args)
        {
            throw new Exception("I am not a function, can not be call.");
        }

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


        public string name;
        FieldInfo field_info;
        PropertyInfo property_info;
    }

    public class ExtFuncWrap: ExtWrap
    {
        MethodInfo method_info;
        ExtFuncAttribute attr;

        ParameterInfo[] param_arr;

        public ExtFuncWrap(MethodInfo method, ExtFuncAttribute attr)
        {
            if (method.ContainsGenericParameters)
            {
                throw new Exception($"{method.DeclaringType}.{method.Name} contain generic parameter");
            }

            this.method_info = method;
            this.attr = attr;
            param_arr = method.GetParameters();
            if (method.IsStatic)
            {
                if (attr.is_extension_func)
                {
                    if(param_arr.Length == 0)
                    {
                        throw new Exception($"{method.DeclaringType}.{method.Name} has no param, can not be export as object func");
                    }
                }
            }
        }

        // 情况梳理
        // 1. 对象方法调用 a.f(xx)
        // 2. 对象方法对象 f(xx,this=a)
        // 3. 静态方法调用 A.f(xx) or f(xx)
        // 4. 对象静态扩展方法调用 a.f(xx)，反射函数的调用方式略特殊一些
        public override List<object> Call(Args args)
        {
            // 支持下命名参数
            object that = null;
            object[] target_args = new object[param_arr.Length];
            int sp_base = 0;
            if (method_info.IsStatic)
            {
                if (attr.is_extension_func)
                {
                    var t = args.that;
                    if (args.name_args.ContainsKey(Config.MAGIC_THIS))
                    {
                        t = args.name_args[Config.MAGIC_THIS];
                    }
                    if (!param_arr[0].ParameterType.IsInstanceOfType(t))
                    {
                        throw new Exception("this can not be null when call object.method");
                    }
                    target_args[sp_base++] = t;
                }
            }
            else
            {
                // 对象方法
                that = args.that;
                if (args.name_args.ContainsKey(Config.MAGIC_THIS))
                {
                    that = args.name_args[Config.MAGIC_THIS];
                }
                if(!method_info.DeclaringType.IsInstanceOfType(that))
                {
                    throw new Exception("this can not be null when call object.method");
                }
            }
            for(int idx = sp_base; idx < param_arr.Length; idx++)
            {
                var param = param_arr[idx];
                object arg;
                if (!args.name_args.TryGetValue(param.Name, out arg))
                {
                    if(idx - sp_base >= args.args.Count)
                    {
                        if (param.HasDefaultValue)
                        {
                            arg = param.DefaultValue;
                        }
                        else
                        {
                            throw new Exception($"{method_info.DeclaringType}.{method_info.Name} miss {idx - sp_base} arg");
                        }
                    }
                    else
                    {
                        arg = args.args[idx - sp_base];
                    }
                }
                target_args[idx] = ExtWrap.CheckAndConvertFromSSToSS(arg, param.ParameterType);
            }

            var ret = method_info.Invoke(args.that, target_args);
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

    public class ExtFieldWrap
    {
        FieldInfo field_info;

        
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ExtFuncAttribute : Attribute
    {
        public string name;
        public string tip;// 帮助文档
        public bool is_extension_func = false;// static extension 函数标记，类似c#的this扩展方法
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class ExtFieldAttribute : Attribute
    {
        public string name;
        public string tip;// 帮助文档
        public bool onlyread = false;
    }

    // 注册到全局表中，可以用于公开一些静态字段或者函数
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ExtClassAttribute : Attribute
    {
        public string name;
        public string tip;// 帮助文档
    }
}
