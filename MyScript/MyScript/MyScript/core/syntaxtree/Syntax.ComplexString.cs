using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{

    public class ComplexString : ExpSyntaxTree
    {
        public ComplexString(int line_)
        {
            Line = line_;
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
#nullable disable
        public ComplexStringItem(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
#nullable restore
        public ExpSyntaxTree exp;
        public int len = 0;
        public string? format = null;

        protected override object _GetResults(Frame frame)
        {
            var obj = exp.GetResult(frame);
            string ret = string.Empty;
            if(obj is string str)
            {
                ret = str;
            }
            else if (obj is not null)
            {
                obj = MyNumber.TryConvertFrom(obj) ?? obj;
                if(format != null && obj is IFormattable formater)
                {
                    ret = formater.ToString(format, null);
                }
                else
                {
                    ret = obj.ToString() ?? string.Empty;
                }
            }
            // padding
            if (len > ret.Length)
            {
                ret = ret.PadLeft(len - ret.Length);
            }
            else if (len < -ret.Length)
            {
                ret = ret.PadRight(-len - ret.Length);
            }
            return ret;
        }
    }

}
