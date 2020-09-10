using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimpleScript;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            {
                //Type[] types = new Type[] {
                //    typeof(bool),
                //    typeof(char),
                //    typeof(byte), typeof(sbyte),
                //    typeof(ushort), typeof(short),
                //    typeof(uint), typeof(int),
                //    typeof(ulong), typeof(long),
                //    typeof(float), typeof(double),
                //};

                //foreach(var t in types)
                //{
                //    Console.WriteLine("{0}, {1}", t.IsPrimitive, t.IsValueType);
                //    var obj = Activator.CreateInstance(t, true);
                //    Console.WriteLine("{0}", obj);
                //}

                //return;
            }

            {
                SimpleScript.Test.TestManager.RunTest();
                //return;
            }

            {
                //ImportCodeGenerate.GenDelegateFactorySource("generate/DelegateFactory.cs", new Type[]{
                //    typeof(Func<int,int>),
                //    typeof(Action),
                //});

                //Console.WriteLine(typeof(Int64).IsPrimitive);
                VM vm = new VM();
                LibBase.Register(vm);
                LibCoroutine.Register(vm);

                vm.ComileFile("test.ss", "test.ssc");

                var pipe = new IOPipe();
                vm.m_hooker.SetPipeServer(pipe);
                vm.m_hooker.SetBreakMode(SimpleScript.DebugProtocol.BreakMode.StopForOnce);

                vm.DoFile("test.ssc");

                // 测试协程，当成是事件循环也行
                Console.Write("Start update coroutine!");
                if (Console.IsInputRedirected == false)
                {
                    Console.WriteLine(" Press Esc to end.");
                }
                else
                {
                    Console.WriteLine();
                }
                
                while (true)
                {
                    if (Console.IsInputRedirected == false && Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Escape)
                        {
                            break;
                        }
                    }
                    CoroutineMgr.Update();
                    System.Threading.Thread.Sleep(10);
                }

                return;
            }
        }
    }
}
