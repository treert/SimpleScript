using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class ExpressionList : SyntaxTree
    {
#nullable disable
        public ExpressionList(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
#nullable restore
        public List<ExpSyntaxTree> exp_list = new List<ExpSyntaxTree>();
        public List<bool> split_flag_list = new List<bool>();

        public void AddExp(ExpSyntaxTree exp, bool split)
        {
            exp_list.Add(exp);
            split_flag_list.Add(split);
        }

        // 这样写代码会简洁很多，性能啥的不管了。
        public MyArray GetResultForSplit(Frame frame)
        {
            var obj = GetResult(frame);
            if(obj is MyArray arr)
            {
                return arr;
            }
            else
            {
                arr = new MyArray();
                arr.Add(obj);
                return arr;
            }
        }

        public object? GetResult(Frame frame)
        {
            if (exp_list.Count == 1)
            {
                // 不需要考虑拆分MyArray了
                return exp_list[0].GetResult(frame);
            }
            MyArray arr = new MyArray();
            int i = 0;
            for (; i < exp_list.Count; i++)
            {
                var exp = exp_list[i];
                var split = split_flag_list[i];
                arr.AddItem(exp.GetResult(frame), split);
            }
            return arr;
        }
    }

}
