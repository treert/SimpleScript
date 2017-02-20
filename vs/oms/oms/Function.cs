using System;
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
            return 0;
        }
        public void SetInstructionByIndex(int index, Instruction i)
        {
            
        }

        public void FillInstructionBx(int index, int bx)
        {

        }
        public void SetHasVarArg()
        {

        }

        // 添加指令，返回添加的指令的index
        public int AddInstruction(Instruction i, int line)
        {
            return 0;
        }
        public void SetFixedArgCount(int fixed_arg_count_)
        {

        }
        public int GetFixedArgCount()
        {
            return 0;
        }
        public bool HasVararg()
        {
            return false;
        }

        public void SetParent(Function parent)
        {

        }
        public void AddConstNumber(double num)
        {

        }
        public int AddConstString(string str)
        {
            return 0;
        }
        public int AddChildFunction(Function child)
        {
            return 0;
        }
        // debug info 
        public void AddLocalVar(string name,int register, int begin_pc, int end_pc)
        {

        }
        public void AddUpValue(string name,int register, bool parent_local)
        {

        }
        public int SearchUpValue(string name)
        {
            return -1;
        }
    }
}
