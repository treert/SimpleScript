using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class ArrayDefine : ExpSyntaxTree
    {
        public ArrayDefine(int line_)
        {
            _line = line_;
        }
        public List<ExpSyntaxTree> fileds = new List<ExpSyntaxTree>();
        protected override List<object> _GetResults(Frame frame)
        {
            MyArray ret = new MyArray();
            foreach(var f in fileds)
            {
                var val = f.GetOneResult(frame);
                ret.m_items.Add(val);
            }

            return new List<object>() { ret };
        }
    }
}
