using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{

    public class ComplexString : ExpSyntaxTree
    {
        public ComplexString(int line_)
        {
            _line = line_;
        }
        public bool is_shell = false;// ` and ```
        public string shell_name = null;// 默认空的执行时取 Config.def_shell
        public List<ExpSyntaxTree> list = new List<ExpSyntaxTree>();

        protected override List<object> _GetResults(Frame frame)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.Append(item.GetString(frame));
            }
            if (is_shell)
            {
                // todo
                throw new NotImplementedException();
            }
            else
            {
                return new List<object>() { sb.ToString() };
            }
        }
    }

    public class ComplexStringItem : ExpSyntaxTree
    {
        public ComplexStringItem(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree exp;
        public int len = 0;
        public string format = null;

        protected override List<object> _GetResults(Frame frame)
        {
            var obj = exp.GetOneResult(frame);
            string str = Utils.ToString(obj, format, len);
            return new List<object>() { str };
        }
    }

}
