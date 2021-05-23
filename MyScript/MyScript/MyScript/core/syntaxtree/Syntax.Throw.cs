using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class ThrowStatement : ExpSyntaxTree
    {
#nullable disable
        public ThrowStatement(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
#nullable restore
        public ExpSyntaxTree exp;

        protected override object _GetResults(Frame frame)
        {
            ThrowException ep = new ThrowException(Source, Line, exp?.GetResult(frame));
            throw ep;
        }
    }
}
