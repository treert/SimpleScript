using System;
using System.Collections.Generic;
using System.Text;

namespace SS.Test
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
            lex.Init("+ - * / % ^ == ~= <= >= < > = ( ) { } [ ] ; : , . .. ... += ++ -= --");
            ExpectTrue(lex.GetNextToken().m_type == (int)'+');
            ExpectTrue(lex.GetNextToken().m_type == (int)'-');
            ExpectTrue(lex.GetNextToken().m_type == (int)'*');
            ExpectTrue(lex.GetNextToken().m_type == (int)'/');
            ExpectTrue(lex.GetNextToken().m_type == (int)'%');
            ExpectTrue(lex.GetNextToken().m_type == (int)'^');
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.EQ);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.NE);
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
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.DOTS);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.ADD_SELF);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.ADD_ONE);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.DEC_SELF);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.DEC_ONE);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.EOS);
        }
    }

    class TestLex6 : TestBase
    {
        public override void Run()
        {
            var lex = new Lex();
            lex.Init(@"and do else elseif false for def global if in local 
nil not or return then true while");
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.AND);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.DO);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.ELSE);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.ELSEIF);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.FALSE);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.FOR);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.FUNCTION);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.GLOBAL);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.IF);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.IN);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.LOCAL);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.NIL);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.NOT);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.OR);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.RETURN);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.THEN);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.TRUE);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.WHILE);
            ExpectTrue(lex.GetNextToken().m_type == (int)TokenType.EOS);
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
            lex.Init(@"""${name..[=[ haha ha ]=]
..''''
..`ls -l`
..```bash `lalala`
```
} \n \t end""");
            ExpectTrue(lex.GetNextToken().EqualTo(TokenType.STRING_BEGIN));
            ExpectTrue(lex.CurStringType == Lex.BlockType.DoubleQuotation);
            {
                ExpectTrue(lex.GetNextToken().EqualTo('{'));
                ExpectTrue(lex.GetNextToken().EqualTo(TokenType.NAME));
                ExpectTrue(lex.GetNextToken().EqualTo(TokenType.CONCAT));
                ExpectTrue(lex.GetNextToken().EqualTo(TokenType.STRING));
                ExpectTrue(lex.GetNextToken().EqualTo(TokenType.CONCAT));
                ExpectTrue(lex.GetNextToken().EqualTo(TokenType.STRING_BEGIN));
                ExpectTrue(lex.CurStringType == Lex.BlockType.SingleQuotation);
                ExpectTrue(lex.GetNextToken().m_string == "'");
                ExpectTrue(lex.GetNextToken().EqualTo(TokenType.CONCAT));
                ExpectTrue(lex.GetNextToken().EqualTo(TokenType.STRING_BEGIN));
                ExpectTrue(lex.CurStringType == Lex.BlockType.InverseQuotation);
                ExpectTrue(lex.GetNextToken().m_string == "ls -l");
                ExpectTrue(lex.GetNextToken().EqualTo(TokenType.CONCAT));
                ExpectTrue(lex.GetNextToken().EqualTo(TokenType.STRING_BEGIN));
                ExpectTrue(lex.CurStringType == Lex.BlockType.InverseThreeQuotation);
                ExpectTrue(lex.GetNextToken().EqualTo(TokenType.STRING));
                ExpectTrue(lex.GetNextToken().EqualTo('}'));
            }
            ExpectTrue(lex.GetNextToken().m_string == " \n \t end");
            ExpectTrue(lex.IsStringEnded);
            ExpectTrue(lex.GetNextToken().EqualTo(TokenType.EOS));
        }
    }
}
