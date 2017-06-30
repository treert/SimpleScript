﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimpleScript;

namespace SimpleScriptConsole
{
    class Program
    {
        static void _PrintTypeName(object xx)
        {
            Console.WriteLine("{0} {1}", xx, xx.GetType().FullName);
        }
        static void ExecuteFile(string file_name, VM vm)
        {
            var content = System.IO.File.ReadAllText(file_name);
            try
            {
                vm.DoString(content);
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

        static void Main(string[] args)
        {

            VM vm = new VM();
            LibBase.Register(vm);

            if (args.Length > 0)
            {
                ExecuteFile("test.lua", vm);
            }
            else
            {
                ExecuteConsole(vm);
            }
            //SimpleScript.Test.TestManager.RunTest();
        }
    }
}
