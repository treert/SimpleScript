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
    }
}