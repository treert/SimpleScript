using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{
    /// <summary>
    /// 静态函数结构，包含
    /// 1. 指令数组
    /// 2. 常量(字符串，数字)
    /// 3. 局部变量
    /// 4. 闭包变量
    /// 5. 子函数
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
            // todo ...
        }
        public int AddUpValue(string name,int register, bool parent_local)
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
            for(int i = 0; i < _upvalues.Count; ++i)
            {
                if (_upvalues[i].name == name)
                    return i;
            }
            return -1;
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

        List<Function> _child_functions = new List<Function>();
        List<object> _const_objs = new List<object>();
        Function _parent = null;
        bool _has_vararg = false;
        int _fixed_arg_count = 0;
        List<Instruction> _codes = new List<Instruction>();
    }
}
