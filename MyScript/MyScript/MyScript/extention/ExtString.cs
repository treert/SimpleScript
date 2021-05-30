using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyScript
{
    /// <summary>
    /// 对字符串做扩展支持。
    /// 硬编码实现，不适用与扩展其他类型。
    /// </summary>
    public static class ExtString
    {
        static Dictionary<string, ICall> s_func_map;
        static ExtString()
        {
            s_func_map = new Dictionary<string, ICall>(StringComparer.OrdinalIgnoreCase) {
                { "sub",ICall.Create(Sub) },
                { "join",ICall.Create(Join) },
            };
        }
        public static object? Get(string str, object key)
        {
            if(key is string ss)
            {
                if(s_func_map.TryGetValue(ss, out ICall? func))
                {
                    return func;
                }
                switch (ss)
                {
                    case "size":
                        return ss.Length;
                }
                return null;
            }
            var num = MyNumber.TryConvertFrom(key);
            if(num is not null && num.IsInt32)
            {
                return str.GetByIdx((int)num);
            }
            return null;
        }

        public static object? Sub(MyArgs args)
        {
            var that = args.That;
            var start = MyNumber.TryConvertFrom(args[0]);
            var len = MyNumber.TryConvertFrom(args[1]);
            if(that is string str && start is not null)
            {
                if (str.Length == 0) return "";
                int s = (int)start;
                s = (s % str.Length + str.Length) % str.Length;
                if(len is null)
                {
                    return str.Substring(s);
                }
                else
                {
                    return str.Substring(s, (int)len);
                }
            }
            return null;
        }

        public static object? Join(MyArgs args)
        {
            if(args.That is string str)
            {
                return args.m_args.m_items.Join(str);
            }
            return null;
        }
    }
}
