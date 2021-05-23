using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    /// <summary>
    /// a ? b : c
    /// a ?: c
    /// a ?? c (a == null)
    /// 
    /// c can be special statement,like return，check MyScript.bnf
    /// </summary>
    public class QuestionExp : ExpSyntaxTree
    {
#nullable disable
        public QuestionExp(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
#nullable restore
        public ExpSyntaxTree a;// exp = a ? b : c
        public ExpSyntaxTree b;
        public ExpSyntaxTree c;
        public bool isqq = false;
        protected override object _GetResults(Frame frame)
        {
            if (b == null)
            {
                var aa = a.GetResult(frame);
                if (isqq && aa is null)
                {
                    return c.GetResult(frame);
                }
                else if(!isqq && Utils.ToBool(aa) == false)
                {
                    return c.GetResult(frame);
                }
                return aa;
            }
            else
            {
                var aa = a.GetBool(frame);
                return aa ? b.GetResult(frame) : c.GetResult(frame);
            }
        }
    }

}
