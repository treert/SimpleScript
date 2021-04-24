using System;
using System.Collections.Generic;
using System.Text;
namespace MyScript
{
    public class Utils
    {
        public const long MaxSafeInt = 9007199254740991;// 2^54 - 1
        public const long MinSafeInt = -9007199254740991;
        public const string MAGIC_THIS = "this";
        public static string def_shell = "bash";

        // 稍微优化下性能，(/ □ \)
        public static readonly List<object> EmptyResults = new List<object>();

        /// <summary>
        /// 等性比较，非常重要。实现调用的C#的 Object.Equals ,所有NaN == NaN 是true。
        /// </summary>
        public static bool CheckEquals(object a, object b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;

            // 需要对数字类型的特殊处理下
            var fa = MyNumber.TryConvertFrom(a);
            var fb = MyNumber.TryConvertFrom(b);
            if (fa.HasValue && fb.HasValue)
            {
                return fa.Value.Equals(fb.Value);
            }
            else
            {
                return a!.Equals(b);
            }
        }

        public static bool ToBool(object obj)
        {
            if (obj == null) return false;
            if (obj is bool b)
            {
                return b;
            }
            return true;
        }

        public static MyNumber ToNumber(object obj)
        {
            return MyNumber.ForceConvertFrom(obj);
        }

        public static bool TryConvertToInt32(object obj, out int ret)
        {
            var n = MyNumber.TryConvertFrom(obj);
            if (n.HasValue && n.Value.IsInt32)
            {
                ret = (int)n;
                return true;
            }
            ret = 0;
            return false;
        }

        public static string ToString(object obj)
        {
            return obj == null ? "" : obj.ToString();
        }

        public static string ToString(object obj, string format, int len)
        {
            return obj?.ToString() ?? "";
            // todo
            //throw new NotImplementedException();
        }
    }
}
