using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimpleScript;
using SimpleScript.Serialize;

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
        }

        class A : IEquatable<A>
        {
            public bool Equals(A other)
            {
                return true;
            }
            public override bool Equals(object obj)
            {
                return obj.GetType() == typeof(A);
                //return base.Equals(obj);
            }
        }
        class C {
            internal int _aA = 1;
            internal Dictionary<int, int> dic = new Dictionary<int, int>{ 
                {1,2},
            };
            internal T _t = new T();
            internal E _e = E.A;
            internal int[] _arr = new int[] { 0, 1, 2, 3, 4 };
            internal object _obj = null;
            internal List<int> _list = new List<int> {0,1,2};
        }
        struct T {
            public int _aA;
            public string b;
            public A x;
            public B _bb;
        }

        enum E
        {
            A = 1,
            B = 2,
        }

        struct B
        {
            public int _bb;
        }
        

        static void Main(string[] args)
        {
            //{
            //    var type = typeof(T);
            //    var filed = type.GetField("_aA");
            //    T t = new T();
            //    t._aA = 12;
            //    Console.WriteLine(filed.GetValue(t));
            //    filed.SetValue(t, 3);
            //    Console.WriteLine(filed.GetValue(t));
            //    //return;
            //}

            //{
            //    MemoryStream stream = new MemoryStream();
            //    var obj = new C();
            //    obj._aA = 12;
            //    obj.dic[1] = 32;
            //    obj.dic[2] = 100;
            //    obj._t._aA = 2;
            //    obj._t.b = "absc";
            //    obj._t._bb._bb = 123;
            //    obj._e = E.B;
            //    obj._arr[2] = 200;
            //    obj._obj = 11111;
            //    XSerializer.singleton.Serialize(stream, obj);
            //    stream.Seek(0, SeekOrigin.Begin);
            //    C b = XSerializer.singleton.Deserialize<C>(stream);
            //    Console.WriteLine(b.dic.Count);
            //    Console.WriteLine(b.dic[1]);
            //    Console.WriteLine(b._t._aA);
            //    Console.WriteLine(b._t.b);
            //    Console.WriteLine(b._t._bb._bb);
            //    Console.WriteLine(b._e);
            //    Console.WriteLine(b._arr[2]);

            //    Console.WriteLine(b._obj);
            //    Console.WriteLine(b._list[2]);

            //    Console.WriteLine(typeof(object).IsClass);
            //    Console.WriteLine(typeof(object).IsValueType);

            //    return;
            //}

            {
                //List<int> xx = new List<int>();
                //for (int i = 0; i < 1000; i++)
                //{
                //    xx.Add(i);
                //}
                //MemoryStream stream = new MemoryStream();
                //Console.WriteLine(stream.Length);
                //XSerializer.singleton.Serialize(stream, xx);
                //stream.Seek(0, SeekOrigin.Begin);
                //Console.WriteLine(stream.Length);
                //var yy = XSerializer.singleton.Deserialize<List<int>>(stream);
                //Console.WriteLine(stream.Length);
                //Console.WriteLine(yy[100]);

                //string ss = "hellohello";
                //string a = ss.Substring(0, 5);
                //string b = ss.Substring(5, 5);
                //Console.WriteLine(a == b);
                //Console.WriteLine("" == string.Empty);
                //return;
            }

            {
                VM vm_1 = new VM();
                LibBase.Register(vm_1);
                Compile("test.lua", "test.luac", vm_1);
                ExecuteBinFile("test.luac", vm_1);

                //Dictionary<object, int> dic = new Dictionary<object, int>();
                //string t1 = new string(new char[] { 'a' });
                //string t2 = "a";
                //object s1 = t1;
                //object s2 = t2;
                //Console.WriteLine(t1 == t2);
                //Console.WriteLine(s1 == s2);
                //dic[s1] = 1;
                //dic[s2] = 2;
                //Console.WriteLine(dic[s1]);

                //A a1 = new A();
                //A a2 = new A();
                //dic[a1] = 3;
                //dic[a2] = 4;
                //Console.WriteLine(a1 == a2);
                //Console.WriteLine(dic[a1]);

                //SortedList<Type, int> xx = new SortedList<Type, int>();
                //xx[typeof(int)] = 1;
                //xx[typeof(bool)] = 2;
                //Console.WriteLine(xx.Values[1]);
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
            else if (args[0] == "-b" && args.Length == 2)
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
            //SimpleScript.Test.TestManager.RunTest();


        }
    }
}
