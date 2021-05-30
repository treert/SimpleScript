using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{

    public class MyBaseException : Exception, IGetSet
    {
        public virtual object? Get(object key)
        {
            if(key is string str)
            {
                if(str == "msg")
                {
                    return Message;
                }
            }
            return null;
        }

        public virtual void Set(object key, object? val)
        {
            // @om do noting
        }
    }

    public class MyLineException : MyBaseException
    {
        public string m_source = string.Empty;
        public int m_line = 0;
        public string m_msg = string.Empty;
        public override string Message => $"{m_source}:{m_line} {m_msg}";
    }

    public class MyColumnException : MyLineException
    {
        public int m_column = 0;
        public override string Message => $"{m_source}:{m_line}:{m_column} {m_msg}";
    }

    public class MyWrapException : MyLineException
    {
        public Exception inner;
        public MyWrapException(Exception inner, string source, int line)
        {
            this.inner = inner;
            m_source = source;
            m_line = line;
        }

        public override string Message => $"{m_source}:{m_line} {inner.Message}";

        // @om 也许能保持c#堆栈
        public override string? StackTrace => base.StackTrace;
    }

    public class ScriptException : MyBaseException
    {
        protected string _info = string.Empty;
        public override string Message
        {
            get
            {
                return _info;
            }
        }

        public void SetInfo(string info)
        {
            _info = info;
        }

        public void SetInfo(params object[] args)
        {
            var string_build = new StringBuilder();
            foreach (var obj in args)
            {
                string_build.Append(obj);
            }
            _info = string_build.ToString();
        }
    }

    #region 几个特殊的异常，只是利用了异常的机制来实现 return break continue，不算是错误。

    /// <summary>
    /// 利用异常返回结果，不算在异常错误里，多处代码都要特殊处理
    /// </summary>
    public class ReturnException : MyBaseException
    {
        public object? result = null;
    }

    /// <summary>
    /// 特殊异常，用来实现break，FuncCall需要特殊处理下
    /// </summary>
    public class BreakException : MyBaseException
    {
        public int line;
        public BreakException(int line)
        {
            this.line = line;
        }
    }

    /// <summary>
    /// 特殊异常，用来实现contine，FuncCall需要特殊处理下
    /// </summary>
    public class ContineException : MyBaseException
    {
        public int line;
        public ContineException(int line)
        {
            this.line = line;
        }
    }
    #endregion 

    public class ThrowException : MyLineException
    {
        public object? m_obj;

        public ThrowException(string source, int line, object? obj)
        {
            m_source = source;
            m_line = line;
            m_obj = obj;
        }

        public override string Message
        {
            get
            {
                return $"{m_source}:{m_line} throw: {m_obj}";
            }
        }

        public override object? Get(object key)
        {
            if ("obj".Equals(key))
            {
                return m_obj;
            }
            return base.Get(key);
        }
    }

    public class RunException : MyLineException
    {
        public RunException(string source, int line, string msg)
        {
            m_source = source;
            m_line = line;
            m_msg = msg;
        }
    }


    public class LexException : MyColumnException
    {
        public LexException(string source, int line, int column, string msg)
        {
            m_source = source;
            m_line = line;
            m_column = column;
            m_msg = msg;
        }
    }

    public class LexUnexpectEndException : LexException
    {
        public LexUnexpectEndException(string source_, int line_, int column_, string msg)
            : base(source_, line_, column_, msg)
        {
        }
    }

    public class ParserException : MyColumnException
    {
        public ParserException(string source, int line, int column, string msg)
        {
            m_source = source;
            m_line = line;
            m_column = column;
            m_msg = msg;
        }
    }

}
