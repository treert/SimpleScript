using System;
using System.Collections;
using System.Collections.Generic;
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

        public static string ToString(object obj, string format, int len)
        {
            string ret;
            if (obj == null)
            {
                ret = "";
            }
            if (obj is string)
            {
                ret = obj as string;// string 保持不变
            }
            else
            {
                // 尝试调用扩展的tostring
                ExtContain ext;
                if (ext_types.TryGetValue(obj.GetType(), out ext))
                {
                    var func = ext.Get(obj, ExtConfig.key_tostring) as ExtFuncWrap;
                    if (func != null)
                    {
                        ret = func.CallWithThisAndReturnOne(obj) as string;
                        // @om 不做检查，如果返回null，下面会挂掉，不要那么搞。
                    }
                    else
                    {
                        ret = obj.ToString();
                    }
                }
                else
                {
                    ret = obj.ToString();
                }
            }

            // padding
            if (len > ret.Length)
            {
                ret = ret.PadLeft(len - ret.Length);
            }
            else if (len < -ret.Length)
            {
                ret = ret.PadRight(-len - ret.Length);
            }
            return ret;
        }

        public static object Get(object obj, object key)
        {
            if (obj == null || key == null) return null;

            if (obj is IGetSet)
            {
                return (obj as IGetSet).Get(key);
            }
            else if (obj is IDictionary)
            {
                return (obj as IDictionary)[key];
            }
            else if (obj is IList)
            {
                var ls = obj as IList;
                if(Utils.TryConvertToInt32(key,out int d))
                {
                    return ls.GetByIdx(d);
                }
            }
            else if (obj is Array)
            {
                var arr = obj as Array;
                if (arr.Rank == 1 && arr.GetLowerBound(0) == 0)
                {
                    if (Utils.TryConvertToInt32(key, out int d))
                    {
                        return arr.GetByIdx(d);
                    }
                }
            }
            else if (obj is string)
            {
                if (Utils.TryConvertToInt32(key, out int d))
                {
                    return (obj as string).GetByIdx(d);
                }
            }
            else if (obj is ExtContain)
            {
                return (obj as ExtContain).Get(null, key);
            }
            else
            {
                ExtContain ext;
                ext_types.TryGetValue(obj.GetType(), out ext);
                if (ext != null)
                {
                    return ext.Get(obj, key);
                }
            }

            return null;
        }

        public static void Set(object obj, object key, object val)
        {
            if (obj == null || key == null) return;

            if (obj is Table)
            {
                (obj as Table).Set(key, val);
            }
            else if (obj is IDictionary)
            {
                (obj as IDictionary)[key] = val;// 类型不对，会抛异常
            }
            else if (obj is IList)
            {
                var ls = obj as IList;
                if(Utils.TryConvertToInt32(key, out int d))
                {
                    ls.SetByIdx(d, val);
                }
            }
            else if (obj is Array)
            {
                var arr = obj as Array;
                if (arr.Rank == 1 && arr.GetLowerBound(0) == 0)
                {
                    if (Utils.TryConvertToInt32(key, out int d))
                    {
                        arr.SetByIdx(d, val);
                    }
                }
            }
            else if (obj is ExtContain)
            {
                (obj as ExtContain).Set(null, key, val);
            }
            else
            {
                ExtContain ext;
                ext_types.TryGetValue(obj.GetType(), out ext);
                if (ext != null)
                {
                    ext.Set(obj, key, val);
                }
                else
                {
                    throw new Exception($"{obj.GetType().Name} does not have any wrap for write");
                }
            }
        }
    }
}
