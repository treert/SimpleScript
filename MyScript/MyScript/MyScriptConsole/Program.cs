using System;
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

            BigInteger a = BigInteger.Parse("5566656765756463542436657565765");
            BigInteger b = -1;
            BigInteger c = a ^ b;
            Console.WriteLine(c);
        }
    }
}
