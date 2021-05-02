using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyScript
{
    public class UnaryExpression : ExpSyntaxTree
    {
        public UnaryExpression(int line_)
        {
            _line = line_;
        }
#nullable disable
        public ExpSyntaxTree exp;
        public Token op;
#nullable restore

        protected override object _GetResults(Frame frame)
        {
            if (op.Match('-'))
            {
                return - exp.GetNumber(frame);
            }
            else if (op.Match(Keyword.NOT))
            {
                return !exp.GetBool(frame);
            }
            else if (op.Match('~'))
            {
                return ~exp.GetNumber(frame);
            }
            else
            {
                Debug.Assert(false);
            }
            return null;
        }
    }

}
