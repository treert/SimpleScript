using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    /// <summary>
    /// 执行线程【CFuntionCall里再次使用vm，新开执行线程，好处：避免函数调用栈来回穿插】
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
            return _active_top - _cfunc_register_idx;
        }

        public object GetValue(int idx)
        {
            int index;
            if (idx < 0)
                index = _active_top + idx;
            else
                index = _cfunc_register_idx + idx;

            if (_cfunc_register_idx <= index && index < _active_top)
                return _stack[index];
            else
                return null;
        }

        public void PushValue(object obj)
        {
            _stack[_active_top++] = obj;
        }

        public int GetTopIdx()
        {
            return _active_top;
        }

        public void Clear()
        {
            Debug.Assert(_max_used_top < OmsConf.MAX_STACK_SIZE);
            for(int i = 0; i < _max_used_top; ++i)
            {
                _stack[i] = null;
            }
            _calls.Clear();
            _active_top = 0;
            _max_used_top = 0;
        }

        public bool IsRuning()
        {
            return _active_top > 0;
        }

        object[] _stack = new object[OmsConf.MAX_STACK_SIZE];
        int _max_used_top = 0;

        void SetMaxUsedStackIdx(int idx)
        {
            _max_used_top = Math.Max(_max_used_top, idx);
            if(_max_used_top > OmsConf.MAX_STACK_SIZE)
            {
                _max_used_top = OmsConf.MAX_STACK_SIZE;
                Error("stack overflow");
            }
        }
        /// <summary>
        /// 特殊的top，不能简单当成栈顶，用途
        /// 1. 函数任意参数，arg_count = top - func_idx - 1
        /// 2. 函数返回值个数，top = last_ret_idx + 1【实现中确保stack[top] == nil】
        /// 3. ...语法会设置这个值，和函数返回值相同
        /// 4. CFuntion操作堆栈时的栈顶
        /// 【注意】是Thread的属性，不是CallInfo的，因为每次只有一个CallInfo处于激活状态，且这个信息对于内部Call来说也就使用一次。
        /// </summary>
        int _active_top = 0;
        /// <summary>
        /// CFuntion的寄存器起始index，stack[register-1] == CFuntion
        /// 【注意】实现有些特殊，这个索引是Thread的属性，而不是CFunctionCall的属性。
        ///         CFuntion里再次call vm会新开thread处理【用于兼容宿主可能有的协程功能】。
        /// </summary>
        int _cfunc_register_idx = 0;
        
        class CallInfo
        {
            public int register_idx;
            public int pc;
            public int func_idx;
            public Function func;
        }

        Stack<CallInfo> _calls = new Stack<CallInfo>(200);

        string _error_msg = null;

        void ThrowError(string msg)
        {
            //if(_calls.Count > 0)
            //{
            //    var call = _calls.Peek();
            //    var func = call.func;

            //    if(func.OpCodeSize)
            //}
        }

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
            throw new RuntimeException(msg);
        }

        public void Run()
        {
            try
            {
                if (OpCall(0, -1, true))
                {
                    Execute();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        void Execute()
        {
            while(_calls.Count > 0)
            {
                ExecuteFrame();
            }
        }

        void ExecuteFrame()
        {
            CallInfo call = _calls.Peek();
            Function func = call.func;
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
                        // copy env table to child
                        // be careful!!! do not like closure,
                        // Function is static object share by all ptr,
                        // so best use module("name.space") at file level.
                        func.CopyEnvToChild(bx);
                        break;
                    case OpType.OpType_Move:
                        _stack[a] = _stack[b];
                        break;
                    case OpType.OpType_Call:
                        if (OpCall(a, i.GetB(), i.GetC() == 1))
                        {
                            // will enter next frame
                            return;
                        }
                        break;
                    case OpType.OpType_GetGlobal:
                        _stack[a] = func.GetEnvTable().GetValue(func.GetConstValue(bx));
                        break;
                    case OpType.OpType_SetGlobal:
                        func.GetEnvTable().SetValue(func.GetConstValue(bx), _stack[a]);
                        break;
                    case OpType.OpType_VarArg:
                        OpCopyVarArg(a, call);
                        break;
                    case OpType.OpType_Ret:
                        OpReturn(call.func_idx, a, i.GetB(), i.GetC() == 1);
                        return;// finish
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
                        if(_stack[a] is Table)
                        {
                            _stack[a] = (_stack[a] as Table).Count();
                        }
                        else
                        {
                            _stack[a] = 0;
                        }
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
                        OpConcat(a,b,c);
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
                    case OpType.OpType_FillNilFromTopToA:
                        while(_active_top < a)
                        {
                            _stack[_active_top++] = null;
                        }
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
            }

            // return nil
            OpReturn(call.func_idx, -1, 0, false);
        }

        bool OpCall(int func_idx, int arg_count, bool any_value)
        {
            if (any_value)
            {
                arg_count = _active_top - func_idx - 1;
            }
            else
            {
                _active_top = func_idx + 1 + arg_count;
            }

            object func = _stack[func_idx];

            if (func is Function)
            {
                OpCallFunction(func as Function, func_idx, arg_count);
                return true;// ready to enter new frame
            }
            else if (func is CFunction)
            {
                OpCallCFunction(func as CFunction, func_idx, arg_count);
                return false;// continue current frame
            }
            else
            {
                throw new RuntimeException("type error");
            }
        }

        void OpCallFunction(Function func, int func_idx, int arg_count)
        {

            CallInfo call = new CallInfo();
            call.func_idx = func_idx;
            call.func = func;
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

            _calls.Push(call);

            SetMaxUsedStackIdx(call.register_idx + func.GetMaxRegisterCount() +
                Math.Max(0, arg_count - fixed_args));// ... will copy all var args
        }

        void OpCallCFunction(CFunction cfunc, int func_idx, int arg_count)
        {
            SetMaxUsedStackIdx(func_idx + OmsConf.MAX_FUNC_REGISTER);

            _cfunc_register_idx = func_idx + 1;
            // call c function
            int ret_count = cfunc(this);

            // copy result to stack
            int src = _active_top - ret_count;
            int dst = func_idx;
            for (int i = 0; i < ret_count; ++i)
            {
                _stack[dst + i] = _stack[src + i];
            }
            _active_top = dst + ret_count;
            _stack[_active_top] = null;
        }

        void OpReturn(int dst, int src, int ret_count, bool ret_any)
        {
            if (ret_any)
            {
                ret_count = _active_top - src;
            }

            for (int i = 0; i < ret_count; ++i)
            {
                _stack[dst + i] = _stack[src + i];
            }

            // set new top and pop current callinfo
            _active_top = dst + ret_count;
            _stack[_active_top] = null;
            _calls.Pop();
        }

        void OpCopyVarArg(int dst, CallInfo call)
        {
            Function func = call.func;
            int src = call.func_idx + 1 + func.GetFixedArgCount();
            while (src < call.register_idx)
            {
                _stack[dst++] = _stack[src++];
            }
            _active_top = dst;
            _stack[_active_top] = null;
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
            else if (step < 0)
            {
                return var >= limit;
            }
            return false;
        }

        private void OpConcat(int a, int b, int c)
        {
            _stack[a] = ValueUtils.ToString(_stack[b]) + ValueUtils.ToString(_stack[c]);
        }
    }
}
