using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    public static class OmsConf 
    {
        public const int MAX_FUNC_REGISTER = 256;
        public const int MAX_STACK_SIZE = 10000;
        public const int BX_MIN = Int16.MinValue;
        public const int BX_MAX = Int16.MaxValue;
        public const int MAX_CFUNC_ARG_COUNT = 32;
        public const string MAGIC_THIS = "this";
    }
}
