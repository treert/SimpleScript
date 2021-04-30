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

        protected override object _GetResults(Frame frame)
        {
            if (idx == null)
            {
                ICall func = caller.GetResult(frame) as ICall;
                Func<Args,object> xx = func.Call;
                if (func == null)
                {
                    throw frame.NewRunException(caller.line, "expect something can call");
                }
                var args = this.args.GetArgs(frame);
                return func.Call(args);
            }
            else
            {
                var t = caller.GetResult(frame);
                var idx = this.idx.GetResult(frame);
                ICall func = ExtUtils.Get(t, idx) as ICall;
                if (func == null)
                {
                    throw frame.NewRunException(caller.line, "expect something can call");
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
        public class KW
        {
            public Token k;
            public ExpSyntaxTree w;
        }
        public List<ExpSyntaxTree> exp_list = new List<ExpSyntaxTree>();
        public ExpSyntaxTree kw_table = null;
        public List<KW> kw_exp_list = new List<KW>();

        public Args GetArgs(Frame frame)
        {
            Args args = new Args(frame);
            for (int i = 0; i < exp_list.Count; i++)
            {
                args.args.Add(exp_list[i].GetResult(frame));
            }
            if (kw_table != null)
            {
                // todo@om 这个实现不好
                Table table = kw_table.GetResult(frame) as Table;
                if (table != null)
                {
                    var it = table._itor_node.next;
                    while (it != table._itor_node)
                    {
                        if (it.key is string str)
                        {
                            args.name_args[str] = it.value;
                        }
                        it = it.next;
                    }
                }
                else
                {
                    // todo@om warning log
                }
            }
            foreach (var kw in kw_exp_list)
            {
                var val = kw.w.GetResult(frame);
                args.name_args[kw.k.m_string] = val;
            }
            return args;
        }
    }
}
