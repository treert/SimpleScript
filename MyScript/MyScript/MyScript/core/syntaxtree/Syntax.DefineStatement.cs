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
            var fn = func_body.GetOneResult(frame);
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
            var results = Utils.EmptyResults;
            if (exp_list)
            {
                results = exp_list.GetResults(frame);
            }
            for (int i = 0; i < name_list.names.Count; i++)
            {
                var name = name_list.names[i];
                var obj = results.Count > i ? results[i] : null;
                if (is_global)
                {
                    frame.AddGlobalName(name.m_string);
                    if (results.Count > i)
                    {
                        frame.func.vm.global_table[name.m_string] = obj;
                    }
                }
                else
                {
                    frame.AddLocalVal(name.m_string, obj);
                }
            }
        }
    }

}
