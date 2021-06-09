using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyScript;

namespace MyScriptStdLib
{
    public class LibTable : IGetSet, ICall
    {
        public static void Register(VM vm)
        {
            var lib = new LibTable();
            vm.global_table["table"] = lib;
        }

        static Dictionary<string, ICall> s_func_map;
        static LibTable()
        {
            s_func_map = new Dictionary<string, ICall>() {
                { "count",ICall.Create(GetCount) },
            };
        }

        static object GetCount(MyArgs args)
        {
            var obj = args[0];
            if(obj is MyTable t)
            {
                return t.Count;
            }
            else if(obj is MyArray a)
            {
                return a.Count;
            }
            else if(obj is string s)
            {
                return s.Length;
            }
            return 0;
        }

        public object Call(MyArgs args)
        {
            throw new NotImplementedException();
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
    }
}
