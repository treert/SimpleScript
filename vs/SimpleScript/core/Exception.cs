using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    public class ScriptException:Exception
    {

    }

    public class BaseException : ScriptException
    {
        protected string _info = string.Empty;
        public override string Message
        {
            get
            {
                return _info;
            }
        }

        protected void SetInfo(params object[] args)
        {
            var string_build = new StringBuilder();
            foreach(var obj in args)
            {
                string_build.Append(obj);
            }
            _info = string_build.ToString();
        }
    }
    public class LexException : BaseException
    {
        public LexException(string source_, int line_, int column_,string msg)
        {
            SetInfo(source_, ":", line_, ":", column_, " ",msg);
        }
    }

    public class ParserException : BaseException
    {
        public ParserException(string source_, int line_, int column_, string msg)
        {
            SetInfo(source_, ":", line_, ":", column_, " ", msg);
        }
    }

    public class CodeGenerateException : BaseException
    {
        public CodeGenerateException(string source_, int line_, string msg)
        {
            SetInfo(source_, ":", line_, " ", msg);
        }
    }

    public class RuntimeException : BaseException
    {
        public RuntimeException(string source_, int line_, string format, params object[] args)
        {
            SetInfo(source_, ":", line_, " ", string.Format(format, args));
        }

        private string[] _trace_back;
        public void SetTraceBackInfo(params string[] infos)
        {
            // todo@om
            _trace_back = infos;
        }
    }

    public class CFunctionException : BaseException
    {
        public CFunctionException(string format, params object[] args)
        {
            _info = string.Format(format, args);
        }
    }

    class OtherException : ScriptException
    {
        private string _info = string.Empty;
        public override string Message
        {
            get
            {
                return _info;
            }
        }

        public OtherException(string format, params object[] args)
        {
            _info = string.Format(format, args);
        }
    }
}
