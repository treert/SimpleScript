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

        // 所有vm共享的，这儿需要存一份
        static Dictionary<string, ExtContain> ext_global = new Dictionary<string, ExtContain>();

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
                    throw new Exception($"{obj.GetType().Name} does not have any wrap");
                }
            }
        }
    }

    /// <summary>
    /// ExtWrap的容器，以类为单位，静态和对象的分离开（分离开，也意味着静态和对象的名字可以重复）。
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
                // @om 这儿做些特殊处理，支持类似__index/__newindex 之类的操作。
                throw new Exception("wrap only support string now");
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
            throw new NotImplementedException();
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
    public class ExtWrap
    {
        public object Get(object obj)
        {
            return this;// 对于方法而言，返回自己，等待被调用
        }

        public bool Set(object obj, object value)
        {
            // 只有属性支持这个
            return true;
        }

        // 方法被调用
        public List<object> Call(Args args)
        {
            throw new NotImplementedException();
        }


        public string name;
        public string tip;
    }

    public class ExtFuncWrap
    {

    }

    public class ExtFieldWrap
    {

    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = true, AllowMultiple = false)]
    public class ExtFuncAttribute : Attribute
    {
        public string name;
        public string tip;// 帮助文档
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
