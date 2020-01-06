using System;
using System.Collections.Generic;
using System.Text;

namespace SScript
{
    public class Config
    {
        public const string MAGIC_THIS = "this";
        public static string def_shell = "bash";

        // 稍微优化下性能，(/ □ \)
        public static readonly List<object> EmptyResults = new List<object>();

        public const long MaxSafeInt = 9007199254740991;
        public const long MinSafeInt = -9007199254740991;
    }
}
