using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MyScript
{
    public abstract class SyntaxTree
    {
        protected int _line = -1;
        public int line
        {
            // set { _line = value; }
            get { return _line; }
        }

        public static implicit operator bool(SyntaxTree exsit)
        {
            return exsit != null;
        }

        public void Exec(Frame frame)
        {
            //try
            {
                _Exec(frame);
            }
        }

        protected virtual void _Exec(Frame frame) { }
    }

    // 表达式语法结构，可以返回一个或者多个结果。极端情况会返回0个，比如 ...
    // 性能浪费何其严重，Σ( ° △ °|||)︴
    public abstract class ExpSyntaxTree : SyntaxTree
    {
        protected override void _Exec(Frame frame)
        {
            _GetResults(frame);
        }

        public object GetResult(Frame frame)
        {
            // @om 要不要做异常行号计算？
            return _GetResults(frame);
        }

        public bool GetBool(Frame frame)
        {
            var x = GetResult(frame);
            return Utils.ToBool(x);
        }

        public string GetString(Frame frame)
        {
            var x = GetResult(frame);
            return Utils.ToString(x);
        }

        public double GetValidNumber(Frame frame)
        {
            var x = GetResult(frame);
            double f = Utils.ToNumber(x);
            // @om 这个接口做下double有效性判断
            if (double.IsNaN(f))
            {
                throw frame.NewRunException(line, "exp can not convert to valid double");
            }
            return f;
        }

        public MyNumber GetNumber(Frame frame)
        {
            var x = GetResult(frame);
            // todo@om
            double f = Utils.ToNumber(x);
            return f;
        }

        protected abstract object _GetResults(Frame frame);
    }

    public class BlockTree : SyntaxTree
    {
        public BlockTree(int line_)
        {
            _line = line_;
        }
        public List<SyntaxTree> statements = new List<SyntaxTree>();

        protected override void _Exec(Frame frame)
        {
            frame.EnterBlock();
            {
                foreach (var it in statements)
                {
                    it.Exec(frame);
                }
            }
            frame.LeaveBlock();
        }
    }


    public class NameList : SyntaxTree
    {
        public NameList(int line_)
        {
            _line = line_;
        }
        public List<Token> names = new List<Token>();

        public void AddLocals(Frame frame, params object[] objs)
        {
            for (int i = 0; i < names.Count; i++)
            {
                frame.AddLocalVal(names[i].m_string, objs.Length > i ? objs[i] : null);
            }
        }

        public void AddLocals(Frame frame, MyArray objs)
        {
            for (int i = 0; i < names.Count; i++)
            {
                frame.AddLocalVal(names[i].m_string, objs[i]);
            }
        }
    }

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
            if(idx == null)
            {
                ICall func = caller.GetResult(frame) as ICall;
                if(func == null)
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
                        if(it.key is string str)
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
