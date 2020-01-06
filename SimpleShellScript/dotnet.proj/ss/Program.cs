using SScript.Test;
using System;
using System.Collections.Generic;

static class ExtClass
{
    public static int ExF(this int a, int b)
    {
        a += 1;
        return a + b;
    }
}

class Program
{


    static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");
        TestManager.RunTest();
        {
            Func<List<int>> f = () =>
            {
                Console.WriteLine("ff");
                return new List<int>() { 1, 2 };
            };

            Func<int> g = () =>
            {
                Console.WriteLine("gg");
                return 0;
            };


            Dictionary<object, object> dic = new Dictionary<object, object>();

            double a = 1.2;
            double b = 1.2;

            object oa = a;
            object ob = b;

            try
            {
                throw new SScript.ScriptException();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine($"{oa == ob}");

            dic[a] = "12";
            Console.WriteLine(dic[ob]);

            Console.WriteLine(f()[g()]);

            ulong xx = (1uL << 63) + 1;
            Console.WriteLine(xx);
            Console.WriteLine((double)xx);
        }


        {
            double dd = 1.0 / 0.0;
            Console.WriteLine(dd);
            Console.WriteLine(double.IsNormal(double.Epsilon*2.0e100));
            Console.WriteLine(double.IsNegativeInfinity(dd));
            Console.WriteLine(double.NaN > dd);
            Console.WriteLine(double.NaN <= dd);
            Console.WriteLine(SScript.Utils.ToNumber(".1e10"));
            Console.WriteLine(SScript.Utils.ToNumber("0.23E+3"));
            Console.WriteLine(SScript.Utils.ToNumber(null));
            Console.WriteLine(SScript.Utils.ToNumber(new object()));
            Console.WriteLine(SScript.Utils.ToNumber('a'));
            Console.WriteLine(SScript.Utils.ToNumber(""));
            Console.WriteLine(SScript.Utils.ToNumber((byte)3));
            Console.WriteLine((double)(byte)3);

            Console.WriteLine((int) -2.5);
        }
        {
            var f = typeof(ExtClass).GetMethod("ExF");
            int a = 1;
            a.ExF(2);
            Console.WriteLine(f.Invoke(null,new object[] { a,2}));
            Console.WriteLine(a);
            Console.WriteLine((int)'✘');
            Console.WriteLine((int)'9');
        }
        {
            object a = (double)1;
            object b = (double)1;
            Dictionary<object, int> d = new Dictionary<object, int>();
            d[a] = 1;
            d[b] = 2;
            Console.WriteLine(a == b);
            Console.WriteLine(a.Equals(b));
            Console.WriteLine(d[a]);
            ulong c = 12345678901234567890L;
            double e = 12345678901234567891L;
            Console.WriteLine(e.ToString("f0"));
            Console.WriteLine(c.ToString("f1"));
            Console.WriteLine(c == e);
            Console.WriteLine(double.TryParse("1234567891234567891234",out e));
        }
    }
}