using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    /// <summary>
    /// 执行线程【不支持协程，一个VM只维护一个执行线程，简化实现】
    ///     1. 执行指令
    ///     2. 维护堆栈
    /// </summary>
    public class Thread
    {
        public VM VM
        {
            get
            {
                return _vm;
            }
        }

        public int GetStatckSize()
        {
            return _top - _register;
        }

        public object GetValue(int idx)
        {
            int index;
            if (idx < 0)
                index = _top + idx;
            else
                index = _register + idx;

            if (_register <= index && index < _top)
                return _stack[index];
            else
                return null;
        }

        public void PushValue(object obj)
        {
            _stack[_top++] = obj;
        }

        public int GetTopIdx()
        {
            return _top;
        }

        object[] _stack = new object[OmsConf.MAX_STACK_SIZE];
        int _top = 0;
        // save register base for cfunction call
        int _register = 0;
        
        class CallInfo
        {
            public int register_idx;
            public int pc;
            public int func_idx;
            public bool is_cfuntion = false;
        }

        LinkedList<CallInfo> _calls = new LinkedList<CallInfo>();

        string _error_msg = null;

        VM _vm;
        public Thread(VM vm_)
        {
            _vm = vm_;
        }

        void Error(string msg)
        {
            _error_msg = msg;
            // todo 
            // throw exception or ...
        }

        public void Run(int func_idx)
        {
            if(Call(func_idx, -1, true))
            {
                Execute();
            }
        }

        bool Call(int func_idx,int arg_count,bool any_value)
        {
            if(any_value)
            {
                arg_count = _top - func_idx - 1;
            }

            if(_stack[func_idx] is Function)
            {
                CallFunction(_stack[func_idx] as Function, func_idx, arg_count);
                return true;
            }
            else
            {
                Debug.Assert(_stack[func_idx] is CFunction);
                CallCFunction(_stack[func_idx] as CFunction, func_idx, arg_count);
                return false;
            }
        }

        void CallFunction(Function func, int func_idx, int arg_count)
        {
            CallInfo call = new CallInfo();
            call.func_idx = func_idx;
            call.pc = 0;

            int fixed_args = func.GetFixedArgCount();
            int arg_idx = func_idx + 1;
            if (func.HasVararg())
            {
                // move args for var_arg
                int old_arg = arg_idx;
                arg_idx += arg_count;
                for (int i = 0; i < arg_count && i < fixed_args; ++i)
                    _stack[arg_idx + i] = _stack[old_arg + i];
            }
            call.register_idx = arg_idx;
            // fill nil for rest fixed_arg
            for (int i = arg_count; i < fixed_args; ++i)
            {
                _stack[arg_idx + i] = null;
            }

            _calls.AddLast(call);
        }

        void CallCFunction(CFunction cfunc,int base_idx, int arg_count)
        {
            _top = base_idx + 1 + arg_count;
            _register = base_idx + 1;
            // call c function
            int ret_count = cfunc(this);

            // copy result to stack
            int src = _top - ret_count;
            int dst = base_idx;
            for(int i = 0; i < ret_count; ++i)
            {
                _stack[dst + i] = _stack[src + i];
            }
            _top = dst + ret_count;
            _stack[_top] = null;
            // yield or chunck return can get all args
            _register = base_idx;
        }

        void Return(int a, int ret_count, bool ret_any)
        {
            var call = _calls.Last.Value;
            int src = a;
            int dst = call.func_idx;

            // ret will copy result over regiter, need shrink stack now
            // todo
            if(ret_any)
            {
                ret_count = _top - src;
            }
            for(int i = 0; i < ret_count; ++i)
            {
                _stack[dst + i] = _stack[src + i];
            }

            // set new top and pop current callinfo
            _top = dst + ret_count;
            _stack[_top] = null;
            _calls.RemoveLast();
        }

        bool OpForCheck(int a, int b, int c)
        {
            double var = ValueUtils.ToNumber(_stack[a]);
            double limit = ValueUtils.ToNumber(_stack[b]);
            double step = ValueUtils.ToNumber(_stack[c]);

            if (step > 0)
            {
                return var <= limit;
            }
            else if(step < 0)
            {
                return var >= limit;
            }
            return false;
        }

        void Execute()
        {
            while(_calls.Count > 0)
            {
                if(_calls.Last<CallInfo>().is_cfuntion)
                {
                    return;// call from outside, just return, CallCFuntion will remove it at last 
                }
                ExecuteFrame();
            }
        }

        void ExecuteFrame()
        {
            CallInfo call = _calls.Last<CallInfo>();
            Function func = _stack[call.func_idx] as Function;
            int code_size = func.OpCodeSize();

            int a, b, c, bx;

            while (call.pc < code_size)
            {
                Instruction i = func.GetInstruction(call.pc);
                ++call.pc;

                a = call.register_idx + i.GetA();
                b = call.register_idx + i.GetB();
                c = call.register_idx + i.GetC();
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
                    case OpType.OpType_LoadFunc:
                        _stack[a] = func.GetChildFunction(bx);
                        break;
                    case OpType.OpType_Move:
                        _stack[a] = _stack[b];
                        break;
                    case OpType.OpType_Call:
                        if (Call(a, i.GetB(), i.GetC() == 1))
                        {
                            // will enter next frame
                            return;
                        }
                        break;
                    case OpType.OpType_GetGlobal:
                        _stack[a] = _vm.m_global.GetValue(func.GetConstValue(bx));
                        break;
                    case OpType.OpType_SetGlobal:
                        _vm.m_global.SetValue(func.GetConstValue(bx), _stack[a]);
                        break;
                    case OpType.OpType_VarArg:
                        // todo
                        break;
                    case OpType.OpType_Ret:
                        Return(a, i.GetB(), i.GetC() == 1);
                        return;
                        //break;
                    case OpType.OpType_Jmp:
                        call.pc += -1 + bx;
                        break;
                    case OpType.OpType_JmpFalse:
                        if (ValueUtils.IsFalse(_stack[a]))
                            call.pc += -1 + bx;
                        break;
                    case OpType.OpType_JmpTrue:
                        if (!ValueUtils.IsFalse(_stack[a]))
                            call.pc += -1 + bx;
                        break;
                    case OpType.OpType_JmpNil:
                        if (null == _stack[a])
                            call.pc += -1 + bx;
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
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) < ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_Greater:
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) > ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_Equal:
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) == ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_UnEqual:
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) != ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_LessEqual:
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) <= ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_GreaterEqual:
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) >= ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_NewTable:
                        _stack[a] = _vm.NewTable();
                        break;
                    case OpType.OpType_SetTable:
                        (_stack[a] as Table).SetValue(_stack[b], _stack[c]);
                        break;
                    case OpType.OpType_GetTable:
                        _stack[c] = (_stack[a] as Table).GetValue(_stack[b]);
                        break;
                    case OpType.OpType_TableIter:
                        _stack[a] = (_stack[b] as Table).GetIter();
                        break;
                    case OpType.OpType_TableIterNext:
                        (_stack[a] as Table.Iterator).Next(out _stack[b], out _stack[c]);
                        break;
                    case OpType.OpType_ForCheck:
                        if(OpForCheck(a,b,c))
                        {
                            ++call.pc;// jump over JumpTail instruction
                        }
                        break;
                    case OpType.OpType_StackShrink:
                        // todo shrink
                        break;
                    case OpType.OpType_SetTop:
                        while(_top < a)
                        {
                            _stack[_top++] = null;
                        }
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
            }

            _top = call.func_idx;
            _stack[_top] = null;
            _calls.RemoveLast();
        }
    }
}
