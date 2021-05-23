using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class DoWhileStatement : SyntaxTree
    {
#nullable disable
        public DoWhileStatement(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
#nullable restore
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
