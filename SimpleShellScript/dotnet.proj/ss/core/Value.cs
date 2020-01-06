using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;


/// <summary>
/// 人力有穷时，简单化吧，虽然想加好多语法糖。
/// 1. ss 语言层面只支持非常少的类型：
/// </summary>
namespace SScript
{
    public interface IForIter
    {
        bool Next(out object key, out object val);
    }

    public interface IForEach
    {
        IForIter GetIter();
    }

    public interface IForKeys
    {
        List<object> Keys();
        object Get(object key);
    }

    public interface ITable
    {
        object Get(object key);
        object Set(object key, object value);
    }

    public interface ICall
    {
        List<object> Call(Args args);
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
            frame.AddLocalVal(Config.MAGIC_THIS, args.that);

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
            return Config.EmptyResults;
        }
    }

    public class Args
    {
        public object that = null;// this
        public Dictionary<string, object> name_args;
        public List<object> args;

        public Args()
        {
            name_args = new Dictionary<string, object>();
            args = new List<object>();
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
    }

    // 内置Table，
    // 1. 同时作为 array and set。数组的支持是残次的，不要在其中挖洞
    // 2. 支持一个接近js原型的结构，不用lua元表那么复杂的结构了
    public class Table
    {
        Dictionary<object, object> _items = new Dictionary<object, object>();
        Table prototype = null;

        public List<object> GetKeys()
        {
            List<object> ret = new List<object>();
            var it = this;
            do
            {
                ret.AddRange(it._items.Keys);
                it = it.prototype;
            } while (it != null);
            ret = ret.Distinct().ToList();
            ret.Sort();
            return ret;
        }

        public int Len => _items.Count;

        public object Set(object key, object value)
        {
            if(key == null)
            {
                return null;// @om 就不报错了
            }
            
            return null;
        }

        public object Get(object key)
        {
            if (key == null)
            {
                return null;// @om 就不报错了
            }
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
