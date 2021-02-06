using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using MyScript;

namespace MyScriptConsole
{
    struct A
    {
        public int aa;
        public A(int x)
        {
            aa = x;
        }
        public A(bool xx)
        {
            this = new A(123);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var ls1 = Enum.GetValues(typeof(Keyword));
            var ls2 = Enum.GetNames(typeof(Keyword));
            Keyword key = 0;
            var xx = Enum.TryParse<Keyword>("in",true, out key);

            {
                Dictionary<object, string> map = new Dictionary<object, string>() {
                    {12,"12" },
                    {12.0,"12.0" },
                    {(BigInteger)12,"big 12" },
                    {(short)12, "short 12" },
                    {13.3, "double 13.3" },
                    {13.3f, "13.3f" },
                };
                foreach(var it in map)
                {
                    Console.WriteLine($"{it.Key} {it.Key.GetType().Name} {it.Value}");
                }
                {
                    object num = (Int16)12;
                    var it = map[num];
                    Console.WriteLine($"{it}");
                }
                
            }


            BigInteger a = BigInteger.Parse("5566656765756463542436657565765");
            BigInteger b = -1;
            BigInteger c = a ^ b;
            Console.WriteLine(c);
        }
    }
}
