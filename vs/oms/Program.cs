using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{
    class Program
    {
        static void Main(string[] args)
        {
            var lex = new Lex();
            lex.Init("a = 123");
            Console.WriteLine(lex.GetNextToken());
            Console.WriteLine(lex.GetNextToken());
            Console.WriteLine(lex.GetNextToken());
            Console.WriteLine(lex.GetNextToken());
        }
    }
}
