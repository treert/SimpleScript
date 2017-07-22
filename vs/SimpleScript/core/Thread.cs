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

        public int GetCFunctionArgCount()
        {
            return _active_top - _cfunc_register_idx;
        }

        public object GetCFunctionArg(int idx)
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

        public void PushCFunctionValue(object obj)
        {
            PushValue(obj);
        }

        internal void PushValue(object obj)
        {
            _stack[_active_top++] = obj;
        }

        internal object GetValue(int idx)
        {
            return _stack[idx];
        }

        public int GetTopIdx()
        {
            return _active_top;
        }

        public void SetModuleEnv(Table table)
        {
            if(_calls.Count > 0)
            {
                _calls.Peek().closure.env_table = table;
            }
            else
            {
                Error("moudule(xxx) should use from script");
            }
        }

        public void Clear()
        {
            Debug.Assert(_max_used_top < OmsConf.MAX_STACK_SIZE);
            for(int i = 0; i < _max_used_top; ++i)
            {
                _stack[i] = null;// so c# can gc
            }
            OpCloseUpvalueTo(0);
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
            public Closure closure;
        }

        Stack<CallInfo> _calls = new Stack<CallInfo>(256);

        #region upvalue

        LinkedList<UpValue> _upvalues = new LinkedList<UpValue>();

        UpValue NewUpValue(int idx)
        {
            return new UpValue(_stack, idx);
        }

        void OpCloseUpvalueTo(int a)
        {
            while (_upvalues.Count > 0)
            {
                UpValue upvalue = _upvalues.Last<UpValue>();
                if (upvalue.idx >= a)
                {
                    upvalue.Close();
                    _upvalues.RemoveLast();
                }
                else
                    break;
            }
        }

        void OpGenerateClosure(int a, Function func)
        {
            CallInfo call = _calls.Peek();
            Closure closure = call.closure;

            Closure new_closure = _vm.NewClosure();
            new_closure.func = func;
            new_closure.env_table = closure.env_table;
            _stack[a] = new_closure;

            // prepare upvalues
            int count = func.GetUpValueCount();
            for (int i = 0; i < count; ++i)
            {
                var upvalue_info = func.GetUpValueInfo(i);
                if (upvalue_info.is_parent_local)
                {
                    UpValue upvalue = null;
                    int reg = call.register_idx + upvalue_info.register;
                    // find upvalue
                    var iter = _upvalues.Last;
                    while (iter != null)
                    {
                        if (iter.Value.idx <= reg)
                        {
                            break;
                        }
                        else
                        {
                            iter = iter.Previous;
                        }
                    }
                    if (iter == null)
                    {
                        upvalue = NewUpValue(reg);
                        _upvalues.AddFirst(upvalue);
                    }
                    else if (iter.Value.idx < reg)
                    {
                        upvalue = NewUpValue(reg);
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

        #endregion

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
            // throw new RuntimeException(msg);
        }

        internal void Run()
        {
            //try
            {
                if (OpCall(0, -1, true))
                {
                    Execute();
                }
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
            Closure closure = call.closure;
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
                    case OpType.OpType_Closure:
                        OpGenerateClosure(a, func.GetChildFunction(bx));
                        break;
                    case OpType.OpType_Call:
                        if (OpCall(a, i.GetB(), i.GetC() == 1))
                        {
                            // will enter next frame
                            return;
                        }
                        break;
                    case OpType.OpType_GetUpvalue:
                        upvalue = closure.GetUpvalue(bx);
                        _stack[a] = upvalue.Read();
                        break;
                    case OpType.OpType_SetUpvalue:
                        upvalue = closure.GetUpvalue(bx);
                        upvalue.Write(_stack[a]);
                        break;
                    case OpType.OpType_GetGlobal:
                        _stack[a] = closure.env_table.GetValue(func.GetConstValue(bx));
                        break;
                    case OpType.OpType_SetGlobal:
                        closure.env_table.SetValue(func.GetConstValue(bx), _stack[a]);
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
                        OpNeg(a);
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
                            throw NewOpTypeError("get length of ", a);
                        }
                        break;
                    case OpType.OpType_Add:
                        CheckArithType(b, c, "add");
                        _stack[a] = (double)(_stack[b]) + (double)(_stack[c]);
                        break;
                    case OpType.OpType_Sub:
                        CheckArithType(b, c, "sub");
                        _stack[a] = (double)(_stack[b]) - (double)(_stack[c]);
                        break;
                    case OpType.OpType_Mul:
                        CheckArithType(b, c, "multiply");
                        _stack[a] = (double)(_stack[b]) * (double)(_stack[c]);
                        break;
                    case OpType.OpType_Div:
                        CheckArithType(b, c, "div");
                        _stack[a] = (double)(_stack[b]) / (double)(_stack[c]);
                        break;
                    case OpType.OpType_Pow:
                        CheckArithType(b, c, "power");
                        _stack[a] = Math.Pow((double)(_stack[b]), (double)(_stack[c]));
                        break;
                    case OpType.OpType_Mod:
                        CheckArithType(b, c, "mod");
                        _stack[a] = (double)(_stack[b]) % (double)(_stack[c]);
                        break;
                    case OpType.OpType_Concat:
                        OpConcat(a,b,c);
                        break;
                    case OpType.OpType_Less:
                        CheckCompareType(b, c, "<");
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) < ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_Greater:
                        CheckCompareType(b, c, ">");
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) > ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_Equal:
                        _stack[a] = Object.Equals(_stack[b], _stack[c]);
                        break;
                    case OpType.OpType_UnEqual:
                        _stack[a] = !Object.Equals(_stack[b], _stack[c]);
                        break;
                    case OpType.OpType_LessEqual:
                        CheckCompareType(b, c, "<=");
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) <= ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_GreaterEqual:
                        CheckCompareType(b, c, ">=");
                        _stack[a] = ValueUtils.ToNumber(_stack[b]) >= ValueUtils.ToNumber(_stack[c]);
                        break;
                    case OpType.OpType_NewTable:
                        _stack[a] = _vm.NewTable();
                        break;
                    case OpType.OpType_AppendTable:
                        OpTableAppend(a, b, _active_top - b);
                        break;
                    case OpType.OpType_SetTable:
                        OpSetTable(a, b, c);
                        break;
                    case OpType.OpType_GetTable:
                        OpGetTable(a, b, c);
                        break;
                    case OpType.OpType_TableIter:
                        if (_stack[a] is Table)
                            _stack[a] = (_stack[b] as Table).GetIter();
                        else
                            throw NewOpTypeError("foreach ", a);
                        break;
                    case OpType.OpType_TableIterNext:
                        Debug.Assert(_stack[a] is Table.Iterator);
                        (_stack[a] as Table.Iterator).Next(out _stack[b], out _stack[c]);
                        break;
                    case OpType.OpType_ForInit:
                        if (!(_stack[a] is double))
                            throw NewRuntimeError("'for' init need be number");
                        if (!(_stack[b] is double))
                            throw NewRuntimeError("'for' limit need be number");
                        if (!(_stack[c] is double))
                            throw NewRuntimeError("'for' step need be number");
                        if (((double)_stack[c] == 0))
                            throw NewRuntimeError("'for' step should be nozero");
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
                    case OpType.OpType_CloseUpvalue:
                        OpCloseUpvalueTo(a);
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
            }

            // return nil
            OpReturn(call.func_idx, -1, 0, false);
        }

        Tuple<string, string> GetOperandNameAndScope(int idx)
        {
            var call = _calls.Peek();
            var reg = idx - call.register_idx;
            var closure = call.closure;
            var func = call.closure.func;
            var pc = call.pc - 1;

            // Search last instruction which dst register is reg,
            // and get the name base on the instruction
            while(pc > 0)
            {
                --pc;
                Instruction instruction = func.GetInstruction(pc);
                switch(instruction.GetOp())
                {
                    case OpType.OpType_GetGlobal:
                        if(reg == instruction.GetA())
                        {
                            int bx = instruction.GetBx();
                            var key = func.GetConstValue(bx);
                            if (key is string)
                                return new Tuple<string, string>("global", key as string);
                            else
                                return new Tuple<string, string>("global", "?");
                        }
                        break;
                    case OpType.OpType_Move:
                        if (reg == instruction.GetA())
                        {
                            int b = instruction.GetB();
                            var name = func.GetLocalVarNameByPc(b, pc);// todo@om has bug ??
                            if (name != null)
                                return new Tuple<string, string>("local", name);
                            else
                                return new Tuple<string, string>("?", "?");
                        }
                        break;
                    case OpType.OpType_GetUpvalue:
                        if (reg == instruction.GetA())
                        {
                            int bx = instruction.GetBx();
                            var upvalue_info = func.GetUpValueInfo(bx);
                            return new Tuple<string, string>("upvalue", upvalue_info.name);
                        }
                        break;
                    case OpType.OpType_GetTable:
                        if(reg == instruction.GetC())
                        {
                            int b = instruction.GetB();
                            var key_idx = call.register_idx + b;
                            // todo@om has bug ??
                            if (_stack[key_idx] is string)
                                return new Tuple<string, string>("table member", _stack[key_idx] as string);
                            else
                                return new Tuple<string, string>("table member", "?");
                        }
                        break;
                }
            }

            return new Tuple<string, string>("?", "?");
        }

        Tuple<string, int> GetCurrentInstructionPos()
        {
            var call = _calls.Peek();
            var module_name = call.closure.func.GetModuleName();
            var line = call.closure.func.GetInstructionLine(call.pc - 1);
            return new Tuple<string, int>(module_name, line);
        }

        RuntimeException NewRuntimeError(string format, params object[] args)
        {
            var pos = GetCurrentInstructionPos();
            return new RuntimeException(pos.Item1, pos.Item2, format, args);
        }

        RuntimeException NewOpTypeError(string op, int idx)
        {
            var info = GetOperandNameAndScope(idx);
            return NewRuntimeError("attempt to {0} {1} '{2}' (a {3} value) ",
                op, info.Item1, info.Item2, ValueUtils.GetTypeName(_stack[idx]));
        }

        void CheckArithType(int b, int c, string op)
        {
            if(_stack[b] is double && _stack[c] is double)
            {
               
            }
            else
            {
                throw NewRuntimeError("attemp to {0} {1} with {2}",
                    op, ValueUtils.GetTypeName(_stack[b]), ValueUtils.GetTypeName(_stack[c]));
            }
        }

        void CheckCompareType(int b, int c, string op)
        {
            if (_stack[b] is double && _stack[c] is double)
            {

            }
            else
            {
                throw NewRuntimeError("attemp to compare({0}) {1} with {2}",
                    op, ValueUtils.GetTypeName(_stack[b]), ValueUtils.GetTypeName(_stack[c]));
            }
        }

        void CheckTableType(int a, int b, string op)
        {
            if (_stack[a] is Table && _stack[b] != null)
                return;

            if(_stack[b] == null)
            {
                throw NewRuntimeError("the key of Table can not be nil");
            }
            else
            {
                throw NewOpTypeError(op, a);
            }
        }

        void OpSetTable(int a, int b, int c)
        {
            if (_stack[b] == null || _stack[a] == null)
            {
                throw NewRuntimeError("the key of Table can not be nil");
            }

            var obj = _stack[a];
            if(obj is Table)
            {
                (obj as Table).SetValue(_stack[b], _stack[c]);
            }
            else if(obj is IUserData)
            {
                (obj as IUserData).Set(_stack[b], _stack[c]);
            }
            else
            {
                var handler = VM.m_import_manager.GetOrCreateHandler(obj.GetType());
                handler.SetValue(obj, _stack[b], _stack[c]);
            }
        }

        void OpGetTable(int a, int b, int c)
        {
            if (_stack[b] == null || _stack[a] == null)
            {
                throw NewRuntimeError("the key of Table can not be nil");
            }

            var obj = _stack[a];
            if (obj is Table)
            {
                _stack[c] = (obj as Table).GetValue(_stack[b]);
            }
            else if (obj is IUserData)
            {
                _stack[c] = (obj as IUserData).Get(_stack[b]);
            }
            else
            {
                var handler = VM.m_import_manager.GetOrCreateHandler(obj.GetType());
                _stack[c] = handler.GetValue(obj, _stack[b]);
            }
        }

        void OpNeg(int a)
        {
            var obj = _stack[a];
            if(obj is double)
            {
                _stack[a] = -(double)(obj);
            }
            else
            {
                throw NewOpTypeError("neg", a);
            }
        }

        void OpTableAppend(int a, int idx, int count)
        {
            Table table = _stack[a] as Table;
            if(table == null)
            {
                throw NewOpTypeError("append value to", a);
            }
            for(int i = 0; i < count; ++i)
            {
                table.Add(_stack[idx + i]);
            }
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

            if (func is Closure)
            {
                CallClosure(func as Closure, func_idx, arg_count);
                return true;// ready to enter new frame
            }
            else if (func is CFunction)
            {
                OpCallCFunction(func as CFunction, func_idx, arg_count);
                return false;// continue current frame
            }
            else
            {
                throw NewOpTypeError("call", func_idx);
            }
        }

        void CallClosure(Closure closure, int func_idx, int arg_count)
        {
            CallInfo call = new CallInfo();
            Function func = closure.func;

            call.closure = closure;
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
                    _stack[arg_idx + i] = _stack[old_arg++];
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
            // ret will copy result over regiter, need close upvalue now
            OpCloseUpvalueTo(dst);

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
            Function func = call.closure.func;
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
            Debug.Assert(_stack[a] is double && _stack[b] is double && _stack[c] is double);
            double var = (double)(_stack[a]);
            double limit = (double)(_stack[b]);
            double step = (double)(_stack[c]);

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
