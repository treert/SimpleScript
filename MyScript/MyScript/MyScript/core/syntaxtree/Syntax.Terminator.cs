using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyScript
{
    public class Terminator : ExpSyntaxTree
    {
        public Token token;
        public Terminator(Token token_)
        {
            token = token_;
            _line = token_.m_line;
        }

        protected override List<object> _GetResults(Frame frame)
        {
            if (token.Match(TokenType.DOTS))
            {
                return frame.extra_args;
            }

            object obj = null;
            if (token.Match(TokenType.NAME))
            {
                obj = frame.Read(token.m_string);
            }
            else if (token.Match(Keyword.NIL))
            {
                obj = null;
            }
            else if (token.Match(Keyword.TRUE))
            {
                obj = true;
            }
            else if (token.Match(Keyword.FALSE))
            {
                obj = false;
            }
            else if (token.Match(TokenType.NUMBER))
            {
                obj = token.m_number;
            }
            else if (token.Match(TokenType.STRING))
            {
                obj = token.m_string;
            }
            else
            {
                Debug.Assert(false);
            }
            return new List<object>() { obj };
        }
    }


}
