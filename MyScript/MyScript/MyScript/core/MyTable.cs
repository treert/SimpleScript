using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyScript
{
    // 类似java的LinkedHashMap，内置Table。
    // 简单的实现，不支持lua元表或者js原型。如果需要支持这种，自定义结构，实现接口就好，比如实现一个class。
    public class MyTable : IGetSet, IForEach
    {
#nullable disable
        internal class ItemNode
        {
            public object key;
            public object value;
            public ItemNode next;
            public ItemNode prev;
        }
#nullable restore

        internal ItemNode _itor_node = new();

        Dictionary<object, ItemNode> _key_map = new();

        public MyTable()
        {
            _itor_node.prev = _itor_node;
            _itor_node.next = _itor_node;
        }

        public void Add(MyTable other)
        {
            var it = other._itor_node.next;
            while (it != other._itor_node)
            {
                _AddNode(it.key, it.value);
                it = it.next;
            }
        }

        internal IEnumerable<ItemNode> GetItemNodeItor()
        {
            ItemNode it = _itor_node.next;
            while (it != _itor_node)
            {
                yield return it;
            }
        }

        public object this[string idx]
        {
            get {
                return Get(idx);
            }
            set {
                Set(idx, value);
            }
        }

        public int Len => _key_map.Count;

        void _RemoveNode(ItemNode node)
        {
            _key_map.Remove(node.key!);
            node.prev!.next = node.next;
            node.next!.prev = node.prev;
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
            node.prev!.next = node;
            return node;
        }

        private void _AddNode(object key, object value)
        {
            if (_key_map.TryGetValue(key, out var node))
            {
                node.value = value;
            }
            else
            {
                _key_map.Add(key, _AddNodeAtLast(key, value));
            }
        }

        public void Set(object key, object? value)
        {
            // todo@om 应该不会出现这种情况
            if (key == null)
            {
                return;// @om 就不报错了
            }
            key = PreConvertKey(key);
            if(value == null)
            {
                if(_key_map.TryGetValue(key, out var node))
                {
                    _RemoveNode(node);
                }
            }
            else
            {
                _AddNode(key, value);
            }
        }

        public object? Get(object key)
        {
            if (key == null)
            {
                return null;// @om 就不报错了
            }
            key = PreConvertKey(key);

            if (_key_map.TryGetValue(key, out var node))
            {
                return node.value;
            }
            return null;
        }

        // 数字类型的全部转换成MyNumber
        public static object PreConvertKey(object key)
        {
            var n = MyNumber.TryConvertFrom(key);
            return n ?? key;
        }

        public IEnumerable<object?> GetForEachItor(int expect_cnt)
        {
            if (expect_cnt > 1)
            {
                var it = _itor_node.next;
                while (it != _itor_node)
                {
                    yield return new MyArray { it.key, it.value };
                    it = it.next;
                }
            }
            else
            {
                var it = _itor_node.next;
                while (it != _itor_node)
                {
                    yield return it.value;
                    it = it.next;
                }
            }
            yield break;
        }
    }
}
