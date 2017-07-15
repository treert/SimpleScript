using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SimpleScript
{
    /*
    code: int32_t
    A   : uint8_t
    B   : uint8_t
    C   : uint8_t
    Bx  : int16_t (B+C)
    */
    public enum OpType
    {
        OpType_InValid = 0,
        OpType_LoadNil,                 // A    R(A) := nil
        OpType_LoadBool,                // AB   R(A) := (B == 1)
        OpType_LoadInt,                 // ABx  R(A) := Bx
        OpType_LoadConst,               // ABx  R(A) := Const(Bx)
        OpType_Move,                    // AB   R(A) := R(B)
        OpType_GetUpvalue,              // ABx  R(A) := Upvalue(Bx)
        OpType_SetUpvalue,              // ABx  Upvalue(Bx) := R(A)
        OpType_GetGlobal,               // ABx  R(A) := Global(Bx)
        OpType_SetGlobal,               // ABx  Global(Bx) := R(A)
        OpType_Closure,                 // ABx  R(A) := Closure(ChildFunc(Bx))
        OpType_Call,                    // ABC  R(A)..R(top-1) := Call(R(A),B:fix arg count,C==1:any arg to top)
        OpType_VarArg,                  // A    R(A)..R(top-1) := ...
        OpType_Ret,                     // ABC  return C!=1 ? R(A)..R(B) : R(A)..R(top-1)
        OpType_JmpFalse,                // ABx  if not R(A) then pc += Bx
        OpType_JmpTrue,                 // ABx  if R(A) then pc += Bx
        OpType_JmpNil,                  // ABx  if R(A) == nil then pc += Bx
        OpType_Jmp,                     // Bx   pc += Bx
        OpType_Neg,                     // A    R(A) = -R(A)
        OpType_Not,                     // A    R(A) = not R(A)
        OpType_Len,                     // A    R(A) = #R(A)
        OpType_Add,                     // ABC  R(A) = R(B) + R(C)
        OpType_Sub,                     // ABC  R(A) = R(B) - R(C)
        OpType_Mul,                     // ABC  R(A) = R(B) * R(C)
        OpType_Div,                     // ABC  R(A) = R(B) / R(C)
        OpType_Pow,                     // ABC  R(A) = R(B) ^ R(C)
        OpType_Mod,                     // ABC  R(A) = R(B) % R(C)
        OpType_Concat,                  // ABC  R(A) = R(B) .. R(C)
        OpType_Less,                    // ABC  R(A) = R(B) < R(C)
        OpType_Greater,                 // ABC  R(A) = R(B) > R(C)
        OpType_Equal,                   // ABC  R(A) = R(B) == R(C)
        OpType_UnEqual,                 // ABC  R(A) = R(B) ~= R(C)
        OpType_LessEqual,               // ABC  R(A) = R(B) <= R(C)
        OpType_GreaterEqual,            // ABC  R(A) = R(B) >= R(C)
        OpType_NewTable,                // A    R(A) = {}
        OpType_AppendTable,             // AB   R(A).append(R(B)..R(top-1))
        OpType_SetTable,                // ABC  R(A)[R(B)] = R(C)
        OpType_GetTable,                // ABC  R(C) = R(A)[R(B)]
        OpType_TableIter,               // AB   R(A) = get_iter(R(B))
        OpType_TableIterNext,           // ABC  R(B) = iter_key(R(A)), R(C) = iter_key(R(A)) 
        OpType_ForInit,                 // ABC  For init, make sure R(a),R(b),R(c) type is number
        OpType_ForCheck,                // ABC  if CheckStep(R(A),R(B),R(C)) { ++pc } next code is jmp tail
        OpType_FillNilFromTopToA,       // A    R(top)..R(A) := nil; top must be set before
        OpType_CloseUpvalue,            // A    close upvalue to R(A)
    }

    public struct Instruction
    {
        System.Int32 _opcode;
        public int GetCode()
        {
            return _opcode;
        }
        public static Instruction ConvertFrom(int val)
        {
            Instruction code;
            code._opcode = val;
            return code;
        }
        public Instruction(OpType op, int res)
        {
            _opcode = (((int)op) << 24) | (res & 0xffffff);
        }
        public void SetBx(int bx)
        {
            Debug.Assert(Int16.MinValue <= bx && bx <= Int16.MaxValue);
            _opcode = _opcode | (bx & 0xffff);
        }
        public int GetBx()
        {
            return (Int16)(_opcode & 0xffff);
        }
        public OpType GetOp()
        {
            return (OpType)((_opcode >> 24) & 0xff);
        }
        public int GetA()
        {
            return (_opcode >> 16) & 0xff;
        }
        public int GetB()
        {
            return (_opcode >> 8) & 0xff;
        }
        public int GetC()
        {
            return _opcode & 0xff;
        }
        public static Instruction A(OpType op, int a)
        {
            Debug.Assert(0 <= a && a <= Byte.MaxValue);
            return new Instruction(op, a << 16);
        }
        public static Instruction AB(OpType op, int a, int b)
        {
            Debug.Assert(0 <= a && a <= Byte.MaxValue);
            Debug.Assert(0 <= b && b <= Byte.MaxValue);
            return new Instruction(op, (a<<16) | (b<<8));
        }
        public static Instruction ABC(OpType op, int a, int b,int c)
        {
            Debug.Assert(0 <= a && a <= Byte.MaxValue);
            Debug.Assert(0 <= b && b <= Byte.MaxValue);
            Debug.Assert(0 <= c && c <= Byte.MaxValue);
            return new Instruction(op, (a << 16) | (b << 8) | (c));
        }
        public static Instruction ABx(OpType op, int a, int bx)
        {
            Debug.Assert(0 <= a && a <= Byte.MaxValue);
            Debug.Assert(Int16.MinValue <= bx && bx <= Int16.MaxValue);
            return new Instruction(op, (a<<16) | bx);
        }
        public static Instruction Bx(OpType op, int bx)
        {
            Debug.Assert(Int16.MinValue <= bx && bx <= Int16.MaxValue);
            return new Instruction(op, bx);
        }
    }
}
