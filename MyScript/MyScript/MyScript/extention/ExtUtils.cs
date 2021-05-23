using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace MyScript
{
    // todo 这个大概要做VM级别的隔离
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
    }
}
