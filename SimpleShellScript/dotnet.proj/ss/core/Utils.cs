using System;
using System.Collections.Generic;
using System.Text;

namespace SScript
{
    public class Utils
    {
        public static int Compare(object a, object b)
        {
            if (a == b) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            // 需要对数字类型的特殊处理下
            var fa = Utils.ConvertToPriciseDouble(a);
            var fb = Utils.ConvertToPriciseDouble(b);

            if (double.IsNaN(fa) || double.IsNaN(fb))
            {
                // 尝试用 IComparable 来比较
                var ta = a as IComparable;
                var tb = b as IComparable;
                if (ta != null && tb != null)
                {
                    return ta.CompareTo(tb);
                }
                else
                {
                    throw new Exception($"Can not compare, one obj has not implement IComparable");
                }
            }
            else
            {
                return fa.CompareTo(fb);
            }
        }

        public static bool CheckEquals(object a, object b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;

            // 需要对数字类型的特殊处理下
            var fa = Utils.ConvertToPriciseDouble(a);
            var fb = Utils.ConvertToPriciseDouble(b);

            if(double.IsNaN(fa) || double.IsNaN(fb))
            {
                return a.Equals(b);
            }
            else
            {
                return fa == fb;
            }
        }

        public static bool ToBool(object obj)
        {
            if (obj == null) return false;
            if (obj is bool)
            {
                return (bool)obj;
            }
            if(obj is double)
            {
                return !double.IsNaN((double)obj);// 对NaN特殊处理下，NaN 不能用于常见的一些运算
            }
            // todo 要不要对0，"" 做些特殊处理
            return true;
        }

        public static double ToNumber(object obj)
        {
            if (obj is string)
            {
                return ParseNumber(obj as string);
            }
            else if (obj is char)
            {
                return (char)obj;// Convert.ToDouble 不支持,大概是因为char转int有歧义，比如 '0'
            }
            else if (obj == null)
            {
                return double.NaN;
            }
            try
            {
                return Convert.ToDouble(obj);
            }
            catch
            {
                return double.NaN;
            }
        }

        public static double ParseNumber(string s)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(s))
                {
                    return double.NaN;
                }
                s = s.Trim();
                if (s[0] == '0')
                {
                    if (s.Length == 1)
                    {
                        return 0;
                    }

                    if (s[1] == 'x' || s[1] == 'X')
                    {
                        return Convert.ToUInt32(s.Substring(2), 16);// 0xff
                    }
                    else if (s[1] == 'b' || s[1] == 'B')
                    {
                        return Convert.ToUInt32(s.Substring(2), 2);// 0b01
                    }
                    else if (s[1] == '.')
                    {
                        return Convert.ToDouble(s);
                    }
                    else
                    {
                        return Convert.ToUInt32(s.Substring(1), 8);
                    }
                }
                else
                {
                    return Convert.ToDouble(s);
                }
            }
            catch
            {
                return double.NaN;
            }
        }

        public static double ConvertToPriciseDouble(object obj)
        {
            if (obj is double)
            {
                return (double)obj;
            }
            if (obj == null || obj is string || (obj.GetType().IsPrimitive == false && !(obj is decimal)))
            {
                return double.NaN;// 这个最常用，算是一点点优化了。
            }
            if (obj is float)
            {
                return (float)obj;
            }
            if (obj is byte)
            {
                return (byte)obj;
            }
            if (obj is sbyte)
            {
                return (sbyte)obj;
            }
            if (obj is short)
            {
                return (short)obj;
            }
            if (obj is ushort)
            {
                return (ushort)obj;
            }
            if (obj is int)
            {
                return (int)obj;
            }
            if (obj is uint)
            {
                return (uint)obj;
            }
            if (obj is long)
            {
                if(Math.Abs((long)obj) > Config.MaxSafeInt)
                {
                    return double.NaN;
                }
                return (long)obj;
            }
            if (obj is ulong)
            {
                if (((ulong)obj) > Config.MaxSafeInt)
                {
                    return double.NaN;
                }
                return (ulong)obj;
            }
            if (obj is decimal)
            {
                if (Math.Abs((decimal)obj) > Config.MaxSafeInt)
                {
                    return double.NaN;
                }
                return decimal.ToDouble((decimal)obj);
            }
            return double.NaN;
        }

        public static string ToString(object obj)
        {
            return obj == null ? "" : obj.ToString();
        }

        public static string ToString(object obj, string format, int len)
        {
            // todo
            throw new NotImplementedException();
        }
    }
}
