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
        public static object? GetByIdx(this string str, int idx)
        {
            if (str.Length == 0) return null;
            idx = ((idx % str.Length) + str.Length) % str.Length;
            return str[idx];
        }

        public static string Join(this IList ls, string str)
        {
            if (ls.Count == 0) return "";
            StringBuilder sb = new StringBuilder(ls[0]?.ToString());
            for(var i = 1; i < ls.Count; i++)
            {
                sb.Append(str);
                sb.Append(ls[i]?.ToString());
            }
            return sb.ToString();
        }
    }

}
