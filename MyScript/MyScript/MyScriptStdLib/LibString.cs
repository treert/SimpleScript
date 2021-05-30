using System;
using System.Collections.Generic;
using System.Text;
using MyScript;

namespace MyScriptStdLib
{
    public class LibString : IGetSet, ICall
    {
        public static LibString Register(VM vm)
        {
            var lib = new LibString();
            vm.global_table["string"] = lib;
            return lib;
        }

        static Dictionary<string, ICall> s_func_map;
        static LibString()
        {
            s_func_map = new Dictionary<string, ICall>() {
                { "join",ICall.Create(Join) },
            };
        }

        public object Call(MyArgs args)
        {
            //if (args.m_args.Count == 0) return null;// 想了想，保证返回字符串吧
            return Utils.ToString(args[0]);
        }

        public object Get(object key)
        {
            if (key is string ss)
            {
                if (s_func_map.TryGetValue(ss, out ICall func))
                {
                    return func;
                }
                return null;
            }
            return null;
        }

        public void Set(object key, object val)
        {
            throw new NotImplementedException();
        }

        public static string Join(MyArgs args)
        {
            var ls = args.m_args.m_items;
            if (ls.Count < 2) return "";
            var str = ls[0]?.ToString();
            StringBuilder sb = new StringBuilder(ls[1]?.ToString());
            for (var i = 2; i < ls.Count; i++)
            {
                sb.Append(str);
                sb.Append(ls[i]?.ToString());
            }
            return sb.ToString();
        }
    }
}
