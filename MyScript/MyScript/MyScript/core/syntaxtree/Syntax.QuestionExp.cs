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
        public QuestionExp(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree a;// exp = a ? b : c
        public ExpSyntaxTree b;
        public ExpSyntaxTree c;
        public bool isqq = false;
        protected override List<object> _GetResults(Frame frame)
        {
            if (b == null)
            {
                var aa = a.GetResults(frame);
                var a1 = aa.Count > 0 ? aa[0] : null;
                if (isqq && a1 is null)
                {
                    return c.GetResults(frame);
                }
                else if(!isqq && Utils.ToBool(a1) == false)
                {
                    return c.GetResults(frame);
                }
                
                return aa;
            }
            else
            {
                var aa = a.GetBool(frame);
                return aa ? b.GetResults(frame) : c.GetResults(frame);
            }
        }
    }

}
