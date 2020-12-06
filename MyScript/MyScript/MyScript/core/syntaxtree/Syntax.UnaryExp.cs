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
        public ExpSyntaxTree exp;
        public Token op;

        protected override List<object> _GetResults(Frame frame)
        {
            object ret = null;
            if (op.Match('-'))
            {
                ret = exp.GetValidNumber(frame);
            }
            else if (op.Match(Keyword.NOT))
            {
                ret = !exp.GetBool(frame);
            }
            else
            {
                Debug.Assert(false);
            }
            return new List<object>() { ret };
        }
    }

}
