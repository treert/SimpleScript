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
                var content = System.IO.File.ReadAllText(file_name);
                vm.DoString(content);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void ExecuteBinFile(string file_name, VM vm)
        {
            try
            {
                FileStream file = new FileStream(file_name, FileMode.Open, FileAccess.Read, FileShare.Read);

                var func = vm.Deserialize(file);
                vm.CallFunction(func);

                file.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void Compile(string src_file, string bin_file, VM vm)
        {
            try
            {
                var content = File.ReadAllText(src_file);
                FileStream file = new FileStream(bin_file, FileMode.Create);

                vm.Serialize(content, file);

                file.Close();
                Console.WriteLine("out file: {0}", bin_file);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
                    vm.DoString(line);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        static void ShowHelpThenExit()
        {
            string help_str = @"use way:
ss                          // run terminal
ss xx.ss                    // run source file
ss -b xx.ssc                // run bin file
ss -c xx.ss [-o xx.ssc]     // compile
";
            Console.WriteLine(help_str);
            Console.Write("Press enter to exit");
            Console.ReadLine();
        }

        static void Main(string[] args)
        {

            VM vm = new VM();
            LibBase.Register(vm);

            if(args.Length == 0)
            {
                ExecuteConsole(vm);
            }

            if(args[0] == "-c")
            {
                // 编译模式
                string src_file = null;
                string bin_file = null;
                if(args.Length == 2)
                {
                    src_file = args[1];
                    bin_file = src_file + "c";
                }
                else if(args.Length == 4 && args[2] == "-o")
                {
                    src_file = args[1];
                    bin_file = args[3];
                }
                if(src_file != null && File.Exists(src_file) &&
                    Directory.Exists(Path.GetDirectoryName(bin_file)))
                {
                    Compile(src_file, bin_file, vm);
                }
                else
                {
                    ShowHelpThenExit();
                }
            }
            else if(args[0] == "-r" && args.Length == 2)
            {
                // 运行二进制
                if (File.Exists(args[1]))
                {
                    ExecuteBinFile(args[1], vm);
                }
                else
                {
                    ShowHelpThenExit();
                }
            }
            else if(args.Length == 1)
            {
                if(File.Exists(args[0]))
                {
                    ExecuteFile(args[0], vm);
                }
                else
                {
                    ShowHelpThenExit();
                }
            }
            //SimpleScript.Test.TestManager.RunTest();


        }
    }
}
