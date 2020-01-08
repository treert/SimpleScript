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

        public static object GetByIdx(this Array arr, int idx)
        {
            if (arr.Length == 0) return null;// 没得办法
            // 对索引做循环处理
            idx = ((idx % arr.Length) + arr.Length) % arr.Length;
            return arr.GetValue(idx);// arr.Rank == 1 or will throw exception
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
                if(arr.Rank == 1)
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
                return (obj as ExtContain).Get(key);
            }
            // 扩展
            {
                ExtContain ext;
                ext_types.TryGetValue(obj.GetType(), out ext);
                if(ext != null)
                {
                    
                }
            }
            return null;
        }

        public static void Set(object obj, string key, object val)
        {

        }
    }

    public class ExtContain
    {
        public object Get(object key)
        {
            throw new NotImplementedException();
        }
        public bool Set(object key, object value)
        {
            throw new NotImplementedException();
        }
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
