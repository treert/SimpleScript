using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{
    enum OpType
    {
        OpType_LoadNil = 1,             // A    A: register
        OpType_LoadBool,                // AB   A: register B: 1 true 0 false
        OpType_LoadInt,                 // ABx  A: register Bx: const unsigned int
        OpType_LoadConst,               // ABx  A: register Bx: const index
        OpType_Move,                    // AB   A: dst register B: src register
        OpType_GetUpvalue,              // AB   A: register B: upvalue index
        OpType_SetUpvalue,              // AB   A: register B: upvalue index
        OpType_GetGlobal,               // ABx  A: value register Bx: const index
        OpType_SetGlobal,               // ABx  A: value register Bx: const index
        OpType_Closure,                 // ABx  A: register Bx: proto index
        OpType_Call,                    // ABC  A: register B: arg count C: is any arg
        OpType_VarArg,                  // A    A: register
        OpType_Ret,                     // ABC  A: return value start register B: return value count C: return any count
        OpType_JmpFalse,                // AsBx A: register sBx: diff of instruction index
        OpType_JmpTrue,                 // AsBx A: register sBx: diff of instruction index
        OpType_JmpNil,                  // AsBx A: register sBx: diff of instruction index
        OpType_Jmp,                     // sBx  sBx: diff of instruction index
        OpType_Neg,                     // A    A: operand register and dst register
        OpType_Not,                     // A    A: operand register and dst register
        OpType_Len,                     // A    A: operand register and dst register
        OpType_Add,                     // ABC  A: dst register B: operand1 register C: operand2 register
        OpType_Sub,                     // ABC  A: dst register B: operand1 register C: operand2 register
        OpType_Mul,                     // ABC  A: dst register B: operand1 register C: operand2 register
        OpType_Div,                     // ABC  A: dst register B: operand1 register C: operand2 register
        OpType_Pow,                     // ABC  A: dst register B: operand1 register C: operand2 register
        OpType_Mod,                     // ABC  A: dst register B: operand1 register C: operand2 register
        OpType_Concat,                  // ABC  A: dst register B: operand1 register C: operand2 register
        OpType_Less,                    // ABC  A: dst register B: operand1 register C: operand2 register
        OpType_Greater,                 // ABC  A: dst register B: operand1 register C: operand2 register
        OpType_Equal,                   // ABC  A: dst register B: operand1 register C: operand2 register
        OpType_UnEqual,                 // ABC  A: dst register B: operand1 register C: operand2 register
        OpType_LessEqual,               // ABC  A: dst register B: operand1 register C: operand2 register
        OpType_GreaterEqual,            // ABC  A: dst register B: operand1 register C: operand2 register
        OpType_NewTable,                // A    A: register of table
        OpType_SetTable,                // ABC  A: register of table B: key register C: value register
        OpType_GetTable,                // ABC  A: register of table B: key register C: value register
        OpType_TableNext,               // A    A: register of table  return A: value A+1: key
        OpType_ForInit,                 // ABC  A: var register B: limit register    C: step register
        OpType_ForStep,                 // ABC  ABC same with OpType_ForInit, next instruction sBx: diff of instruction index
        OpType_CloseUpvalue,            // A    A: close upvalue to this register
        OpType_SetTop,                  // A    A: set new top to this register,current for exp list and table define last exp
    }
    struct Instruction
    {
        int _opcode;
        public Instruction(OpType op, int a)
        {
            _opcode = (((int)op) << 24) | (a & 0xffff);
        }

        public static Instruction AsBx(OpType op, int a, int sbx)
        {
            return new Instruction(op, a);
        }
        public static Instruction A(OpType op, int a)
        {
            return new Instruction(op, a);
        }
        public static Instruction AB(OpType op, int a, int b)
        {
            return new Instruction(op, a);
        }
        public static Instruction ABC(OpType op, int a, int b,int c)
        {
            return new Instruction(op, a);
        }
        public static Instruction SBx(OpType op, int b)
        {
            return new Instruction(op, b);
        }
    }
}
