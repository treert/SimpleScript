using System;
using System.Collections.Generic;
using System.Text;
/// <summary>
/// 一些基础的扩展
/// </summary>
namespace MyScript
{
    public class MyConsole : ICall, IGetSet
    {
        public object? Call(MyArgs args)
        {
            for(var i = 0; i < args.m_args.Count; i++)
            {
                Console.Write(args[i]);
            }
            Console.WriteLine();
            return null;
        }
        Dictionary<object, ICall> m_items = new Dictionary<object, ICall>();
        public MyConsole()
        {
            m_items.Add("test", ICall.Create(Test));
        }
        public object? Get(object key)
        {
            if(m_items.TryGetValue(key, out ICall? ret))
            {
                return ret;
            }
            throw new Exception($"unexport key {key}");
        }

        public object? Test(MyArgs args)
        {
            Console.WriteLine($"test {args.m_args.Count}");
            return null;
        }

        public void Set(object key, object? val)
        {
            throw new NotImplementedException();
        }
    }
}
