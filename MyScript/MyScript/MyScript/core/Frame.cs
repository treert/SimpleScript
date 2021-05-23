using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyScript
{
    /// <summary>
    /// 单个执行帧：管理局部变量
    /// </summary>
    public class Frame
    {
        public class GenBlock
        {
            /// <summary>
            /// Block 局部变量表，如果是null，表示是全局的
            /// </summary>
            public Dictionary<string, LocalValue?> values = new Dictionary<string, LocalValue?>();
            public List<IDisposable> scope_objs = new List<IDisposable>();
            public GenBlock? parent = null;
        }

        public MyFunction func;
        GenBlock cur_block;

        public Frame(MyFunction func)
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

        public void AddGlobalVal(string name, object? obj)
        {
            AddGlobalName(name);
            func.vm.global_table[name] = obj;
        }

        public LocalValue? GetName(string name, out bool is_global)
        {
            GenBlock? b = this.cur_block;
            LocalValue? ret = null;
            while(b != null)
            {
                if(b.values.TryGetValue(name, out ret))
                {
                    is_global = true;
                    return ret;
                }
                b = b.parent;
            }
            is_global = !this.func.upvalues.TryGetValue(name, out ret);
            return ret;
        }

        public LocalValue AddLocalVal(string name, object? obj)
        {
            var v = AddLocalName(name);
            v.obj = obj;
            return v;
        }

        public object Write(string name, object? obj)
        {
            bool global;
            var v = GetName(name, out global);
            if (global)
            {
                func.vm.global_table[name] = obj;
            }
            else if (v is not null)
            {
                v.obj = obj;
            }
            else
            {
                func.module_table[name] = obj;
            }
            return obj;
        }

        public object? Read(string name)
        {
            var v = GetName(name, out bool global);
            if (global)
            {
                return func.vm.global_table[name];
            }
            else if (v is not null)
            {
                return v.obj;
            }
            else
            {
                var obj = func.module_table[name];
                if(obj == null)
                {
                    obj = func.vm.global_table[name];
                }
                return obj;
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
            // todo@om 这儿如果抛异常了，就难受了。
            foreach(var obj in cur_block.scope_objs)
            {
                obj.Dispose();
            }
            this.cur_block = this.cur_block.parent!;
        }

        public GenBlock CurrentBlock
        {
            get => cur_block;
            set {
                // 不应该出现value不在栈里的情况
                while(cur_block != value)
                {
                    LeaveBlock();
                }
            }
        }

        public void AddScopeObj(object? obj)
        {
            if (obj is IDisposable a)
            {
                cur_block.scope_objs.Add(a);
            }
            else if (obj is MyArray b)
            {
                AddScopeObjs(b);
            }
        }

        public void AddScopeObjs(MyArray arr)
        {
            foreach(var obj in arr)
            {
                AddScopeObj(obj);
            }
        }

        
        public Dictionary<string, LocalValue?> GetAllUpvalues()
        {
            var dic = new Dictionary<string, LocalValue?>();
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
            return new RunException(func.code.Source, line, msg);
        }
    }
}
