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

        protected override object _GetResults(Frame frame)
        {
            throw new BreakException(_line);
        }
    }
}
