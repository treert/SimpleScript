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
    }

    public class Table
    {

    }

    public class LocalValue
    {
        public object obj;
    }
}
