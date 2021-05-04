using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript.Test
{
    class TestLex1 : TestBase
    {
        public override void Run()
        {
            var lex = new Lex();
            lex.Init(" \r\n\t\v\f");
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.EOS);
        }
    }
    
    class TestLex2 : TestBase
    {
        public override void Run()
        {
            var lex = new Lex();
            lex.Init(@"// this is comment
//[[this is long comment]]
//[[this is long comment too//]]
//[=incomplete comment]");
            try
            {
                lex.GetNextToken();
                Error("not exception");
            }
            catch (LexException) { }
        }
    }

    class TestLex4 : TestBase
    {
        public override void Run()
        {
            var lex = new Lex();
            lex.Init("3 3.0 3.1416 314.16e-2 0.31416E1 0xff 0Xf "
        + "0x");
            for (int i = 0; i < 7; ++i)
            {
                ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.NUMBER);
            }
            try
            {
                lex.GetNextToken();
                Error("not exception");
            }
            catch (LexException) { }
        }
    }

    class TestLex5 : TestBase
    {
        public override void Run()
        {
            var lex = new Lex();
            lex.Init("+ - * / % ^ == ~= <= >= < > = ( ) { } [ ] ; : , . .. += -= .=");
            ExpectTrue(lex.GetNextToken().m_type == (int)'+');
            ExpectTrue(lex.GetNextToken().m_type == (int)'-');
            ExpectTrue(lex.GetNextToken().m_type == (int)'*');
            ExpectTrue(lex.GetNextToken().m_type == (int)'/');
            ExpectTrue(lex.GetNextToken().m_type == (int)'%');
            ExpectTrue(lex.GetNextToken().m_type == (int)'^');
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.EQ);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.BIT_XOR_SELF);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.LE);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.GE);
            ExpectTrue(lex.GetNextToken().m_type == (int)'<');
            ExpectTrue(lex.GetNextToken().m_type == (int)'>');
            ExpectTrue(lex.GetNextToken().m_type == (int)'=');
            ExpectTrue(lex.GetNextToken().m_type == (int)'(');
            ExpectTrue(lex.GetNextToken().m_type == (int)')');
            ExpectTrue(lex.GetNextToken().m_type == (int)'{');
            ExpectTrue(lex.GetNextToken().m_type == (int)'}');
            ExpectTrue(lex.GetNextToken().m_type == (int)'[');
            ExpectTrue(lex.GetNextToken().m_type == (int)']');
            ExpectTrue(lex.GetNextToken().m_type == (int)';');
            ExpectTrue(lex.GetNextToken().m_type == (int)':');
            ExpectTrue(lex.GetNextToken().m_type == (int)',');
            ExpectTrue(lex.GetNextToken().m_type == (int)'.');
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.CONCAT);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.ADD_SELF);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.DEC_SELF);
            ExpectTrue(lex.GetNextToken().Match(TokenType.CONCAT_SELF));
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.EOS);
        }
    }

    class TestLex6 : TestBase
    {
        public override void Run()
        {
            var lex = new Lex();
            lex.Init(@"and else elseif global false for fn if in local 
nil not or return true while");
            ExpectTrue(lex.GetNextToken().Match(Keyword.AND));
            ExpectTrue(lex.GetNextToken().Match(Keyword.ELSE));
            ExpectTrue(lex.GetNextToken().Match(Keyword.ELSEIF));
            ExpectTrue(lex.GetNextToken().Match(Keyword.GLOBAL));
            ExpectTrue(lex.GetNextToken().Match(Keyword.FALSE));
            ExpectTrue(lex.GetNextToken().Match(Keyword.FOR));
            ExpectTrue(lex.GetNextToken().Match(Keyword.FN));
            ExpectTrue(lex.GetNextToken().Match(Keyword.IF));
            ExpectTrue(lex.GetNextToken().Match(Keyword.IN));
            ExpectTrue(lex.GetNextToken().Match(Keyword.LOCAL));
            ExpectTrue(lex.GetNextToken().Match(Keyword.NIL));
            ExpectTrue(lex.GetNextToken().Match(Keyword.NOT));
            ExpectTrue(lex.GetNextToken().Match(Keyword.OR));
            ExpectTrue(lex.GetNextToken().Match(Keyword.RETURN));
            ExpectTrue(lex.GetNextToken().Match(Keyword.TRUE));
            ExpectTrue(lex.GetNextToken().Match(Keyword.WHILE));
            ExpectTrue(lex.GetNextToken().Match(TokenType.EOS));
        }
    }

    class TestLex7 : TestBase
    {
        public override void Run()
        {
            var lex = new Lex();
            lex.Init("_ __ ___ _1 _a _a1 a1 a_ a_1 name");
            for (int i = 0; i < 10; ++i)
                ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.NAME);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.EOS);
        }
    }

    class TestLex_String: TestBase
    {
        public override void Run()
        {
            var lex = new Lex();
            lex.Init(@"""${name..\[ haha ha ]
..""'""
..\[ls -l]
..\[bash `lalala`
]
} \n \t end""");
            ExpectTrue(lex.GetNextToken().Match(TokenType.STRING_BEGIN));
            ExpectTrue(lex.CurStringType == StringBlockType.DoubleQuotation);
            {
                ExpectTrue(lex.GetNextToken().Match('{'));
                ExpectTrue(lex.GetNextToken().Match(TokenType.NAME));
                ExpectTrue(lex.GetNextToken().Match(TokenType.CONCAT));
                ExpectTrue(lex.GetNextToken().Match(TokenType.STRING));
                ExpectTrue(lex.GetNextToken().Match(TokenType.CONCAT));
                ExpectTrue(lex.GetNextToken().Match(TokenType.STRING_BEGIN));
                ExpectTrue(lex.CurStringType == StringBlockType.DoubleQuotation);
                ExpectTrue(lex.GetNextToken().m_string == "'");
                ExpectTrue(lex.GetNextToken().Match(TokenType.CONCAT));
                ExpectTrue(lex.GetNextToken().m_string == "ls -l");
                ExpectTrue(lex.GetNextToken().Match(TokenType.CONCAT));
                ExpectTrue(lex.GetNextToken().m_string == "bash `lalala`\n");
                //ExpectTrue(lex.GetNextToken().Match(TokenType.STRING));
                ExpectTrue(lex.GetNextToken().Match('}'));
            }
            ExpectTrue(lex.GetNextToken().m_string == " \n \t end");
            ExpectTrue(lex.IsStringEnded);
            ExpectTrue(lex.GetNextToken().Match(TokenType.EOS));
        }
    }
}
