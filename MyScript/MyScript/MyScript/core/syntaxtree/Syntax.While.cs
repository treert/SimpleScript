using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class WhileStatement : SyntaxTree
    {
        public WhileStatement(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree exp;
        public BlockTree block;

        protected override void _Exec(Frame frame)
        {
            while (true)
            {
                var obj = exp.GetResult(frame);
                if (Utils.ToBool(obj))
                {
                    block.Exec(frame);
                }
                else
                {
                    break;
                }
            }
        }
    }
}
