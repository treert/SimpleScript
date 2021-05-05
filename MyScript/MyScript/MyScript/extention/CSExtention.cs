using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyScript
{
    public static class ExtList
    {
        /// <summary>
        /// https://stackoverflow.com/questions/12231569/is-there-in-c-sharp-a-method-for-listt-like-resize-in-c-for-vectort
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="size"></param>
        /// <param name="element"></param>
        public static void Resize<T>(this List<T?> list, int size, T? element = default(T))
        {
            int count = list.Count;
            if(size <= 0)
            {
                list.Clear();
            }
            else if (size < count)
            {
                list.RemoveRange(size, count - size);
            }
            else if (size > count)
            {
                if (size > list.Capacity)   // Optimization
                    list.Capacity = size;

                list.AddRange(Enumerable.Repeat(element, size - count));
            }
        }
    }

#nullable disable
    public static class DictionaryExtensions
    {
        public static Dictionary<TKey, TValue> AddRange<TKey, TValue>(this Dictionary<TKey, TValue> source, IEnumerable<(TKey, TValue)> kvps)
        {
            foreach (var kvp in kvps)
            {
                source[kvp.Item1] = kvp.Item2;
            }
            return source;
        }
        public static Dictionary<TKey, TValue> AddRange<TKey, TValue>(this Dictionary<TKey, TValue> source, IEnumerable<KeyValuePair<TKey, TValue>> kvps)
        {
            foreach (var kvp in kvps)
            {
                source[kvp.Key] = kvp.Value;
            }
            return source;
        }
    }
#nullable restore
    public static class ExtSomeApi
    {
        public static object? GetByIdx(this IList ls, int idx)
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
            MyArgs args = new MyArgs(objs);
            args.that = that;
            var ret = func.Call(args);
            return ret;
        }
    }

}
