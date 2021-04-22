using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class IfStatement : SyntaxTree
    {
        public IfStatement(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree exp;
        public BlockTree true_branch;
        public SyntaxTree false_branch;

        protected override void _Exec(Frame frame)
        {
            var obj = exp.GetResult(frame);
            if (Utils.ToBool(obj))
            {
                true_branch.Exec(frame);
            }
            else
            {
                false_branch?.Exec(frame);
            }
        }
    }
}
