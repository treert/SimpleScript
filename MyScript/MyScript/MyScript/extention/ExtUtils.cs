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


        static Dictionary<Type, ExtContain> ext_types = new Dictionary<Type, ExtContain>();

        public static void Register(VM vm, ExtFuncWrap func_wrap)
        {
            var that_type = func_wrap.that_type;
            if (that_type == null)
            {
                // global, todo split name
                vm.global_table[func_wrap.name] = func_wrap;
            }
            else
            {
                ExtContain contain;
                if (ext_types.TryGetValue(func_wrap.that_type, out contain) == false)
                {
                    contain = new ExtContain(func_wrap.that_type);
                    ext_types.Add(func_wrap.that_type, contain);
                }
                contain.Register(func_wrap.name, func_wrap);
            }


        }

        // 注册类里面的函数
        public static void Import(VM vm, Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                foreach (var attr in method.GetCustomAttributes<ExtFuncAttribute>())
                {
                    var f = new ExtFuncWrap(method, attr);
                    Register(vm, f);
                }
            }
            // todo 
        }

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
                throw new NotSupportedException($"{obj.GetType().Name} Has Not Implete IGetSet");
            }
        }
    }
}
