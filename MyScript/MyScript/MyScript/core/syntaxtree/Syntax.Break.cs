using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class BreakStatement : ExpSyntaxTree
    {
        public BreakStatement(int line_, string source)
        {
            Line = line_;
            Source = source;
        }

        protected override object _GetResults(Frame frame)
        {
            throw new BreakException(Line);
        }
    }
}
