using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{
    class Program
    {
        static void _PrintTypeName(object xx)
        {
            Console.WriteLine("{0} {1}",xx , xx.GetType().FullName);
        }
        static void Main(string[] args)
        {
            test.TestManager.RunTest();
            VM vm = new VM();

            var content = System.IO.File.ReadAllText("test/test.lua");
            vm.DoString(content);
            _PrintTypeName(1);
            _PrintTypeName(1.0 is double);
        }
    }
}
