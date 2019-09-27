using System;
using System.Collections.Generic;
using System.Text;


namespace SScript
{
    
    /// <summary>
    /// 运行时函数
    /// </summary>
    public class Function
    {
        public VM vm;
        public FunctionBody code;
        public Table module_table = null;
        // 环境闭包值，比较特殊的是：当Value == null，指这个变量是全局变量。
        public Dictionary<string, LocalValue> upvalues = new Dictionary<string, LocalValue>();

        public void Call(params object[] objs)
        {

        }

        public void Call(Dictionary<string, object> name_args, params object[] args)
        {

        }

        public void Call(Args args)
        {
            Frame frame = new Frame(this);

        }
    }

    public class Args
    {
        public Dictionary<string, object> name_args = new Dictionary<string, object>();
        public List<object> args = new List<object>();

        public Args()
        {
            name_args = new Dictionary<string, object>();
            args = new List<object>();
        }
    }

    public class Table
    {
        public object Set(object key, object val)
        {
            return null;
        }

        public object Get(object key)
        {
            return null;
        }
    }

    public class LocalValue
    {
        public object obj;

        public static implicit operator bool(LocalValue exsit)
        {
            return exsit != null;
        }
    }
}
