using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SimpleScript
{
    /// <summary>
    /// 静态函数结构，（可序列化成编译后代码）
    /// 1. 指令数组
    /// 2. 常量(字符串，数字)
    /// 3. 局部变量
    /// 4. 子函数
    /// </summary>
    public partial class Function
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
        public int GetCodeCount()
        {
            return _codes.Count;
        }
        public int GetInstructionLine(int idx)
        {
            return _code_lines[idx];
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
        public int GetLocalVarIndexByNameAndPc(string name, int pc)
        {
            foreach(var info in _local_var_infos)
            {
                if(info.name == name && info.begin_pc <= pc && pc < info.end_pc)
                {
                    return info.register_idx;
                }
            }
            return -1;// not find
        }

        internal List<LocalVarInfo> GetAllLocalVarInfo()
        {
            return _local_var_infos;
        }

        public int GetMaxRegisterCount()
        {
            return _max_register_count;
        }
        public void SetMaxRegisterCount(int count)
        {
            _max_register_count = Math.Max(_max_register_count, count);
        }

        int _max_register_count = 0;

        public string GetFileName()
        {
            return _file_name;
        }
        public void SetFileName(string moudle_name_)
        {
            _file_name = moudle_name_;
        }
        public string GetFuncName()
        {
            return _func_name;
        }
        public void SetFuncName(string func_name_)
        {
            _func_name = func_name_;
        }
        string _file_name = string.Empty;
        string _func_name = string.Empty;

        List<Function> _child_functions = new List<Function>();
        List<object> _const_objs = new List<object>();
        bool _has_vararg = false;
        int _fixed_arg_count = 0;
        List<Instruction> _codes = new List<Instruction>();
        List<int> _code_lines = new List<int>();

        // For debug
        internal struct LocalVarInfo
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

        #region upvalue

        public int AddUpValue(string name, int register, bool parent_local)
        {
            _upvalues.Add(new UpValueInfo(name, register, parent_local));
            return _upvalues.Count - 1;
        }
        public int GetUpValueCount()
        {
            return _upvalues.Count;
        }
        public UpValueInfo GetUpValueInfo(int idx)
        {
            return _upvalues[idx];
        }

        public int SearchUpValue(string name)
        {
            for (int i = 0; i < _upvalues.Count; ++i)
            {
                if (_upvalues[i].name == name)
                    return i;
            }
            return -1;
        }

        internal List<UpValueInfo> GetAllUpValueInfos()
        {
            return _upvalues;
        }

        public class UpValueInfo
        {
            public string name;
            public int register;
            public bool is_parent_local;
            public UpValueInfo(string name_, int register_, bool is_parent_local_)
            {
                name = name_;
                register = register_;
                is_parent_local = is_parent_local_;
            }
        }

        List<UpValueInfo> _upvalues = new List<UpValueInfo>();
        #endregion
    }
}
