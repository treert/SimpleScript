using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class IfStatement : SyntaxTree
    {
#nullable disable
        public IfStatement(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
#nullable restore
        public ExpSyntaxTree exp;
        public BlockTree true_branch;
        public SyntaxTree? false_branch;

        protected override void _Exec(Frame frame)
        {
            var ret = exp.GetBool(frame);
            if (ret)
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
