using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class ContinueStatement : ExpSyntaxTree
    {
        public ContinueStatement(int line_, string source)
        {
            Line = line_;
            Source = source;
        }

        protected override object _GetResults(Frame frame)
        {
            throw new ContineException(Line);
        }
    }
}
