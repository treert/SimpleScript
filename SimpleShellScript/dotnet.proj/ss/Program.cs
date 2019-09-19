using SScript.Test;
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");
        TestManager.RunTest();


        Dictionary<object, object> dic = new Dictionary<object, object>();

        double a = 1.2;
        double b = 1.2;

        object oa = a;
        object ob = b;

        Console.WriteLine($"{oa == ob}");

        dic[a] = "12";
        Console.WriteLine(dic[b]);
    }
}