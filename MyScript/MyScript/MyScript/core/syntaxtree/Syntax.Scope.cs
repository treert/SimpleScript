using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class ScopeStatement:SyntaxTree
    {
        public ScopeStatement(int line_)
        {
            _line = line_;
        }

        public NameList name_list;
        public ExpressionList exp_list;
        public BlockTree block;

        protected override void _Exec(Frame frame)
        {
            frame.EnterBlock();
            {
                var results = exp_list.GetResults(frame);
                if (name_list)
                {
                    for (int i = 0; i < name_list.names.Count; i++)
                    {
                        var name = name_list.names[i];
                        var obj = results.Count > i ? results[i] : null;
                        frame.AddLocalVal(name.m_string, obj);
                    }
                }
                // 想了想，随便啦。
                //foreach(var ret in results)
                //{
                //    if(!(ret is IDisposable))
                //    {
                //        throw frame.NewRunException(exp_list.line, "scope params must be IDisposable");
                //    }
                //}
                try
                {
                    block.Exec(frame);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    foreach (var ret in results)
                    {
                        if (ret is IDisposable tmp)
                        {
                            tmp.Dispose();
                        }
                    }
                }
            }
            frame.LeaveBlock();

        }
    }
}