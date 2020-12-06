using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class ThrowStatement : ExpSyntaxTree
    {
        public ThrowStatement(int line)
        {
            _line = line;
        }
        public ExpSyntaxTree exp;

        protected override List<object> _GetResults(Frame frame)
        {
            ThrowException ep = new ThrowException();
            ep.line = _line;
            ep.source_name = frame.func.code.source_name;
            if (exp)
            {
                ep.obj = exp.GetOneResult(frame);
            }
            throw ep;
        }
    }
}
