using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public abstract class DefineStatement : SyntaxTree
    {
        public bool is_global = false;
    }

    public class DefineFunctionStatement : DefineStatement
    {
        public DefineFunctionStatement(int line_)
        {
            _line = line_;
        }

        public Token name;
        public FunctionBody func_body;
        protected override void _Exec(Frame frame)
        {
            if (is_global)
            {
                frame.AddGlobalName(name.m_string);
            }
            else
            {
                frame.AddLocalName(name.m_string);
            }
            // 用这种方法可以实现local 递归函数
            var fn = func_body.GetResult(frame);
            frame.Write(name.m_string, fn);
        }
    }

    public class DefineNameListStatement : DefineStatement
    {
        public DefineNameListStatement(int line_)
        {
            _line = line_;
        }
        public NameList name_list;
        public ExpressionList exp_list;

        protected override void _Exec(Frame frame)
        {
            name_list.DefineValues(frame, exp_list?.GetResult(frame));
        }
    }

}
