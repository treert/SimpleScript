using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyScript
{
    public class UnaryExpression : ExpSyntaxTree
    {
#nullable disable
        public UnaryExpression(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
#nullable restore
        public ExpSyntaxTree exp;
        public Token op;

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
