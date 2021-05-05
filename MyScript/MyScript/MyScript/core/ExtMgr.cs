using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyScript
{
    public class ExtMgr
    {
        Dictionary<Type, ExtItem> m_raw_items = new Dictionary<Type, ExtItem>();
        Dictionary<Type, ExtItem> m_all_items = new Dictionary<Type, ExtItem>();

        public ExtItem? GetItem(Type type)
        {
            if(m_all_items.TryGetValue(type, out var ret))
            {
                return ret;
            }
            return null;
        }

        public void Register(Type type, ExtItem item)
        {
            m_raw_items.Add(type, item);
        }
        /// <summary>
        /// 注册的地方自己调用吧。就不自动调用了。
        /// </summary>
        public void RebuildAll()
        {
            m_all_items.Clear();
            var xx = from it in m_raw_items select (it.Key, it.Value.Clone());
            m_all_items.AddRange(xx);

            foreach(var t1 in m_all_items)
                foreach(var t2 in m_raw_items)
                {
                    if(t1.Key != t2.Key && t1.Key.IsAssignableTo(t2.Key))
                    {
                        t1.Value.Merge(t2.Value);
                    }
                }
        }
    }

    public class ExtItem
    {
        Dictionary<string, ICall> m_calls = new Dictionary<string, ICall>();
        public void Merge(ExtItem item)
        {
            m_calls.AddRange(item.m_calls);
        }
        public ExtItem Clone()
        {
            var item = new ExtItem();
            item.m_calls.AddRange(m_calls);
            return item;
        }

        public void Register(string name, ICall call)
        {
            m_calls[name] = call;
        }
        public ICall? GetCall(string name)
        {
            if (m_calls.TryGetValue(name, out var ret))
            {
                return ret;
            }
            return null;
        }
    }
}
