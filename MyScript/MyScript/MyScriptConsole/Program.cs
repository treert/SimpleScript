using System;
using MyScript;

namespace MyScriptConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var ls1 = Enum.GetValues(typeof(Keyword));
            var ls2 = Enum.GetNames(typeof(Keyword));
            Keyword key = 0;
            var xx = Enum.TryParse<Keyword>("in",true, out key);
        }
    }
}
