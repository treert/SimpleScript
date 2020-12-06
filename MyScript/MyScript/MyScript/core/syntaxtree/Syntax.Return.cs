using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class ReturnStatement : ExpSyntaxTree
    {
        public ReturnStatement(int line_)
        {
            _line = line_;
        }
        public ExpressionList exp_list;

        protected override List<object> _GetResults(Frame frame)
        {
            ReturnException ep = new ReturnException();
            if (exp_list)
            {
                ep.results = exp_list.GetResults(frame);
            }
            throw ep;
        }
    }
}
