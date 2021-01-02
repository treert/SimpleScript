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
        public object GetOneResult(Frame frame)
        {
            var x = GetResults(frame);
            return x.Count > 0 ? x[0] : null;
        }

        public bool GetBool(Frame frame)
        {
            var x = GetResults(frame);
            return x.Count > 0 ? Utils.ToBool(x[0]) : false;
        }

        public string GetString(Frame frame)
        {
            var x = GetResults(frame);
            return x.Count > 0 ? Utils.ToString(x[0]) : "";
        }

        public double GetValidNumber(Frame frame)
        {
            var x = GetResults(frame);
            double f = Utils.ToNumber(x.GetValueOrDefault(0));
            // @om 这个接口做下double有效性判断
            if (double.IsNaN(f))
            {
                throw frame.NewRunException(line, "exp can not convert to valid double");
            }
            return f;
        }

        public double GetNumber(Frame frame)
        {
            var x = GetResults(frame);
            double f = Utils.ToNumber(x.GetValueOrDefault(0));
            return f;
        }

        public List<object> GetResults(Frame frame)
        {
            // @om 要不要做异常行号计算？
            return _GetResults(frame);
        }

        protected abstract List<object> _GetResults(Frame frame);
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

        public void AddLocals(Frame frame, List<object> objs)
        {
            for (int i = 0; i < names.Count; i++)
            {
                frame.AddLocalVal(names[i].m_string, objs.Count > i ? objs[i] : null);
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

        protected override List<object> _GetResults(Frame frame)
        {
            ICall func = caller.GetOneResult(frame) as ICall;
            if (func == null)
            {
                throw frame.NewRunException(caller.line, "expect fn or ext_fn to call");
            }

            var args = this.args.GetArgs(frame);
            object that = caller is TableAccess ? (caller as TableAccess).table : null;

            return func.Call(args);
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
            for (int i = 0; i < exp_list.Count - 1; i++)
            {
                args.args.Add(exp_list[i].GetOneResult(frame));
            }
            if (exp_list.Count > 0)
            {
                var rets = exp_list.Last().GetResults(frame);
                args.args.AddRange(rets);
            }
            if (kw_table != null)
            {
                Table table = kw_table.GetOneResult(frame) as Table;
                if (table != null)
                {

                    // todo
                }
                // 要不要报个错
            }
            foreach (var kw in kw_exp_list)
            {
                var val = kw.w.GetOneResult(frame);
                args.name_args[kw.k.m_string] = val;
            }
            return args;
        }
    }

    public class ExpressionList : ExpSyntaxTree
    {
        public ExpressionList(int line_)
        {
            _line = line_;
        }
        public List<ExpSyntaxTree> exp_list = new List<ExpSyntaxTree>();

        protected override List<object> _GetResults(Frame frame)
        {
            List<object> list = new List<object>(exp_list.Count);
            for (int i = 0; i < exp_list.Count - 1; i++)
            {
                list.Add(exp_list[i].GetOneResult(frame));
            }
            if (exp_list.Count > 0)
            {
                var rets = exp_list.Last().GetResults(frame);
                list.AddRange(rets);
            }
            return list;
        }
    }

}
