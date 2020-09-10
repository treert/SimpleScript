using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using SimpleScript;
using System.Net.Sockets;
using System.Net;

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

        static void ShowBinCode(string src_file, string bin_file, VM vm)
        {
            try
            {
                using (var stream = File.OpenRead(src_file))
                {
                    var func = vm.Parse(stream, src_file);
                    File.WriteAllText(bin_file, func.ToBinCode());
                }

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

        static void DebugFile(string file_name, VM vm, int port)
        {
            //Console.WriteLine(Environment.CurrentDirectory);
            Console.WriteLine($"Simple Script {VM.Version}");
            try
            {
                var func = vm.Parse(File.ReadAllText(file_name), file_name);

                if(port > 0)
                {
                    TcpListener serverSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                    serverSocket.Start();
                    Console.WriteLine("DebugMode: Listen At {0}", port);

                    var socket = serverSocket.AcceptSocket();
                    if(socket != null)
                    {
                        Console.WriteLine("DebugMode: Build Connect With {0}", socket.RemoteEndPoint);
                        using (var stream = new NetworkStream(socket))
                        {
                            var pipe = new SimpleScript.DebugProtocol.NetServerPipe(stream);
                            vm.m_hooker.SetPipeServer(pipe);
                            vm.m_hooker.SetBreakMode(SimpleScript.DebugProtocol.BreakMode.StopForOnce);
                            vm.CallFunction(func);
                            // 测试协程，当成是事件循环也行
                            Console.WriteLine("Loop update for coroutine, You can close program to Exit!!");
                            while (true)
                            {
                                CoroutineMgr.Update();
                                System.Threading.Thread.Sleep(10);
                            }
                        }
                    }
                }
                else
                {
                    var pipe = new IOPipe();
                    vm.m_hooker.SetPipeServer(pipe);
                    vm.m_hooker.SetBreakMode(SimpleScript.DebugProtocol.BreakMode.StopForOnce);
                    vm.CallFunction(func);

                    // 测试协程，当成是事件循环也行
                    Console.WriteLine("Loop update for coroutine, You can close program to Exit!!");
                    while (true)
                    {
                        CoroutineMgr.Update();
                        System.Threading.Thread.Sleep(10);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("Press Any Key To Exit");
            Console.ReadKey(true);
        }

        static void ShowHelpThenExit()
        {
            string exe_file = "ss.exe";
            if(Environment.GetCommandLineArgs().Length > 0)
            {
                exe_file = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
            }
            string help_str = @"
use way:
    {0}                          // run terminal
    {0} xx.ss or xx.ssc          // run source file or binary file
    {0} -c xx.ss [-o xx.ssc]     // compile
    {0} -d xx.ss [-p port]       // debug port range is [1025,9999]    
    {0} -b xx.ss [-o xx.ssb]     // show bin code
";
            Console.Write($"Simple Script {VM.Version} Copyright (C) 2017");
            Console.WriteLine(help_str, exe_file);
        }
        
        static void Main(string[] args)
        {
            VM vm = new VM();
            LibBase.Register(vm);
            LibCoroutine.Register(vm);

            if (args.Length == 0)
            {
                ExecuteConsole(vm);
            }

            if (args[0] == "-c" || args[0] == "-b")
            {
                bool is_compile = args[0] == "-c";
                string src_file = null;
                string bin_file = null;
                if (args.Length == 2)
                {
                    src_file = args[1];
                    bin_file = src_file + "c";
                    if (is_compile == false) bin_file = src_file + "b";
                }
                else if (args.Length == 4 && args[2] == "-o")
                {
                    src_file = args[1];
                    bin_file = args[3];
                }
                if (src_file != null && File.Exists(src_file) &&
                    Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(bin_file))))
                {
                    if(is_compile)
                    {
                        Compile(src_file, bin_file, vm);
                    }
                    else
                    {
                        ShowBinCode(src_file, bin_file, vm);
                    }
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
            else if (args[0] == "-d" && args.Length >=2)
            {
                if (File.Exists(args[1]))
                {
                    if(args.Length == 2)
                    {
                        DebugFile(args[1], vm, 0);
                    }
                    else if(args.Length == 4 && args[2] == "-p")
                    {
                        int port = 0;
                        int.TryParse(args[3], out port);
                        if(port > 0)
                        {
                            DebugFile(args[1], vm, port);
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
