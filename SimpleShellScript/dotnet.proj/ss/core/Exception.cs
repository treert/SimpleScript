using System;
using System.Collections.Generic;
using System.Text;

namespace SScript
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
    

    public class ReturnException : ScriptException
    {
        public List<object> results = Config.EmptyResults;
    }

    public class BreakException : ScriptException
    {
        public int line;
        public BreakException(int line)
        {
            this.line = line;
        }
    }

    public class ContineException : ScriptException
    {
        public int line;
        public ContineException(int line)
        {
            this.line = line;
        }
    }

    public class ThrowException : Exception
    {
        public int line;
        public string source_name;
        public object obj;
        public override string Message
        {
            get
            {
                return $"{source_name}:{line} throw exception";
            }
        }
    }

    public class RunException : ScriptException
    {
        public RunException(string source_, int line_, string msg)
        {
            SetInfo(source_, ":", line_, " ", msg);
        }
    }


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

    class OtherException : ScriptException
    {
        public OtherException(string format, params object[] args)
        {
            _info = string.Format(format, args);
        }
    }
}
