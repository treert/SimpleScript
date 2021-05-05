using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyScript
{
    /// <summary>
    /// 统一的参数格式
    /// </summary>
    public class MyArgs
    {
        public object? that = null;// this
        public Dictionary<string, object?> name_args = new();
        public MyArray args = new MyArray();
        public Frame? frame = null;// VM 调用外部接口时，通过这个可以传递运行是环境，增加功能

        public MyArgs(Frame frame)
        {
            this.frame = frame;
        }

        public MyArgs(params object[] args)
        {
            this.args.m_items.AddRange(args);
        }

        public MyArgs(Dictionary<string, object?> name_args, params object[] args)
        {
            this.name_args = name_args;
            this.args.m_items.AddRange(args);
        }

        public bool TryGetValue(int idx, string name, out object? ret)
        {
            if (name_args.TryGetValue(name, out ret))
            {
                return true;// 优先级高于数组参数
            }
            else if (idx >= 0 && idx < args.Count)
            {
                ret = args[idx];
                return true;
            }
            ret = null;
            return false;
        }

        public object? this[int idx]
        {
            get {
                if (idx >= 0 && idx < args.Count)
                {
                    return args[idx];
                }
                return null;
            }
        }

        public object? this[string name]
        {
            get {
                object? ret;
                name_args.TryGetValue(name, out ret);
                return ret;
            }
        }
    }
}
