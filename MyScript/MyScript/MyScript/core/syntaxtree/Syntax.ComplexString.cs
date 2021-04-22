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
        public List<ExpSyntaxTree> list = new List<ExpSyntaxTree>();

        protected override object _GetResults(Frame frame)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.Append(item.GetString(frame));
            }
            return sb.ToString();
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

        protected override object _GetResults(Frame frame)
        {
            var obj = exp.GetResult(frame);
            string str = Utils.ToString(obj, format, len);
            return str;
        }
    }

}
