using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class DoWhileStatement : SyntaxTree
    {
        public DoWhileStatement(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree exp;
        public BlockTree block;

        protected override void _Exec(Frame frame)
        {
            while (true)
            {
                block.Exec(frame);

                var obj = exp.GetResult(frame);
                if (!Utils.ToBool(obj))
                {
                    break;
                }
            }
        }
    }
}
