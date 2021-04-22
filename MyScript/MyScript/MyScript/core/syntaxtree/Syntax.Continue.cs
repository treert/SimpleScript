using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class ContinueStatement : ExpSyntaxTree
    {
        public ContinueStatement(int line_)
        {
            _line = line_;
        }

        protected override object _GetResults(Frame frame)
        {
            throw new ContineException(_line);
        }
    }
}
