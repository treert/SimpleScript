using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{
    class BaseException:Exception
    {
        private string _info = string.Empty;
        public string Info
        {
            get
            {
                return _info;
            }
        }

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
    class LexException:BaseException
    {
        public LexException(string source_, int line_, int column_,string msg)
        {
            SetInfo(source_, ":", line_, ":", column_, " ",msg);
        }
    }

    class ParserException:BaseException
    {
        public ParserException(string msg)
        {
            SetInfo(msg);
        }
    }

    class SematicException : BaseException
    {
        public SematicException(string msg)
        {
            SetInfo(msg);
        }
    }
}
