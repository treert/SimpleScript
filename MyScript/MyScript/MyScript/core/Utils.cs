using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
namespace MyScript
{
    public class Utils
    {
        public const string MAGIC_THIS = "this";

        /// <summary>
        /// MyScript内部使用的比较函数，可以用于支持 a <> b。
        /// 1. 字符串，按UTF-16二进制方式比较
        /// 2. 数字，正常比较，不过最小的是NaN。
        /// 3. 其他的就有些随意了
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int Compare(object? a, object? b)
        {
            if (a == b) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            if (a is string sa && b is string sb)
            {
                return string.Compare(sa, sb, StringComparison.Ordinal);
            }

            // 需要对数字类型的特殊处理下
            var fa = MyNumber.TryConvertFrom(a);
            var fb = MyNumber.TryConvertFrom(b);
            if (fa is not null && fb is not null)
            {
                return fa.CompareTo(fb);
            }

            if (a.Equals(b)) return 0;// 相等比较特殊处理下吧

            // 不能使用直接使用 IComparable。大部分实现，遇到类型不一样，会抛异常。
            var ta = a.GetType();
            var tb = b.GetType();
            if(ta == tb)
            {
                if (a is IComparable ia)
                {
                    return ia.CompareTo(b);
                }
            }
            // 没办法了，随便比较下啦。选择HashCode，速度可能快些。用FullName也许会更稳定些。
            // @om C# 的HashCode的实现方法还为找到确定位置，疑似发现一个，太复杂了点。
            return ta.GetHashCode().CompareTo(tb.GetHashCode());
        }

        /// <summary>
        /// 等性比较，非常重要。和Compare不一样，Compare == 0, 不一定相等。
        /// 1. 数字类型特殊处理
        /// </summary>
        public static bool CheckEquals(object? a, object? b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;

            // 需要对数字类型的特殊处理下
            var fa = MyNumber.TryConvertFrom(a);
            var fb = MyNumber.TryConvertFrom(b);
            if (fa is not null && fb is not null)
            {
                return fa.Equals(fb);
            }
            else
            {
                return a!.Equals(b);
            }
        }

        public static bool ToBool(object? obj)
        {
            if (obj == null) return false;
            if (obj is bool b)
            {
                return b;
            }
            if (obj is MyNumber num)
            {
                return num.IsNaN();
            }
            if (obj is double d)
            {
                return double.IsNaN(d);
            }
            if (obj is float f)
            {
                return float.IsNaN(f);
            }
            return true;
        }

        public static MyNumber ToNumber(object? obj)
        {
            return MyNumber.ForceConvertFrom(obj);
        }

        public static string ToString(object? obj)
        {
            return obj?.ToString() ?? string.Empty;
        }
    }
}
