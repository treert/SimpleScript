using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class FunctionStatement : SyntaxTree
    {
#nullable disable
        public FunctionStatement(int line_)
        {
            _line = line_;
        }
#nullable restore
        public FunctionName func_name;
        public FunctionBody func_body;

        protected override void _Exec(Frame frame)
        {
            var fn = func_body.GetResult(frame)!;
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
                    obj = frame.Write(names[0].m_string, new MyTable());
                }
                if (obj is not IGetSet)
                {
                    throw frame.NewRunException(line, $"{names[0].m_string} is not IGetSet which expect to be");
                }
#nullable disable
                IGetSet t = obj as IGetSet;
                for (int i = 1; i < names.Count - 1; i++)
                {
                    var tt = t.Get(names[i].m_string);
                    if (tt == null)
                    {
                        tt = new MyTable();
                        t.Set(names[i].m_string, tt);
                    }
                    if (tt is IGetSet == false)
                    {
                        throw frame.NewRunException(names[i].m_line, $"expect {names[i].m_string} to be a IGetSet");
                    }
                    t = tt as IGetSet;
                }
                t.Set(names[names.Count - 1].m_string, fn);
#nullable restore
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

        public string? Check()
        {
            HashSet<string> set = new HashSet<string>();
            foreach(var it in name_list)
            {
                if (set.Contains(it.token.m_string))
                {
                    return $"arg '{it.token.m_string}' name duplicate";
                }
                set.Add(it.token.m_string);
            }
            if(ls_name != null)
            {
                if (set.Contains(ls_name.m_string))
                {
                    return $"arg '{ls_name.m_string}' name duplicate";
                }
                set.Add(ls_name.m_string);
            }
            if (kw_name != null)
            {
                if (set.Contains(kw_name.m_string))
                {
                    return $"arg '{kw_name.m_string}' name duplicate";
                }
                set.Add(kw_name.m_string);
            }
            foreach (var it in kw_list)
            {
                if (set.Contains(it.token.m_string))
                {
                    return $"arg '{it.token.m_string}' name duplicate";
                }
                set.Add(it.token.m_string);
            }
            return null;
        }
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
            return CreateFunction(frame.func.vm, frame.func.module_table, frame);
        }

        /// <summary>
        /// 直接构建Function
        /// 1. loadfile 加载源码文件。module == null and frame == null
        /// 2. cmdline，一行行执行。frame == null
        /// 3. loadstring 加载字符串。和2效果一样。frame == null
        /// 4. 内置解析。
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        public MyFunction CreateFunction(VM vm, MyTable? module = null, Frame? frame = null)
        {
            MyFunction fn = new MyFunction();
            fn.code = this;
            fn.vm = vm;
            fn.module_table = module ?? new MyTable();
            fn.upvalues = frame != null ? frame.GetAllUpvalues() : new Dictionary<string, LocalValue?>();

            frame ??= new Frame(fn);

            // 默认参数
            if (param_list is not null)
            {
                foreach (var it in param_list.name_list)
                {
                    fn.default_args[it.token.m_string!] = it.exp?.GetResult(frame);
                }
                foreach (var it in param_list.kw_list)
                {
                    fn.default_args[it.token.m_string!] = it.exp?.GetResult(frame);
                }
            }

            return fn;
        }
    }

}
