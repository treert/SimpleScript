using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleScript
{
    public class ScriptException : Exception
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
            foreach (var obj in args)
            {
                string_build.Append(obj);
            }
            _info = string_build.ToString();
        }
    }

    public class BreakException : ScriptException
    {

    }

    public class ContineException : ScriptException { }


    public class LexException : ScriptException
    {
        public LexException(string source_, int line_, int column_, string msg)
        {
            SetInfo(source_, ":", line_, ":", column_, " ", msg);
        }
    }

    public class ParserException : ScriptException
    {
        public ParserException(string source_, int line_, int column_, string msg)
        {
            SetInfo(source_, ":", line_, ":", column_, " ", msg);
        }
    }

    public class CodeGenerateException : ScriptException
    {
        public CodeGenerateException(string source_, int line_, string msg)
        {
            SetInfo(source_, ":", line_, " ", msg);
        }
    }

    public class RuntimeException : ScriptException
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

    public class CFunctionException : ScriptException
    {
        public CFunctionException(string format, params object[] args)
        {
            _info = string.Format(format, args);
        }
    }

    class OtherException : ScriptException
    {
        public OtherException(string format, params object[] args)
        {
            _info = string.Format(format, args);
        }
    }
}
