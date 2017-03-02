using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{
    /// <summary>
    /// 偷个懒，使用c#本身的object
    /// nil         : null
    /// number      : double
    /// string      : string
    /// bool        : bool
    /// closure     : Closure
    /// userdata    : other
    /// </summary>
    struct OValue
    {
        object obj;

    }

    class UpValue
    {
        public object obj;
        public int idx = -1;
        bool IsClosed()
        {
            return idx == -1;
        }
    }
    class Closure
    {
        public Function func;
    }
}
