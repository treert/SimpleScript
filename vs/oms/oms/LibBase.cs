using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{
    static class LibBase
    {
        public static int Print(Thread th)
        {
            int arg_count = th.GetStatckSize();

            for (int i = 0; i < arg_count; ++i)
            {
                object obj = th.GetValue(i);
                if (obj == null)
                    Console.Write("nil");
                else if (obj is bool)
                    Console.Write("{0}", obj);
                else if (obj is double)
                    Console.Write("{0}", obj);
                else if (obj is string)
                    Console.Write(obj);
                else
                    Console.Write("{0}:{1}", obj.GetType().Name, obj);

                if (i != arg_count - 1)
                {
                    Console.Write("\t");
                }
            }
            Console.WriteLine();
            return 0;
        }

        public static void Register(VM vm)
        {
            vm.RegisterGlobalFunc("print", Print);
        }
    }
}
