using System;
using System.Collections.Generic;
using System.Text;

namespace SScript
{
    public class Utils
    {
        public static bool ToBool(object obj)
        {
            if (obj == null) return false;
            if (obj is bool)
            {
                return (bool)obj;
            }
            // todo 要不要对NaN或者0，"" 做些特殊处理
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
                return (char)obj;// Convert.ToDouble 不支持
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

        public static string ToString(object obj)
        {
            return obj == null ? "" : obj.ToString();
        }
    }
}
