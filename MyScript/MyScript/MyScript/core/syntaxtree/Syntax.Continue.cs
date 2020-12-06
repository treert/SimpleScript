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

        protected override List<object> _GetResults(Frame frame)
        {
            throw new ContineException(_line);
        }
    }
}
