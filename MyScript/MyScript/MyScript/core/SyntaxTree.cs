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

        public MyNumber GetNumber(Frame frame)
        {
            var x = GetResult(frame);
            return MyNumber.ForceConvertFrom(x);
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
}
