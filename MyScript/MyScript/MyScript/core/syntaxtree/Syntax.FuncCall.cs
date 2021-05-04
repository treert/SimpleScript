using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyScript
{
    public class FuncCall : ExpSyntaxTree
    {
        public FuncCall(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree caller;
        public ExpSyntaxTree idx;// caller() or caller.idx(args)
        public ArgsList args;

        protected override object? _GetResults(Frame frame)
        {
            if (idx == null)
            {
                ICall? func = caller.GetResult(frame) as ICall;
                if (func == null)
                {
                    throw frame.NewRunException(caller.line, "expect ICall to call");
                }
                var args = this.args.GetArgs(frame);
                return func.Call(args);
            }
            else
            {
                var t = caller.GetResult(frame);
                var idx = this.idx.GetResult(frame);
                ICall? func = ExtUtils.Get(t, idx) as ICall;
                if (func == null)
                {
                    throw frame.NewRunException(caller.line, "expect ICall to call");
                }
                var args = this.args.GetArgs(frame);
                args.that = t;
                return func.Call(args);
            }
        }
    }

    public class ArgsList : SyntaxTree
    {
        public ArgsList(int line_)
        {
            _line = line_;
        }

        public List<(ExpSyntaxTree exp, bool split)> exp_list = new();
        // name == null when **exp
        public List<(Token? name, ExpSyntaxTree exp)> kw_list = new();

        public Args GetArgs(Frame frame)
        {
            Args args = new Args(frame);
            foreach(var it in exp_list)
            {
                args.args.AddItem(it.exp.GetResult(frame), it.split);
            }
            foreach(var it in kw_list)
            {
                var ret = it.exp.GetResult(frame);
                if(it.name is null)
                {
                    if(ret is Table t)
                    {
                        foreach(var item in t.GetItemNodeItor())
                        {
                            if(item.key is string str)
                            {
                                args.name_args[str] = item.value;
                            }
                        }
                    }
                    else
                    {
                        // todo@om do nothing?
                    }
                }
                else
                {
                    args.name_args[it.name.m_string!] = ret;
                }
            }
            return args;
        }
    }
}
