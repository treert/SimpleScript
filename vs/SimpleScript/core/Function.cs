using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    /// <summary>
    /// 静态函数结构，包含
    /// 1. 指令数组
    /// 2. 常量(字符串，数字)
    /// 3. 局部变量
    /// 4. 子函数
    /// </summary>
    class Function
    {
        public Function()
        {
        }
        public int OpCodeSize()
        {
            return _codes.Count;
        }

        public void FillInstructionBx(int index, int bx)
        {
            var i = _codes[index];
            i.SetBx(bx);
            _codes[index] = i;
        }
        // 添加指令，返回添加的指令的index
        public int AddInstruction(Instruction i, int line)
        {
            _codes.Add(i);
            _code_lines.Add(line);
            return _codes.Count - 1;
        }
        public Instruction GetInstruction(int idx)
        {
            return _codes[idx];
        }
        public void SetFixedArgCount(int fixed_arg_count_)
        {
            _fixed_arg_count = fixed_arg_count_;
        }
        public int GetFixedArgCount()
        {
            return _fixed_arg_count;
        }
        public void SetHasVarArg()
        {
            _has_vararg = true;
        }
        public bool HasVararg()
        {
            return _has_vararg;
        }

        public void SetParent(Function parent)
        {
            _parent = parent;
            _env_name = parent._env_name;
            _env_table = parent._env_table;
        }
        public int AddConstNumber(double num)
        {
            _const_objs.Add(num);
            return _const_objs.Count - 1;
        }
        public int AddConstString(string str)
        {
            _const_objs.Add(str);
            return _const_objs.Count - 1;
        }
        public object GetConstValue(int i)
        {
            return _const_objs[i];
        }
        public int AddChildFunction(Function child)
        {
            _child_functions.Add(child);
            return _child_functions.Count - 1;
        }
        public Function GetChildFunction(int idx)
        {
            return _child_functions[idx];
        }
        // special func for moudle("name.space")
        public void CopyEnvToChild(int idx)
        {
            var child = _child_functions[idx];
            child._env_name = _env_name;
            child._env_table = _env_table;
        }
        public void AddLocalVar(string name,int register, int begin_pc, int end_pc)
        {
            Debug.Assert(begin_pc < end_pc);
            _local_var_infos.Add(new LocalVarInfo(name, register, begin_pc, end_pc));
        }
        public string GetLocalVarNameByPc(int register, int pc)
        {
            // a little trick
            foreach(var info in _local_var_infos)
            {
                if(info.register_idx == register &&
                    info.begin_pc <= pc && pc < info.end_pc)
                {
                    return info.name;
                }
            }
            return null;
        }
        public int GetMaxRegisterCount()
        {
            return _MaxRegisterCount;
        }
        public void SetMaxRegisterCount(int count)
        {
            // !!! 实现好像有错误，这个貌似可以设置设成256最大值。
            _MaxRegisterCount = Math.Max(_MaxRegisterCount, count);
        }
        public void SetEnv(string env_name, Table env_table)
        {
            _env_name = env_name;
            _env_table = env_table;
        }
        public Table GetEnvTable()
        {
            Debug.Assert(_env_table != null);
            return _env_table;
        }

        int _MaxRegisterCount = 0;// 需要的最大寄存器数量

        // For runtime
        string _env_name = null;// 函数上下文环境名字
        Table _env_table = null;// 函数上下文环境，就是函数全局表

        List<Function> _child_functions = new List<Function>();
        List<object> _const_objs = new List<object>();
        Function _parent = null;
        bool _has_vararg = false;
        int _fixed_arg_count = 0;
        List<Instruction> _codes = new List<Instruction>();
        List<int> _code_lines = new List<int>();

        // For debug
        struct LocalVarInfo
        {
            public string name;
            public int register_idx;
            public int begin_pc;
            public int end_pc;
            public LocalVarInfo(string name_, int register_idx_, int begin_pc_, int end_pc_)
            {
                name = name_;
                register_idx = register_idx_;
                begin_pc = begin_pc_;
                end_pc = end_pc_;
            }
        }
        List<LocalVarInfo> _local_var_infos = new List<LocalVarInfo>();
    }
}
