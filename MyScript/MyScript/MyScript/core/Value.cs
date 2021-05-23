using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections;

/// <summary>
/// 人力有穷时，简单化吧，虽然想加好多语法糖。
/// 1. ms 语言层面只支持非常少的类型：
/// </summary>
namespace MyScript
{
    public interface IGetSet
    {
        object? Get(object key);
        void Set(object key, object? val);
    }

    public interface ICall
    {
        object? Call(MyArgs args);

        public static ICall Create(Func<MyArgs, object?> func) => new CallWrap(func);

        private class CallWrap : ICall
        {
            readonly Func<MyArgs, object?> func;
            public CallWrap(Func<MyArgs, object?> func)
            {
                this.func = func;
            }
            public object? Call(MyArgs args)
            {
                return func(args);
            }
        }
    }

    public interface IForEach
    {
        /// <summary>
        /// 支持 for in 语法，效率有点低哟，就这么着吧
        /// </summary>
        /// <param name="expect_cnt">expect_cnt 是为了能实现 for v in table {}, for k,v in table {}</param>
        /// <returns></returns>
        IEnumerable<object?> GetForEachItor(int expect_cnt = -1);
    }

    public interface IFormatString
    {
        string FormatString(string format);
    }

    public class LocalValue
    {
        public object? obj;
    }
}
