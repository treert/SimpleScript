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
                    frame.AddLocalVal(code.param_list.kw_name.m_string, args);// 直接获取所有参数好了
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

    public class Args:IGetSet
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

        public object Get(object key)
        {
            throw new NotImplementedException();
        }

        public bool Set(object key, object val)
        {
            throw new NotImplementedException();
        }
    }

    // 类似java的LinkedHashMap，内置Table。
    // 简单的实现，不支持lua元表或者js原型。如果需要支持这种，自定义结构，实现接口就好，比如实现一个class。
    public class Table: IGetSet, IForEach
    {
        class ItemNode
        {
            public object key;
            public object value;
            public ItemNode next = null;
            public ItemNode prev = null;
        }

        ItemNode _itor_node = new ItemNode();

        Dictionary<object, ItemNode> _key_map = new Dictionary<object, ItemNode>();

        public Table()
        {
            _itor_node.prev = _itor_node;
            _itor_node.next = _itor_node;
        }

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

        public int Len => _key_map.Count;

        void _RemoveNode(ItemNode node)
        {
            _key_map.Remove(node.key);
            node.prev.next = node.next;
            node.next.prev = node.prev;
        }

        ItemNode _AddNodeAtLast(object key, object value)
        {
            ItemNode node = new ItemNode {
                key = key,
                value = value,
                prev = _itor_node.prev,
                next = _itor_node,
            };
            _itor_node.prev = node;
            node.prev.next = node;
            return node;
        }

        public bool Set(object key, object value)
        {
            if(key == null)
            {
                return false;// @om 就不报错了
            }
            key = PreConvertKey(key);
            if (_key_map.TryGetValue(key, out var node))
            {
                if(value == null)
                {
                    _RemoveNode(node);
                }
                else
                {
                    node.value = value;
                }
            }
            else
            {
                if(value != null)
                {
                    _key_map.Add(key, _AddNodeAtLast(key, value));
                }
                else
                {
                    return false;// 无事发生
                }
            }
            return true;
        }

        public object Get(object key)
        {
            if (key == null)
            {
                return null;// @om 就不报错了
            }
            key = PreConvertKey(key);

            if(_key_map.TryGetValue(key, out var node)){
                return node.value;
            }
            return null;
        }

        // 对数字类型的key做规约处理。
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
                var it = _itor_node.next;
                while(it != _itor_node)
                {
                    yield return new object[] { it.key, it.value };
                    it = it.next;
                }
            }
            else
            {
                var it = _itor_node.next;
                while (it != _itor_node)
                {
                    yield return new object[] {it.value };
                    it = it.next;
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
