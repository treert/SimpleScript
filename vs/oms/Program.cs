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
        static void ExecuteFile(string file_name,VM vm)
        {
            var content = System.IO.File.ReadAllText(file_name);
            vm.DoString(content);
        }

        static void ExecuteConsole(VM vm)
        {
            Console.WriteLine("OMS 0.1 Copyright (C) 2017");

            for(;;)
            {
                try
                {
                    Console.Write("> ");
                    var line = Console.ReadLine();
                    vm.DoString(line);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        static void Main(string[] args)
        {
            test.TestManager.RunTest();
            VM vm = new VM();
            LibBase.Register(vm);

            if(args.Length > 0)
            {
                ExecuteConsole(vm);
            }
            else
            {
                ExecuteFile("test/test.lua", vm);
            }
        }
    }
}
