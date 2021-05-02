using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyScript;

namespace MyScript.Test
{
    static class TestUtils
    {
        static Lex lex = new Lex();
        static Parser parser = new Parser();
        public static SyntaxTree Parse(string s)
        {
            lex.Init(s);
            return parser.Parse(lex);
        }

        
    }
}
