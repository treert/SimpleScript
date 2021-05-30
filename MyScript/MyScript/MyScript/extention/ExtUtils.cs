using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Text;


/*
 * 扩展的思路1.0，极度简化版
 * 1. 手动扩展，实现IGetSet和IForEach接口。
 *     - 只支持有限的类型，不提供方便的反射支持。类型转换什么的也不支持了。
 *     - 慢慢手撸扩展库。
 * 2. 不提供类继承系统的支持，有需要，以后可以搞一个class库来。
 * 3. 提供一些工具反射函数，可以在ms里使用字符串查询方式获取接口来调用。【方便测试.Net功能啥的】
 * 4. 一些非常基础的功能：tonumber,tostring,tobool+string本身方法的支持，再想想。
 *     - 准备像python一样，提供一些全局基本的函数库：number,string,bool。
 *          - > https://www.runoob.com/python3/python3-built-in-functions.html
 * 
 */

namespace MyScript
{
    public static class ExtUtils
    {
        public static object? Get(object obj, object key)
        {
            if (obj is IGetSet ig)
            {
                return ig.Get(key);
            }
            else if (obj is string str)
            {
                return ExtString.Get(str, key);// 字符串特殊处理下
            }
            else
            {
                // @om do nothing
            }

            return null;
        }

        public static void Set(object obj, object key, object? val)
        {
            Debug.Assert(key != null);

            if (obj is IGetSet ig)
            {
                ig.Set(key, val);
            }
            else
            {
                throw new NotSupportedException($"{obj.GetType().Name} has not implement IGetSet");
            }
        }

        public static object? ConvertFromMSToCS(object? obj, Type target_type)
        {
            if (obj is null)
            {
                return null;// 注意值类型的情况
            }

            if (target_type.IsInstanceOfType(obj))
            {
                return obj;
            }

            // @om 一些特殊处理
            // Enum
            if (target_type.IsEnum)
            {
                if (obj is string str)
                {
                    return Enum.Parse(target_type, str);// will three exception
                }
                var n = MyNumber.TryConvertFrom(obj);
                if (n is not null && n.IsInt64)
                {
                    long d = (long)n;// 经过阅读源码，这个是内部最终也会转换过来的版本
                    return Enum.ToObject(target_type, d);
                }
                else
                {
                    return null;
                }
            }

            // 对数字的相互转换做特殊处理
            {
                var n = MyNumber.TryConvertFrom(obj);
                if(n is not null)
                {
                    var typecode = Type.GetTypeCode(target_type);
                    return typecode switch {
                        TypeCode.Empty => null,
                        TypeCode.Object => null,// 类型不对
                        TypeCode.DBNull => null,
                        TypeCode.Boolean => (bool)n,
                        TypeCode.Char => (char)(int)n,
                        TypeCode.SByte => (sbyte)n,
                        TypeCode.Byte => (byte)(uint)n,
                        TypeCode.Int16 => (short)n,
                        TypeCode.UInt16 => (ushort)(uint)n,
                        TypeCode.Int32 => (int)n,
                        TypeCode.UInt32 => (uint)n,
                        TypeCode.Int64 => (long)n,
                        TypeCode.UInt64 => (ulong)n,
                        TypeCode.Single => (float)n,
                        TypeCode.Double => (double)n,
                        TypeCode.Decimal => (double)n,// 不想支持来着
                        TypeCode.DateTime => null,
                        TypeCode.String => null,
                    };
                }
            }
            return null;
        }

        public static T? ConvertFromMSToCS<T>(object? obj)
        {
            obj = ConvertFromMSToCS(obj, typeof(T));
            if(obj is null)
            {
                return default(T);
            }
            return (T)obj;
        }
    }
}
