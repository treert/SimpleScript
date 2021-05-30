using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyScript
{
    /// <summary>
    /// 统一的参数格式
    /// </summary>
    public class MyArgs:IEnumerable
    {
        public object? m_that = null;// this
        public object? That
        {
            get {
                if(m_name_args.TryGetValue(Utils.MAGIC_THIS, out var ret))
                {
                    return ret;
                }
                return m_that;
            }
        }
        public Dictionary<string, object?> m_name_args = new();
        public MyArray m_args = new MyArray();
        public Frame? m_frame = null;// VM 调用外部接口时，通过这个可以传递运行是环境，增加功能

        public MyArgs(Frame frame)
        {
            this.m_frame = frame;
        }

        public MyArgs(params object[] args)
        {
            this.m_args.m_items.AddRange(args);
        }

        public MyArgs(Dictionary<string, object?> name_args, params object[] args)
        {
            this.m_name_args = name_args;
            this.m_args.m_items.AddRange(args);
        }

        public bool TryGetValue(int idx, string name, out object? ret)
        {
            if (m_name_args.TryGetValue(name, out ret))
            {
                return true;// 优先级高于数组参数
            }
            else if (idx >= 0 && idx < m_args.Count)
            {
                ret = m_args[idx];
                return true;
            }
            ret = null;
            return false;
        }

        public IEnumerator GetEnumerator()
        {
            return m_args.GetEnumerator();
        }

        public object? this[int idx]
        {
            get {
                if (idx >= 0 && idx < m_args.Count)
                {
                    return m_args[idx];
                }
                return null;
            }
        }

        public object? this[string name]
        {
            get {
                object? ret;
                m_name_args.TryGetValue(name, out ret);
                return ret;
            }
        }

        public object? this[int idx, string name]
        {
            get {
                TryGetValue(idx, name, out var ret);
                return ret;
            }
        }
    }
}
