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
        public ExpressionList? exp_list;

        protected override object _GetResults(Frame frame)
        {
            ReturnException ep = new ReturnException();
            if (exp_list is not null)
            {
                ep.result = exp_list.GetResult(frame);
            }
            throw ep;
        }
    }
}
