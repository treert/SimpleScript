﻿using System;
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

        public static string ToString(object obj)
        {
            if (obj is string)
                return (string)obj;
            return obj.ToString();
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

        public void Add(object value)
        {
            int key = _dic.Count + 1;
            _dic.Add((double)key, value);
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

    class UpValue
    {
        object _obj = null;
        object[] _stack = null;// because memory ptr is unsafe in c#
        public readonly int idx = 0;

        public UpValue(object[] stack_, int idx_)
        {
            _stack = stack_;
            idx = idx_;
        }

        public object Read()
        {
            if (_stack == null)
                return _obj;
            else
                return _stack[idx];
        }
        public void Write(object obj_)
        {
            if (_stack == null)
                _obj = obj_;
            else
                _stack[idx] = obj_;
        }
        public bool IsClosed()
        {
            return _stack == null;
        }
        public void Close()
        {
            _obj = _stack[idx];
            _stack = null;
        }
    }

    public class Closure
    {
        public Function func = null;
        public Table env_table = null;
        
        internal void AddUpvalue(UpValue upvalue_)
        {
            _upvalues.Add(upvalue_);
        }
        internal UpValue GetUpvalue(int idx)
        {
            return _upvalues[idx];
        }

        List<UpValue> _upvalues = new List<UpValue>();
    }
}
