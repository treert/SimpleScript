using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyScriptTest
{
    class TestThread
    {
        public static readonly TestThread singleton = new TestThread();

        private TestThread()
        {
            Console.WriteLine("TestThread.ctor");
        }

        public static void Test1()
        {
            Console.WriteLine("=========  TestThread.Test1 Start  =================");

            Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine(TestThread.singleton);

            var th1 = new Thread(()=> {
                Thread.Sleep(1000);
                Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine(TestThread.singleton);
            });
            th1.Start();
            
            var task = Task.Delay(10000);// Task默认使用后台线程执行，也就是 ThreadPool 里的线程。不会卡住程序退出

            //th1.Join();// 前台线程结束前，程序不会结束，所以不用等待

            Task atask = Acall();
            atask.GetAwaiter().GetResult();

            Console.WriteLine("=========  TestThread.Test1 End  =================");
        }

        static async Task Acall()
        {
            Console.WriteLine("A1");
            await Bcall();
            Console.WriteLine("A2");
        }

        static async Task Bcall()
        {
            Console.WriteLine("B1");
            await Ccall();
            Console.WriteLine("B2");
        }

        static async Task Ccall()
        {
            Console.WriteLine("C");
        }
    }
}
