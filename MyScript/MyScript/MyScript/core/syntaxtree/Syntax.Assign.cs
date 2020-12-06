using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyScript
{
    public class AssignStatement : SyntaxTree
    {
        public AssignStatement(int line_)
        {
            _line = line_;
        }
        public List<ExpSyntaxTree> var_list = new List<ExpSyntaxTree>();
        public ExpressionList exp_list;

        protected override void _Exec(Frame frame)
        {
            var results = exp_list.GetResults(frame);
            for (int i = 0; i < var_list.Count; i++)
            {
                var it = var_list[i];
                object val = results.Count > i ? results[i] : null;
                if (it is TableAccess)
                {
                    // TableAccess
                    var access = it as TableAccess;
                    var table = access.table.GetOneResult(frame);
                    var idx = access.index.GetOneResult(frame);
                    if (idx == null)
                    {
                        throw frame.NewRunException(line, "table index can not be null");
                    }

                    ExtUtils.Set(table, idx, val);
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

}
