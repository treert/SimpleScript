using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MyScript
{
    /// <summary>
    /// > https://gist.github.com/mjs3339/73042bc0e717f98796ee9fa131e458d4
    /// </summary>
    public static class BigIntegerHelper
    {
        public static BigInteger? TryParseToBigIntegerBase2(this string str)
        {
            BigInteger big = 0;
            foreach(var ch in str)
            {
                big <<= 1;
                int n = ch - '0';
                if(n >= 0 && n < 2)
                {
                    big += n;
                }
                else
                {
                    return null;
                }
            }
            return big;
        }
        public static BigInteger? TryParseToBigIntegerBase8(this string str)
        {
            BigInteger big = 0;
            foreach (var ch in str)
            {
                big <<= 1;
                int n = ch - '0';
                if (n >= 0 && n < 8)
                {
                    big += n;
                }
                else
                {
                    return null;
                }
            }
            return big;
        }

    }
}
