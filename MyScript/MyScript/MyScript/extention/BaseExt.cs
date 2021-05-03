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
        public object Call(Args args)
        {
            for(var i = 0; i < args.args.Count; i++)
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
        public object Get(object key)
        {
            if(m_items.TryGetValue(key, out ICall ret))
            {
                return ret;
            }
            throw new Exception($"unexport key {key}");
        }

        public List<object> Test(Args args)
        {
            Console.WriteLine($"test {args.args.Count}");
            return Utils.EmptyResults;
        }

        public void Set(object key, object? val)
        {
            throw new NotImplementedException();
        }
    }
}
