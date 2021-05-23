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
        public int Line
        {
            get;
            internal set;
        } = -1;
        public string Source
        {
            get;
            internal set;
        } = string.Empty;

        public static implicit operator bool(SyntaxTree exsit)
        {
            return exsit != null;
        }

        public void Exec(Frame frame)
        {
            try
            {
                _Exec(frame);
            }
            catch (MyBaseException)
            {
                throw;
            }
            catch(Exception e)
            {
                throw new MyWrapException(e, Source, Line);
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

        public object? GetResult(Frame frame)
        {
            try
            {
                return _GetResults(frame);
            }
            catch (MyBaseException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new MyWrapException(e, Source, Line);
            }
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

        protected abstract object? _GetResults(Frame frame);
    }

    public class BlockTree : SyntaxTree
    {
        public BlockTree(int line_, string source)
        {
            Line = line_;
            Source = source;
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
            Line = line_;
        }
        public List<Token> names = new List<Token>();
        public bool is_global = false;// 放在这儿有好处，可以直接指导NameList是不是global的。目前也没什么用途

        public void DefineValues(Frame frame, object obj)
        {
            if (is_global) DefineGlobalValues(frame, obj);
            else DefineLocalValues(frame, obj);
        }

        public void DefineGlobalValues(Frame frame, object obj)
        {
            if (names.Count == 1)
            {
                frame.AddGlobalVal(names[0].m_string, obj);
            }
            else
            {
                if(obj is MyArray arr)
                {
                    for(int i = 0; i < names.Count; i++)
                    {
                        frame.AddGlobalVal(names[i].m_string, arr[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < names.Count; i++)
                    {
                        frame.AddGlobalVal(names[i].m_string, i == 0 ? obj : null);
                    }
                }
            }
        }
        public void DefineLocalValues(Frame frame, object? obj)
        {
            if (names.Count == 1)
            {
                frame.AddLocalVal(names[0].m_string, obj);
            }
            else
            {
                if (obj is MyArray arr)
                {
                    for (int i = 0; i < names.Count; i++)
                    {
                        frame.AddLocalVal(names[i].m_string, arr[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < names.Count; i++)
                    {
                        frame.AddLocalVal(names[i].m_string, i == 0 ? obj : null);
                    }
                }
            }
        }
    }
}
