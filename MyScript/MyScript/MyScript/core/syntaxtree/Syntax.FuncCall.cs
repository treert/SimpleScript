using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyScript
{
    public class FuncCall : ExpSyntaxTree
    {
#nullable disable
        public FuncCall(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
#nullable restore
        public ExpSyntaxTree caller;
        public ExpSyntaxTree? idx;// caller() or caller.idx(args)
        public ArgsList args;

        protected override object? _GetResults(Frame frame)
        {
            if (idx == null)
            {
                ICall? func = caller.GetResult(frame) as ICall;
                if (func != null)
                {
                    var args = this.args.GetArgs(frame);
                    return func.Call(args);
                }
                else
                {
                    // @om do nothing
                }
            }
            else
            {
                var t = caller.GetResult(frame);
                var idx = this.idx.GetResult(frame);
                if(t != null && idx != null)
                {
                    var obj = ExtUtils.Get(t, idx);
                    if(obj is ICall func)
                    {
                        var args = this.args.GetArgs(frame);
                        args.that = t;
                        return func.Call(args);
                    }
                    else
                    {
                        // @om do nothing
                    }
                }
            }
            return null;
        }
    }

    public class ArgsList : SyntaxTree
    {
        public ArgsList(int line_)
        {
            Line = line_;
        }

        public List<(ExpSyntaxTree exp, bool split)> exp_list = new();
        // name == null when **exp
        public List<(Token? name, ExpSyntaxTree exp)> kw_list = new();

        public MyArgs GetArgs(Frame frame)
        {
            MyArgs args = new MyArgs(frame);
            foreach(var it in exp_list)
            {
                args.args.AddItem(it.exp.GetResult(frame), it.split);
            }
            foreach(var it in kw_list)
            {
                var ret = it.exp.GetResult(frame);
                if(it.name is null)
                {
                    if(ret is MyTable t)
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
