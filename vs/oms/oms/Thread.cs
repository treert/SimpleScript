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

        public void ConsumeAllResult()
        {
            _top = _register;
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

        object[] _stack = new object[OmsConf.MAX_STACK_SIZE];
        int _top = 0;
        // save register base for cfunction call
        int _register = 0;
        LinkedList<UpValue> _upvalues = new LinkedList<UpValue>();
        void CloseUpvalueTo(int a)
        {
            while(_upvalues.Count > 0)
            {
                UpValue upvalue = _upvalues.Last<UpValue>();
                if (upvalue.idx >= a)
                {
                    upvalue.Close(_stack[upvalue.idx]);
                    _upvalues.RemoveLast();
                }
                else
                    break;
            }
        }
        
        class CallInfo
        {
            public int register_idx;
            public int pc;
            public int func_idx;
        }

        LinkedList<CallInfo> _calls = new LinkedList<CallInfo>();

        string _error_msg = null;
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
            _top = 1;

            var call = new CallInfo();
            call.register_idx = 1;
            call.pc = 0;
            call.func_idx = 0;
            _calls.Clear();
            _calls.AddLast(call);
            
        }

        public void Reset(Closure closure)
        {
            _stack[0] = closure;
            _top = 1;

            var call = new CallInfo();
            call.register_idx = 1;
            call.pc = 0;
            call.func_idx = 0;
            _calls.Clear();
            _calls.AddLast(call);
        }

        public void Resume()
        {
            _status = ThreadStatus.Runing;
            Execute();
        }

        public void StopToWait()
        {
            _status = ThreadStatus.Stop;
        }

        public void Error(string msg)
        {
            _error_msg = msg;
            _status = ThreadStatus.Error;
        }

        bool Call(int a,int b,bool any_value)
        {
            if(any_value)
            {
                b = _top - a - 1;
            }

            if(_stack[a] is Closure)
            {
                CallClosure(_stack[a] as Closure, a, b);
                return true;
            }
            else
            {
                CallCFunction(_stack[a] as CFunction, a, b);
                return false;
            }
        }

        void CallClosure(Closure closure,int base_idx, int arg_count)
        {
            CallInfo call = new CallInfo();
            Function func = closure.func;

            call.func_idx = base_idx;
            call.pc = 0;

            int fixed_args = func.GetFixedArgCount();
            int arg_idx = base_idx + 1;
            if(func.HasVararg())
            {
                // move args for var_arg
                int old_arg = arg_idx;
                arg_idx += arg_count;
                for (int i = 0; i < arg_count && i < fixed_args; ++i)
                    _stack[arg_idx + i] = _stack[old_arg++];
            }
            call.register_idx = arg_idx;
            // fill nil for rest fixed_arg
            for(int i = arg_count; i < fixed_args; ++i)
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

        void GenerateClosure(int a,Function func)
        {
            var call = _calls.Last.Value;
            Closure closure = _stack[call.func_idx] as Closure;


            Closure new_closure = new Closure();
            new_closure.func = func;
            _stack[a] = new_closure;

            // prepare upvalues
            int count = func.GetUpValueCount();
            for(int i = 0; i < count; ++i)
            {
                var upvalue_info = func.GetUpValueInfo(i);
                if(upvalue_info.is_parent_local)
                {
                    UpValue upvalue = null;
                    int reg = call.register_idx + upvalue_info.register;
                    // find upvalue
                    var iter = _upvalues.Last;
                    while(iter != null)
                    {
                        if(iter.Value.idx <= reg)
                        {
                            break;
                        }
                        else
                        {
                            iter = iter.Previous;
                        }
                    }
                    if(iter == null)
                    {
                        upvalue = new UpValue();
                        upvalue.idx = reg;
                        _upvalues.AddFirst(upvalue);
                    }
                    else if(iter.Value.idx < reg)
                    {
                        upvalue = new UpValue();
                        upvalue.idx = reg;
                        _upvalues.AddAfter(iter, upvalue);
                    }
                    else
                    {
                        upvalue = iter.Value;
                    }
                    new_closure.AddUpvalue(upvalue);
                }
                else
                {
                    var upvalue = closure.GetUpvalue(upvalue_info.register);
                    new_closure.AddUpvalue(upvalue);
                }
            }
        }

        void Return(int a, int ret_count, bool ret_any)
        {
            var call = _calls.Last.Value;
            int src = a;
            int dst = call.func_idx;

            // ret will copy result over regiter, need close upvalue now
            CloseUpvalueTo(dst);
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

        void Execute()
        {
            while(_status == ThreadStatus.Runing && _calls.Count > 0)
            {
                ExecuteFrame();
            }
            if(_status == ThreadStatus.Runing)
            {
                _status = ThreadStatus.Finished;
                _register = 0;
            }
        }

        void ExecuteFrame()
        {
            CallInfo call = _calls.Last<CallInfo>();
            Closure closure = _stack[call.func_idx] as Closure;
            Function func = closure.func;
            int code_size = func.OpCodeSize();

            int a, b, c, bx;
            UpValue upvalue;


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
                    case OpType.OpType_Move:
                        _stack[a] = _stack[b];
                        break;
                    case OpType.OpType_Call:
                        if (Call(a, i.GetB(), i.GetC() == 1))
                        {
                            // will enter next frame
                            return;
                        }
                        if (_status == ThreadStatus.Stop)
                        {
                            return;
                        }
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
                        GenerateClosure(a, func.GetChildFunction(bx));
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
                        (_stack[a] as Table).SetValue(_stack[b], _stack[c]);
                        break;
                    case OpType.OpType_GetTable:
                        _stack[c] = (_stack[a] as Table).GetValue(_stack[b]);
                        break;
                    case OpType.OpType_ForStep:
                        // todo
                        break;
                    case OpType.OpType_CloseUpvalue:
                        CloseUpvalueTo(a);
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
