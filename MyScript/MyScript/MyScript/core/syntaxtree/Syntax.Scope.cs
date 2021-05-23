using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    // 实现类似c# using 的功能
    public class ScopeStatement:SyntaxTree
    {
#nullable disable
        public ScopeStatement(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
#nullable restore
        public NameList? name_list;
        public ExpressionList exp_list;

        protected override void _Exec(Frame frame)
        {
            var results = exp_list.GetResult(frame);
            name_list?.DefineLocalValues(frame, results);
            frame.AddScopeObj(results);
        }
    }
}