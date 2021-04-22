using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    // 实现类似c# using 的功能
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
            if(block) frame.EnterBlock();
            {
                var results = exp_list.GetResultForSplit(frame);
                if (name_list)
                {
                    for (int i = 0; i < name_list.names.Count; i++)
                    {
                        var name = name_list.names[i];
                        var obj = results[i];
                        frame.AddLocalVal(name.m_string, obj);
                    }
                }
                frame.AddScopeObjs(results);
                if (block) block.Exec(frame);
            }
            if (block) frame.LeaveBlock();
        }
    }
}