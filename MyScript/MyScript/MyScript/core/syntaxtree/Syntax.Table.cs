using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class TableDefine : ExpSyntaxTree
    {
        public TableDefine(int line_)
        {
            _line = line_;
        }
        public List<TableField> fields = new List<TableField>();

        protected override List<object> _GetResults(Frame frame)
        {
            Table ret = new Table();
            for (var i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                if (f.index == null && i == fields.Count - 1)
                {
                    var vs = f.value.GetResults(frame);
                    foreach (var v in vs)
                    {
                        ret.Set(++i, v);
                    }
                    break;
                }

                object key = f.index ? f.index.GetOneResult(frame) : i + 1;
                if (key == null)
                {
                    throw frame.NewRunException(f.line, "Table key can not be nil");
                }
                ret.Set(key, f.value.GetOneResult(frame));
            }
            return new List<object>() { ret };
        }
    }

    public class TableField : SyntaxTree
    {
        public TableField(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree index = null;
        public ExpSyntaxTree value;
    }

    // todo 这儿可以做一个优化: a.b.c.d = 1，连着创建2个Table。实际使用时，可能很方便。
    public class TableAccess : ExpSyntaxTree
    {
        public TableAccess(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree table;
        public ExpSyntaxTree index;

        protected override List<object> _GetResults(Frame frame)
        {
            var t = table.GetOneResult(frame);
            var idx = index.GetOneResult(frame);
            if (idx == null)
            {
                throw frame.NewRunException(index.line, "table index can not be null");
            }
            var ret = ExtUtils.Get(t, idx);
            return new List<object>() { ret };
        }
    }

}
