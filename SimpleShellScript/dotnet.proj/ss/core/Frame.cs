using System;
using System.Collections.Generic;
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

        public LocalValue WriteLocal(string name, object obj)
        {
            var v = AddLocalName(name);
            v.obj = obj;
            return v;
        }

        public void Write(string name, object obj)
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
    }
}
