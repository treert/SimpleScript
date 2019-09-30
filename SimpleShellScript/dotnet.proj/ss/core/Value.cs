using System;
using System.Collections.Generic;
using System.Text;


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

    public interface IGetSet
    {
        object Get(object name);
        object Set(object name, object value);
    }

    /// <summary>
    /// 运行时函数
    /// </summary>
    public class Function
    {
        public VM vm;
        public FunctionBody code;
        public Table module_table = null;
        // 环境闭包值，比较特殊的是：当Value == null，指这个变量是全局变量。
        public Dictionary<string, LocalValue> upvalues;

        public void Call(params object[] objs)
        {

        }

        public void Call(Dictionary<string, object> name_args, params object[] args)
        {

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

    public class Table : IGetSet
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


    public class ValueUtils
    {
        public static bool ToBool(object obj)
        {
            throw new NotImplementedException();
        }

        public static double ToNumber(object obj)
        {
            throw new NotImplementedException();
        }
        
    }
}
