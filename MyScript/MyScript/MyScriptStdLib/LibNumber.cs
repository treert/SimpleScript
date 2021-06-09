using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyScript;

namespace MyScriptStdLib
{
    public class LibNumber : IGetSet, ICall
    {
        public static void Register(VM vm)
        {
            var lib = new LibNumber();
            vm.global_table["number"] = lib;
        }

        public object Call(MyArgs args)
        {
            return Utils.ToNumber(args[0]);
        }

        public object Get(object key)
        {
            throw new NotImplementedException();
        }

        public void Set(object key, object val)
        {
            throw new NotImplementedException();
        }
    }
}
