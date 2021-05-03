using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class FunctionStatement : SyntaxTree
    {
        public FunctionStatement(int line_)
        {
            _line = line_;
        }
        public FunctionName func_name;
        public FunctionBody func_body;

        protected override void _Exec(Frame frame)
        {
            var fn = func_body.GetResult(frame);
            if (func_name.names.Count == 1)
            {
                frame.Write(func_name.names[0].m_string, fn);
            }
            else
            {
                var names = func_name.names;
                var obj = frame.Read(names[0].m_string);
                if (obj == null)
                {
                    obj = frame.Write(names[0].m_string, new Table());
                }
                if (obj is not Table)
                {
                    throw frame.NewRunException(line, $"{names[0].m_string} is not Table which expect to be");
                }
                Table t = obj as Table;
                for (int i = 1; i < names.Count - 1; i++)
                {
                    var tt = t.Get(names[i].m_string);
                    if (tt == null)
                    {
                        tt = new Table();
                        t.Set(names[i].m_string, tt);
                    }
                    if (tt is Table == false)
                    {
                        throw frame.NewRunException(names[i].m_line, $"expect {names[i].m_string} to be a IGetSet");
                    }
                    t = tt as Table;
                }
                t.Set(names[names.Count - 1].m_string, fn);
            }
        }
    }

    public class FunctionName : SyntaxTree
    {
        public FunctionName(int line_)
        {
            _line = line_;
        }
        public List<Token> names = new List<Token>();
    }

    public class ParamList : SyntaxTree
    {
        public ParamList(int line_)
        {
            _line = line_;
        }
        public List<(Token token, ExpSyntaxTree? exp)> name_list = new List<(Token, ExpSyntaxTree?)>();
        public Token? ls_name = null;
        public Token? kw_name = null;
        public List<(Token token, ExpSyntaxTree? exp)> kw_list = new List<(Token, ExpSyntaxTree?)>();
    }


    public class FunctionBody : ExpSyntaxTree
    {
        public FunctionBody(int line_)
        {
            _line = line_;
        }
        public ParamList? param_list;
#nullable disable
        public BlockTree block;
        public string source_name;
#nullable restore

        protected override object _GetResults(Frame frame)
        {
            Function fn = new Function();
            fn.code = this;
            fn.vm = frame.func.vm;
            fn.module_table = frame.func.module_table;
            fn.upvalues = frame.GetAllUpvalues();

            // 默认参数

            return fn;
        }
    }

}
