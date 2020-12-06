using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class BreakStatement : ExpSyntaxTree
    {
        public BreakStatement(int line_)
        {
            _line = line_;
        }

        protected override List<object> _GetResults(Frame frame)
        {
            throw new BreakException(_line);
        }
    }
}
