using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{

    public static class ExtSomeApi
    {
        public static T GetValueOrDefault<T>(this List<T> ls, int idx)
        {
            if (ls != null && idx < ls.Count)
            {
                return ls[idx];
            }
            return default(T);
        }
        public static V GetValueOrDefault<K, V>(this Dictionary<K, V> dict, K key)
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
            if (Math.Abs(idx) >= ls.Count)
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
            return ret;
        }
    }

}
