using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{
    enum ThreadStatus
    {
        InValid,
        Stop,
        Runing,
        Finished,
        Error,
    }
    /// <summary>
    /// 执行线程
    /// 1. 维护运行栈：局部变量栈，函数调用栈
    /// 2. 状态机：stop, runing, finished, error。
    /// </summary>
    class Thread
    {
        object[] _stack = new object[OmsConf.MAX_STACK_SIZE];
        
        class CallInfo
        {
            public int register_idx;
            public int pc;
            public int func_idx;
        }

        List<CallInfo> _calls = new List<CallInfo>();

        ThreadStatus _status = ThreadStatus.Stop;
        public ThreadStatus GetStatus()
        {
            return _status;
        }

        VM _vm;
        public Thread(VM vm_)
        {
            _vm = vm_;
        }

        public void Reset(Function func)
        {
            var closure = new Closure();
            closure.func = func;
            _stack[0] = closure;

            var call = new CallInfo();
            call.register_idx = 1;
            call.pc = 0;
            call.func_idx = 0;
            _calls.Clear();
            _calls.Add(call);
        }



        public void Resume()
        {

        }

        public void StopToWait()
        {

        }

        void ExecuteFrame()
        {
            CallInfo call = _calls.Last<CallInfo>();
            Closure closure = _stack[call.func_idx] as Closure;
            Function func = closure.func;

            int pc = call.pc;
            int code_size = func.OpCodeSize();
            int register = call.register_idx;

            int a, b, c, bx;
            UpValue upvalue;

            while(pc < code_size)
            {
                Instruction i = func.GetInstruction(pc);
                ++pc;

                a = register + i.GetA();
                b = register + i.GetB();
                c = register + i.GetC();
                bx = i.GetBx();

                switch(i.GetOp())
                {
                    case OpType.OpType_LoadNil:
                        _stack[a] = null;
                        break;
                    case OpType.OpType_LoadBool:
                        _stack[a] = (bx == 1 ? true : false);
                        break;
                    case OpType.OpType_LoadInt:
                        _stack[a] = (double)bx;
                        break;
                    case OpType.OpType_LoadConst:
                        _stack[a] = func.GetConstValue(bx);
                        break;
                    case OpType.OpType_Move:
                        _stack[a] = _stack[b];
                        break;
                    case OpType.OpType_Call:
                        // todo
                        break;
                    case OpType.OpType_GetUpvalue:
                        upvalue = closure.GetUpvalue(bx);
                        if(upvalue.IsClosed())
                            _stack[a] = upvalue.obj;
                        else
                            _stack[a] = _stack[upvalue.idx];
                        break;
                    case OpType.OpType_SetUpvalue:
                        upvalue = closure.GetUpvalue(bx);
                        if(upvalue.IsClosed())
                            upvalue.obj = _stack[a];
                        else
                            _stack[upvalue.idx] = _stack[a];
                        break;
                    case OpType.OpType_GetGlobal:
                        _stack[a] = _vm.m_global.GetValue(func.GetConstValue(bx));
                        break;
                    case OpType.OpType_SetGlobal:
                        _vm.m_global.SetValue(func.GetConstValue(bx), _stack[a]);
                        break;
                    case OpType.OpType_Closure:
                        // todo
                        break;
                    case OpType.OpType_VarArg:
                        // todo
                        break;
                    case OpType.OpType_Ret:
                        // todo
                        break;
                    case OpType.OpType_Jmp:
                        pc += -1 + bx;
                        break;
                    case OpType.OpType_JmpFalse:
                        if (ValueUtils.IsFalse(_stack[a]))
                            pc += -1 + bx;
                        break;
                    case OpType.OpType_JmpTrue:
                        if (!ValueUtils.IsFalse(_stack[a]))
                            pc += -1 + bx;
                        break;
                    case OpType.OpType_JmpNil:
                        if (null == _stack[a])
                            pc += -1 + bx;
                        break;
                    case OpType.OpType_Neg:
                        _stack[a] = -ValueUtils.ToNumber(_stack[a]);
                        break;
                    case OpType.OpType_Not:
                        _stack[a] = ValueUtils.IsFalse(_stack[a]);
                        break;
                    case OpType.OpType_Len:
                        // todo
                        break;
                    case OpType.OpType_Add:
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) + ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_Sub:
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) - ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_Mul:
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) * ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_Div:
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) / ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_Pow:
                        _stack[a] = Math.Pow(ValueUtils.ToNumber(_stack[b]), ValueUtils.ToNumber(_stack[c]));
                        break;
                    case OpType.OpType_Mod:
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) % ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_Concat:
                        // todo 
                        break;
                    case OpType.OpType_Less:
                        // todo
                        break;
                    case OpType.OpType_Greater:
                        // todo
                        break;
                    case OpType.OpType_Equal:
                        // todo 
                        break;
                    case OpType.OpType_UnEqual:
                        // todo
                        break;
                    case OpType.OpType_LessEqual:
                        // todo
                        break;
                    case OpType.OpType_GreaterEqual:
                        // todo
                        break;
                    case OpType.OpType_NewTable:
                        // todo
                        break;
                    case OpType.OpType_SetTable:
                        // todo
                        break;
                    case OpType.OpType_GetTable:
                        // todo
                        break;
                    case OpType.OpType_ForStep:
                        // todo
                        break;
                    case OpType.OpType_CloseUpvalue:
                        // todo
                        break;
                    case OpType.OpType_SetTop:
                        // todo
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
            }
        }
    }
}
