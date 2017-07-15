using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimpleScript;

namespace SimpleScriptConsole
{
    class Program
    {
        static void ExecuteFile(string file_name, VM vm)
        {
            try
            {
                vm.DoFile(file_name);
            }
            catch (ScriptException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void ExecuteBinFile(string file_name, VM vm)
        {
            try
            {
                vm.DoFile(file_name);
            }
            catch (ScriptException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void Compile(string src_file, string bin_file, VM vm)
        {
            try
            {
                vm.ComileFile(src_file, bin_file);
                Console.WriteLine("out file: {0}", bin_file);
            }
            catch (ScriptException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void ExecuteConsole(VM vm)
        {
            Console.WriteLine("Simple Script 1.0 Copyright (C) 2017");

            for (; ; )
            {
                try
                {
                    Console.Write("> ");
                    var line = Console.ReadLine();
                    vm.DoString(line, "stdin");
                }
                catch (ScriptException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        static void ShowHelpThenExit()
        {
            string help_str = @"Simple Script 1.0 Copyright (C) 2017
use way:
ss                          // run terminal
ss xx.ss or xx.ssc          // run source file or binary file
ss -c xx.ss [-o xx.ssc]     // compile
";
            Console.WriteLine(help_str);
        }

        static void Main(string[] args)
        {
            {
                //SimpleScript.Test.TestManager.RunTest();
                //return;
            }

            {
                Console.WriteLine((object)2.0 == (object)2.0);

                VM vm_1 = new VM();
                LibBase.Register(vm_1);
                Compile("test.ss", "test.ssc", vm_1);
                ExecuteFile("test.ssc", vm_1);
                return;
            }

            VM vm = new VM();
            LibBase.Register(vm);

            if (args.Length == 0)
            {
                ExecuteConsole(vm);
            }

            if (args[0] == "-c")
            {
                // 编译模式
                string src_file = null;
                string bin_file = null;
                if (args.Length == 2)
                {
                    src_file = args[1];
                    bin_file = src_file + "c";
                }
                else if (args.Length == 4 && args[2] == "-o")
                {
                    src_file = args[1];
                    bin_file = args[3];
                }
                if (src_file != null && File.Exists(src_file) &&
                    Directory.Exists(Path.GetDirectoryName(bin_file)))
                {
                    Compile(src_file, bin_file, vm);
                }
                else
                {
                    ShowHelpThenExit();
                }
            }
            else if (args.Length == 1)
            {
                if (File.Exists(args[0]))
                {
                    ExecuteFile(args[0], vm);
                }
                else
                {
                    ShowHelpThenExit();
                }
            }
            else
            {
                ShowHelpThenExit();
            }
        }
    }
}
