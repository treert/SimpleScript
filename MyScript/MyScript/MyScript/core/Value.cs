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
        object Get(object key);
        bool Set(object key, object val);
    }

    public interface ICall
    {
        List<object> Call(Args args);

        // 原来打算直接使用 delegate 的，但是并不方便
        // 1. Method不能直接赋值给object，需要new Delegate(Method)。
        // 2. 函数调用的地方需要额外增加判断
        public delegate List<object> CallDelegate(Args args);
        public class CallWrap : ICall
        {
            CallDelegate func;
            public CallWrap(CallDelegate func)
            {
                this.func = func;
            }
            public List<object> Call(Args args)
            {
                return func(args);
            }
        }
        public static ICall Create(CallDelegate func)
        {
            return new CallWrap(func);
        }
    }

    

    public interface IForEach
    {
        // expect_cnt 是为了能实现 for v in table {} for k,v in table {}
        IEnumerable<object[]> GetForEachItor(int expect_cnt);
    }

    /// <summary>
    /// 运行时函数
    /// </summary>
    public class Function: ICall
    {
        public VM vm;
        public FunctionBody code;
        public Table module_table = null;
        // 环境闭包值，比较特殊的是：当Value == null，指这个变量是全局变量。
        public Dictionary<string, LocalValue> upvalues;

        public List<object> Call(params object[] objs)
        {
            Args args = new Args(objs);
            return Call(args);
        }

        public List<object> Call(Dictionary<string, object> name_args, params object[] objs)
        {
            Args args = new Args(name_args, objs);
            return Call(args);
        }

        public List<object> Call()
        {
            Args args = new Args();
            return Call(args);
        }

        public List<object> Call(Args args)
        {
            Frame frame = new Frame(this);
            // 先填充个this
            frame.AddLocalVal(Utils.MAGIC_THIS, args.that);

            int name_cnt = 0;
            if (code.param_list)
            {
                name_cnt = code.param_list.name_list.Count;
                for (int i = 0; i < name_cnt; i++)
                {
                    frame.AddLocalVal(code.param_list.name_list[i].m_string, args[i]);
                }

                if (code.param_list.kw_name)
                {
                    frame.AddLocalVal(code.param_list.kw_name.m_string, args.name_args);
                }

                foreach(var it in args.name_args)
                {
                    LocalValue v;
                    if(frame.cur_block.values.TryGetValue(it.Key, out v))
                    {
                        v.obj = it.Value;
                    }
                }
            }
            for (int i = name_cnt; i < args.args.Count; i++)
            {
                frame.extra_args.Add(args[i]);
            }
            try
            {
                code.block.Exec(frame);
            }
            catch(ReturnException ep)
            {
                return ep.results;
            }
            catch(ContineException ep)
            {
                throw new RunException(code.source_name, ep.line, "unexpect contine");
            }
            catch(BreakException ep)
            {
                throw new RunException(code.source_name, ep.line, "unexpect break");
            }
            // 其他的异常就透传出去好了。
            return Utils.EmptyResults;
        }
    }

    public class Args
    {
        public object that = null;// this
        public Dictionary<string, object> name_args;
        public List<object> args;
        public Frame frame = null;// VM 调用外部接口时，通过这个可以传递运行是环境，增加功能

        public Args()
        {
            name_args = new Dictionary<string, object>();
            args = new List<object>();
        }

        public Args(Frame frame): this()
        {
            this.frame = frame;
        }

        public Args(params object[] args)
        {
            name_args = new Dictionary<string, object>();
            this.args = new List<object>(args);
        }

        public Args(Dictionary<string, object> name_args, params object[] args)
        {
            this.name_args = name_args;
            this.args = new List<object>(args);
        }

        public object this[int idx]
        {
            get
            {
                if(idx >= 0 && idx < args.Count)
                {
                    return args[idx];
                }
                return null;
            }
        }

        public object this[string name]
        {
            get
            {
                object ret;
                name_args.TryGetValue(name, out ret);
                return ret;
            }
        }
    }

    // 内置Table，在Dictionary的基础上加上简单的元表功能。不用lua元表那么复杂的结构了
    // 如果要实现复杂的结构，那就去继承实现对应的接口好了。
    // 注意1：ForEach的支持是不搜索元表的。
    public class Table: IGetSet, IForEach
    {
        Dictionary<object, object> _items = new Dictionary<object, object>();
        Table prototype = null;

        public object this[string idx]
        {
            get
            {
                return Get(idx);
            }
            set
            {
                Set(idx, value);
            }
        }

        public int Len => _items.Count;

        public bool Set(object key, object value)
        {
            if(key == null)
            {
                return false;// @om 就不报错了
            }
            key = PreConvertKey(key);
            if(value == null)
            {
                return this._items.Remove(key);
            }
            this._items[key] = value;

            return true;
        }

        public object Get(object key)
        {
            if (key == null)
            {
                return null;// @om 就不报错了
            }
            key = PreConvertKey(key);

            var it = this;
            object val;
            do
            {
                if (it._items.TryGetValue(key, out val))
                {
                    return val;
                }
                it = it.prototype;
            } while (it != null);

            return null;
        }

        public static object PreConvertKey(object key)
        {
            double f = Utils.ConvertToPriciseDouble(key);
            if (double.IsNaN(f) == false)
            {
                key = f;
            }
            return key;
        }

        public IEnumerable<object[]> GetForEachItor(int expect_cnt)
        {
            if(expect_cnt > 1)
            {
                foreach(var it in _items)
                {
                    yield return new object[]{ it.Key, it.Value };
                }
            }
            else
            {
                foreach (var it in _items)
                {
                    yield return new object[] { it.Value };
                }
            }
            yield break;
        }
    }

    public class MyArray: IGetSet, IForEach
    {
        public List<object> m_items = new List<object>();

        public object Get(object key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<object[]> GetForEachItor(int expect_cnt)
        {
            if (expect_cnt > 1)
            {
                for (int i = 0; i < m_items.Count; i++)
                {
                    yield return  new object[] { i, m_items[i] };
                }
            }
            else
            {
                for (int i = 0; i < m_items.Count; i++)
                {
                    yield return new object[] { m_items[i] };
                }
            }
            yield break;
        }

        public bool Set(object key, object val)
        {
            throw new NotImplementedException();
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
