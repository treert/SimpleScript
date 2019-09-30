using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SScript
{
    /// <summary>
    /// 单个执行帧：管理局部变量
    /// </summary>
    public class Frame
    {
        public class GenBlock
        {
            public Dictionary<string, LocalValue> values = new Dictionary<string, LocalValue>();
            public GenBlock parent = null;
        }

        public Function func;
        public List<object> extra_args = new List<object>();// for ...
        public GenBlock cur_block;

        public Frame(Function func)
        {
            this.func = func;
            this.cur_block = new GenBlock();
        }

        public LocalValue AddLocalName(string name)
        {
            LocalValue val = new LocalValue();
            this.cur_block.values[name] = val;
            return val;
        }

        public void AddGlobalName(string name)
        {
            this.cur_block.values[name] = null;
        }

        public LocalValue GetName(string name, out bool is_global)
        {
            GenBlock b = this.cur_block;
            LocalValue ret = null;
            while(b != null)
            {
                if(b.values.TryGetValue(name, out ret))
                {
                    is_global = true;
                    return ret;
                }
                b = b.parent;
            }
            is_global = this.func.upvalues.TryGetValue(name, out ret);
            return ret;
        }

        public LocalValue AddLocalVal(string name, object obj)
        {
            var v = AddLocalName(name);
            v.obj = obj;
            return v;
        }

        public object Write(string name, object obj)
        {
            bool global;
            var v = GetName(name, out global);
            if (global)
            {
                func.vm.global_table.Set(name, obj);
            }
            else if (v)
            {
                v.obj = obj;
            }
            else
            {
                func.module_table.Set(name, obj);
            }
            return obj;
        }

        public object Read(string name)
        {
            bool global;
            var v = GetName(name, out global);
            if (global)
            {
                return func.vm.global_table.Get(name);
            }
            else if (v)
            {
                return v.obj;
            }
            else
            {
                return func.module_table.Get(name);
            }
        }

        public GenBlock EnterBlock()
        {
            var b = new GenBlock();
            b.parent = this.cur_block;
            this.cur_block = b;
            return b;
        }

        public void LeaveBlock()
        {
            this.cur_block = this.cur_block.parent;
        }
        
        public Dictionary<string, LocalValue> GetAllUpvalues()
        {
            var dic = new Dictionary<string, LocalValue>();
            var b = cur_block;
            while(b != null)
            {
                foreach(var it in b.values)
                {
                    dic.TryAdd(it.Key, it.Value);
                }
                b = b.parent;
            }
            foreach(var it in func.upvalues)
            {
                dic.TryAdd(it.Key, it.Value);
            }
            return dic;
        }

        public RunException NewRunException(int line, string msg)
        {
            return new RunException(func.code.source_name, line, msg);
        }
    }
}
