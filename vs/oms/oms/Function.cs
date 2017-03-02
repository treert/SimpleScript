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
            _codes[index].SetBx(bx);
        }
        // 添加指令，返回添加的指令的index
        public int AddInstruction(Instruction i, int line)
        {
            _codes.Add(i);
            return _codes.Count - 1;
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
            _const_numbers.Add(num);
            return _const_numbers.Count - 1;
        }
        public int AddConstString(string str)
        {
            _const_strings.Add(str);
            return _const_strings.Count - 1;
        }
        public int AddChildFunction(Function child)
        {
            _child_functions.Add(child);
            return _child_functions.Count - 1;
        }
        public void AddLocalVar(string name,int register, int begin_pc, int end_pc)
        {
            // todo ...
        }
        public void AddUpValue(string name,int register, bool parent_local)
        {
            _upvalues.Add(name, new UpValueInfo(name, register, parent_local));
        }
        public int SearchUpValue(string name)
        {
            UpValueInfo upvalue = null;
            if (_upvalues.TryGetValue(name,out upvalue))
            {
                return upvalue.register;
            }
            return -1;
        }


        class UpValueInfo
        {
            public string name;
            public int register;
            public bool is_local;
            public UpValueInfo(string name_, int register_, bool is_local_)
            {
                name = name_;
                register = register_;
                is_local = is_local_;
            }
        }

        Dictionary<string, UpValueInfo> _upvalues = new Dictionary<string,UpValueInfo>();

        List<Function> _child_functions = new List<Function>();
        List<string> _const_strings = new List<string>();
        List<double> _const_numbers = new List<double>();
        Function _parent = null;
        bool _has_vararg = false;
        int _fixed_arg_count = 0;
        List<Instruction> _codes = new List<Instruction>();
    }
}
