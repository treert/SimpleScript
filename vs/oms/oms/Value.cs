using System;
using System.Diagnostics;
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
    class ValueUtils
    {
        public static bool IsFalse(object obj)
        {
            if(obj == null)
                return true;
            else
            {
                return (obj is bool) && ((bool)(obj) == false);
            }
        }

        public static double ToNumber(object obj)
        {
            if (obj is double)
                return (double)obj;
            return 0;
        }
    }

    class UpValue
    {
        public object obj = null;
        public int idx = -1;
        public bool IsClosed()
        {
            return idx == -1;
        }

        public void Close(object obj_)
        {
            obj = obj_;
            idx = -1;
        }
    }
    class Closure
    {
        public Function func = null;
        public Closure parent = null;
        public void AddUpvalue(UpValue upvalue_)
        {
            _upvalues.Add(upvalue_);
        }

        public UpValue GetUpvalue(int idx)
        {
            return _upvalues[idx];
        }

        List<UpValue> _upvalues = new List<UpValue>();
    }

    class Table
    {
        public void SetValue(object key, object value)
        {
            Debug.Assert(key != null);
            _dic[key] = value;
        }
        public object GetValue(object key)
        {
            if (_dic.ContainsKey(key))
                return _dic[key];
            return null;
        }

        Dictionary<object, object> _dic = new Dictionary<object, object>();
    }

    delegate int CFunction(Thread th);
}
