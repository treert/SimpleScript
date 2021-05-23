using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class ReturnStatement : ExpSyntaxTree
    {
#nullable disable
        public ReturnStatement(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
#nullable restore
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
