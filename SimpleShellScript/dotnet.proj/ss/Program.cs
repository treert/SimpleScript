using SScript.Test;
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");
        TestManager.RunTest();

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
        catch(Exception e)
        {
            Console.WriteLine(e.Message);
        }

        Console.WriteLine($"{oa == ob}");

        dic[a] = "12";
        Console.WriteLine(dic[ob]);

        Console.WriteLine(f()[g()]);

        ulong xx = (1uL<<63)+1;
        Console.WriteLine(xx);
        Console.WriteLine((double)xx);

        {
            double dd = 1.0 / 0.0;
            Console.WriteLine(dd);
            Console.WriteLine(double.IsNormal(double.Epsilon*2.0e100));
            Console.WriteLine(double.IsNegativeInfinity(dd));
            Console.WriteLine(double.NaN > dd);
            Console.WriteLine(double.NaN <= dd);
            Console.WriteLine(SScript.ValueUtils.ToNumber(".1e10"));
            Console.WriteLine(SScript.ValueUtils.ToNumber("0.23E+3"));
            Console.WriteLine(SScript.ValueUtils.ToNumber(null));
            Console.WriteLine(SScript.ValueUtils.ToNumber((byte)3));
            Console.WriteLine((double)(byte)3);
        }
    }
}