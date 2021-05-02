using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class MyArray : IGetSet, IForEach, IEnumerable
    {
        List<object> m_items = new List<object>();
        public int Count { get => m_items.Count; }

        public void AddItem(object obj, bool split)
        {
            if (split && obj is MyArray arr)
            {
                m_items.AddRange(arr.m_items);
            }
            else
            {
                m_items.Add(obj);
            }
        }

        public void Add(object obj)
        {
            m_items.Add(obj);
        }

        public object this[int i]
        {
            get
            {
                if (i >= 0)
                {
                    return i < m_items.Count ? m_items[i] : null;
                }
                else
                {
                    return m_items.Count + i >= 0 ? m_items[m_items.Count + i] : null;
                }
            }
        }

        public IEnumerable<object[]> GetForEachItor(int expect_cnt)
        {
            if (expect_cnt > 1)
            {
                for (int i = 0; i < m_items.Count; i++)
                {
                    yield return new object[] { i, m_items[i] };
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

        public object Get(object key)
        {
            throw new NotImplementedException();
        }

        public bool Set(object key, object val)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)m_items).GetEnumerator();
        }
    }
}
