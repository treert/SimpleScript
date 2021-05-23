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
            
            return null;
        }

        public static object? Join(MyArgs args)
        {
            return null;
        }

        public static void Set(this string str, object key, object? value)
        {
            throw new NotSupportedException("not support modify <string>");
        }
    }
}
