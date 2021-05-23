using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyScript
{
    public class AssignStatement : SyntaxTree
    {
#nullable disable
        public AssignStatement(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
#nullable restore

        public List<ExpSyntaxTree> var_list = new List<ExpSyntaxTree>();
        public ExpressionList exp_list;

        protected override void _Exec(Frame frame)
        {
            if(var_list.Count == 1)
            {
                _AssginOne(frame, var_list[0], exp_list.GetResult(frame));
            }
            else
            { 
                MyArray results = exp_list.GetResultForSplit(frame);
                for (int i = 0; i < var_list.Count; i++)
                {
                    _AssginOne(frame, var_list[i], results[i]);
                }
            }
        }

        void _AssginOne(Frame frame, ExpSyntaxTree it, object val)
        {
            if (it is TableAccess)
            {
                (it as TableAccess).Assign(frame, val);
            }
            else
            {
                // Name
                var ter = it as Terminator;
                Debug.Assert(ter.token.Match(TokenType.NAME));
                var name = ter.token.m_string;
                frame.Write(name, val);
            }
        }
    }

}
