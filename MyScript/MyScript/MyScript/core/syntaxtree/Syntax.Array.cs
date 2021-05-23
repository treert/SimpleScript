using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class ArrayDefine : ExpSyntaxTree
    {
        public ArrayDefine(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
        public List<(ExpSyntaxTree exp, bool split)> fileds = new();
        protected override object _GetResults(Frame frame)
        {
            MyArray ret = new MyArray();
            foreach(var f in fileds)
            {
                var val = f.exp.GetResult(frame);
                ret.AddItem(val, f.split);
            }
            return ret;
        }
    }
}
