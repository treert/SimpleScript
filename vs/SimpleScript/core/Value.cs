using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    /// <summary>
    /// 偷个懒，使用c#本身的object
    /// nil         : null
    /// number      : double
    /// string      : string
    /// bool        : bool
    /// table       : Table
    /// table.iter  : Table.Iterator
    /// userdata    : other get & set
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

    public interface IUserData
    {
        object Get(object name);
        void Set(object name, object value);
    }

    public class Table
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

        public int Count()
        {
            return _dic.Count;
        }

        internal Iterator GetIter()
        {
            return new Iterator(this);
        }

        internal class Iterator
        {
            Table _table;
            Dictionary<object, object>.Enumerator _iter;
            public Iterator(Table table)
            {
                _table = table;
                _iter = table._dic.GetEnumerator();
            }

            public bool Next(out object key, out object value)
            {
                if(_iter.MoveNext())
                {
                    key = _iter.Current.Key;
                    value = _iter.Current.Value;
                    return true;
                }
                else
                {
                    key = null;
                    value = null;
                    return false;
                }
            }
        }

        Dictionary<object, object> _dic = new Dictionary<object, object>();
    }

    public delegate int CFunction(Thread th);
}
