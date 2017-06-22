using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    enum LexicalScope
    {
        UnKown,
        Global,
        Local,
    }
    /// <summary>
    /// 生成函数Funtion
    /// </summary>
    class CodeGenerate
    {
        public Function Generate(SyntaxTree tree)
        {
            Debug.Assert(tree is Chunk);
            return HandleChunk(tree as Chunk);
        }

        class LocalNameInfo
        {
            // name register id
            public int register = 0;
            // name begin and end instruction
            public int begin_pc = 0;

            public LocalNameInfo(int register_, int begin_pc_)
            {
                register = register_;
                begin_pc = begin_pc_;
            }
        };
        enum JumpType
        {
            JumpHead,
            JumpTail,
        }
        class LoopJumpInfo
        {
            public JumpType jump_type;
            public int instruction_index;// Instruction need to fill Bx
            public LoopJumpInfo(JumpType jump_type_,int index_)
            {
                jump_type = jump_type_;
                instruction_index = index_;
            }
        }
        class GenerateLoop
        {
            public GenerateLoop parent;
            public int start_index;
            public List<LoopJumpInfo> jumps = new List<LoopJumpInfo>();
        }
        class GenerateBlock
        {
            public GenerateBlock parent;
            public Dictionary<string, LocalNameInfo> names = new Dictionary<string,LocalNameInfo>();
            public int start_register;
        }

        class GenerateFunction
        {
            public GenerateFunction parent;
            public GenerateBlock current_block;
            public GenerateLoop current_loop;
            public Function function;
            public int func_index = 0;// index in parent
            public int register = 0;// next free register index
        }

        GenerateFunction _current_func = null;

        void InsertName(string name, int register)
        {
            var f = GetCurrentFunction();
            int begin_pc = f.OpCodeSize();
            var names = _current_func.current_block.names;
            LocalNameInfo name_info = null;
            if (names.TryGetValue(name,out name_info))
            {
                // add old one to function
                f.AddLocalVar(name, name_info.register, name_info.begin_pc, begin_pc);
                // replace old one
                names[name] = new LocalNameInfo(register,begin_pc);
            }
            else
            {
                names.Add(name, new LocalNameInfo(register, begin_pc));
            }
        }
        int SearchNameAndScope(string name, out LexicalScope scope)
        {
            int index = SearchFunctionLocalName(_current_func, name);
            if (index >= 0)
            {
                scope = LexicalScope.Local;
                return index;
            }
            else
            {
                // is global
                scope = LexicalScope.Global;
                return _current_func.function.AddConstString(name);
            }
        }
        int SearchFunctionLocalName(GenerateFunction func,string name)
        {
            var block = func.current_block;
            while(block != null)
            {
                LocalNameInfo name_info = null;
                if (block.names.TryGetValue(name, out name_info))
                {
                    return name_info.register;
                }
                else
                    block = block.parent;
            }
            return -1;
        }

        void EnterFunction()
        {
            var func = new GenerateFunction();
            var parent = _current_func;
            func.parent = parent;
            func.function = new Function();
            _current_func = func;

            if(parent != null)
            {
                var index = parent.function.AddChildFunction(func.function);
                func.func_index = index;
                func.function.SetParent(parent.function);
            }
        }

        void LeaveFunction()
        {
            // auto gc, just forget about it
            _current_func = _current_func.parent;
        }
        Function GetCurrentFunction()
        {
            return _current_func.function;
        }
        int GetFunctionIndex()
        {
            return _current_func.func_index;
        }
        void EnterBlock()
        {
            var block = new GenerateBlock();
            block.start_register = GetNextRegisterId();
            block.parent = _current_func.current_block;
            _current_func.current_block = block;
        }

        void LeaveBlock()
        {
            var block = _current_func.current_block;
            // add all local variables info to function
            var f = _current_func.function;
            int end_pc = f.OpCodeSize();
            foreach(var item in block.names)
            {
                f.AddLocalVar(item.Key, item.Value.register,
                    item.Value.begin_pc, end_pc);
            }
            // shrink stack
            var code = Instruction.A(OpType.OpType_StackShrink, block.start_register);
            f.AddInstruction(code, -1);
            ResetRegisterId(block.start_register);

            // auto gc
            _current_func.current_block = block.parent;
        }
        void EnterLoop()
        {
            var loop = new GenerateLoop();
            loop.start_index = GetCurrentFunction().OpCodeSize();
            loop.parent = _current_func.current_loop;
            _current_func.current_loop = loop;
        }
        void AddLoopJumpInfo(JumpType jump_type_,int instruction_index_)
        {
            _current_func.current_loop.jumps.Add(new LoopJumpInfo(jump_type_, instruction_index_));
        }
        void LeaveLoop()
        {
            var f = GetCurrentFunction();
            var loop = _current_func.current_loop;
            int end_index = f.OpCodeSize();
            foreach(var jump in loop.jumps)
            {
                int diff = 0;
                if (jump.jump_type == JumpType.JumpHead)
                    diff = loop.start_index - jump.instruction_index;
                else if (jump.jump_type == JumpType.JumpTail)
                    diff = end_index - jump.instruction_index;

                f.FillInstructionBx(jump.instruction_index, diff);
            }

            // auto gc
            _current_func.current_loop = loop.parent;
        }
        int GetNextRegisterId()
        {
            return _current_func.register;
        }

        int ResetRegisterId(int register)
        {
            if (register >= OmsConf.MAX_FUNC_REGISTER)
            {
                Throw("to many local variables");
            }
            return _current_func.register = register;
        }
        int GenerateRegisterId()
        {
            if (_current_func.register + 1 >= OmsConf.MAX_FUNC_REGISTER)
            {
                Throw("to many local variables");
            }
            return _current_func.register++;
        }

        Function HandleChunk(Chunk tree)
        {
            EnterFunction();
            var f = GetCurrentFunction();
            EnterBlock();
            HandleBlock(tree.block);
            LeaveBlock();
            LeaveFunction();
            return f;
        }

        void HandleBlock(Block tree)
        {
            foreach(var stmt in tree.statements)
            {
                if (stmt is DoStatement)
                    HandleDoStatement(stmt as DoStatement);
                else if (stmt is WhileStatement)
                    HandleWhileStatement(stmt as WhileStatement);
                else if (stmt is IfStatement)
                    HandleIfStatement(stmt as IfStatement);
                else if (stmt is ForStatement)
                    HandleForStatement(stmt as ForStatement);
                else if (stmt is ForEachStatement)
                    HandleForEachStatement(stmt as ForEachStatement);
                else if (stmt is ForInStatement)
                    HandleForInStatement(stmt as ForInStatement);
                else if (stmt is FunctionStatement)
                    HandleFunctionStatement(stmt as FunctionStatement);
                else if (stmt is LocalFunctionStatement)
                    HandleLocalFunctionStatement(stmt as LocalFunctionStatement);
                else if (stmt is LocalNameListStatement)
                    HandleLocalNameListStatement(stmt as LocalNameListStatement);
                else if (stmt is ReturnStatement)
                    HandleReturnStatement(stmt as ReturnStatement);
                else if (stmt is BreakStatement)
                    HandleBreakStatement(stmt as BreakStatement);
                else if (stmt is ContinueStatement)
                    HandleContinueStatement(stmt as ContinueStatement);
                else if (stmt is AssignStatement)
                    HandleAssignStatement(stmt as AssignStatement);
                else
                    HandleExpRead(stmt);
            }
        }

        void HandleDoStatement(DoStatement tree)
        {
            EnterBlock();
            HandleBlock(tree.block);
            LeaveBlock();
        }
        void HandleWhileStatement(WhileStatement tree)
        {
            EnterLoop();
            EnterBlock();

            var register_id = GetNextRegisterId();
            HandleExpRead(tree.exp);

            // jump to loop tail when expression return false
            var f = GetCurrentFunction();
            var code = Instruction.ABx(OpType.OpType_JmpFalse, register_id, 0);
            int index = f.AddInstruction(code, -1);
            AddLoopJumpInfo(JumpType.JumpTail, index);

            HandleBlock(tree.block);

            LeaveBlock();
            // jump to loop head
            code = Instruction.Bx(OpType.OpType_Jmp, 0);
            index = f.AddInstruction(code, -1);
            AddLoopJumpInfo(JumpType.JumpHead, index);
            LeaveLoop();
        }
        void HandleForStatement(ForStatement tree)
        {
            EnterBlock();

            var f = GetCurrentFunction();
            Instruction code;

            // init name, limit, step
            HandleExpRead(tree.exp1);
            int var_register = GenerateRegisterId();

            HandleExpRead(tree.exp2);
            int limit_register = GenerateRegisterId();

            if(tree.exp3 == null)
            {
                // default step is 1
                code = Instruction.ABx(OpType.OpType_LoadInt, GetNextRegisterId(), 1);
                f.AddInstruction(code, -1);
            }
            else
            {
                HandleExpRead(tree.exp3);
            }
            int step_register = GenerateRegisterId();


            EnterLoop();
            {
                EnterBlock();

                // check 'for', continue loop or not
                code = Instruction.ABC(OpType.OpType_ForStep, var_register, limit_register, step_register);
                f.AddInstruction(code, -1);

                // next code jump to loop tail
                code = Instruction.Bx(OpType.OpType_Jmp, 0);
                int index = f.AddInstruction(code, -1);
                AddLoopJumpInfo(JumpType.JumpTail, index);

                var name_register = GenerateRegisterId();
                InsertName(tree.name.m_string, name_register);
                // name = var
                code = Instruction.AB(OpType.OpType_Move, name_register, var_register);
                f.AddInstruction(code, -1);

                // var += step
                code = Instruction.A(OpType.OpType_Add, var_register);
                f.AddInstruction(code, -1);

                HandleBlock(tree.block);

                LeaveBlock();

                // jump to loop head
                code = Instruction.Bx(OpType.OpType_Jmp, 0);
                index = f.AddInstruction(code, -1);
                AddLoopJumpInfo(JumpType.JumpHead, index);
            }
            LeaveLoop();

            LeaveBlock();
        }
        void HandleForEachStatement(ForEachStatement tree)
        {
            // todo think about it... has relate with hash implement
        }
        void HandleForInStatement(ForInStatement tree)
        {
            EnterBlock();

            var f = GetCurrentFunction();
            Instruction code;

            // init iterrate function
            HandleExpList(tree.exp_list, 3);
            var func_register = GenerateRegisterId();
            var state_register = GenerateRegisterId();
            var var_register = GenerateRegisterId();

            EnterLoop();
            {
                EnterBlock();

                // alloca registers for names
                int name_start = GetNextRegisterId();
                HandleNameList(tree.name_list);
                int name_end = GetNextRegisterId();

                // alloca temp registers for call iterate function
                int temp_func = name_start;
                int temp_state = name_start + 1;
                int temp_var = name_start + 2;

                // call iterate function
                Action<int, int> move = (int dst, int src) =>
                {
                    var l_code = Instruction.AB(OpType.OpType_Move, dst, src);
                    f.AddInstruction(l_code, -1);
                };
                move(temp_func, func_register);
                move(temp_state, state_register);
                move(temp_var, var_register);

                code = Instruction.ABC(OpType.OpType_Call, temp_func, 2, 0);
                f.AddInstruction(code, -1);

                code = Instruction.A(OpType.OpType_SetTop, name_end);
                f.AddInstruction(code, -1);

                // jump to loop tail when the first name value is nil
                code = Instruction.ABx(OpType.OpType_JmpNil, name_start, 0);
                int index = f.AddInstruction(code, -1);
                AddLoopJumpInfo(JumpType.JumpTail, index);

                // var = name1
                move(var_register, name_start);

                HandleBlock(tree.block);

                LeaveBlock();
                // jump to loop head
                code = Instruction.Bx(OpType.OpType_Jmp, 0);
                index = f.AddInstruction(code, -1);
                AddLoopJumpInfo(JumpType.JumpHead, index);
            }

            LeaveLoop();

            LeaveBlock();
        }
        void HandleIfStatement(IfStatement tree)
        {
            var f = GetCurrentFunction();
            Instruction code;
            int jmp_if_end_index = 0;
            {
                HandleExpRead(tree.exp);
                int register = GetNextRegisterId();
                code = Instruction.ABx(OpType.OpType_JmpFalse, register, 0);
                int jmp_false_index = f.AddInstruction(code, -1);

                EnterBlock();
                HandleBlock(tree.true_branch);
                LeaveBlock();

                // jump to if end
                code = Instruction.Bx(OpType.OpType_Jmp, 0);
                jmp_if_end_index = f.AddInstruction(code, -1);

                // jump to if false branch
                f.FillInstructionBx(jmp_false_index, f.OpCodeSize() - jmp_false_index);
            }
            if(tree.false_branch != null)
            {
                if (tree.false_branch is IfStatement)
                    HandleIfStatement(tree.false_branch as IfStatement);
                else
                {
                    Debug.Assert(tree.false_branch is Block);
                    EnterBlock();
                    HandleBlock(tree.false_branch as Block);
                    LeaveBlock();
                }
            }
            // jump to if end
            int index = f.OpCodeSize();
            f.FillInstructionBx(jmp_if_end_index, f.OpCodeSize() - jmp_if_end_index);
        }
        void HandleFunctionStatement(FunctionStatement tree)
        {
            HandleFunctionBody(tree.func_body);
            HandleFunctionName(tree.func_name);
        }
        void HandleParamList(ParamList tree)
        {
            var f = GetCurrentFunction();
            f.SetFixedArgCount(tree.name_list.Count);
            if (tree.is_var_arg)
                f.SetHasVarArg();

            for (int i = 0; i < tree.name_list.Count; ++i)
            {
                InsertName(tree.name_list[i].m_string, GenerateRegisterId());
            }
        }
        void HandleFunctionBody(FunctionBody tree)
        {
            EnterFunction();
            var func_index = GetFunctionIndex();
            {
                EnterBlock();
                HandleParamList(tree.param_list);
                HandleBlock(tree.block);
                LeaveBlock();
            }
            LeaveFunction();

            var f = GetCurrentFunction();
            var code = Instruction.ABx(OpType.OpType_LoadFunc, GetNextRegisterId(), func_index);
            f.AddInstruction(code, -1);
        }
        void HandleFunctionName(FunctionName tree)
        {
            int func_register = GenerateRegisterId();
            var f = GetCurrentFunction();
            Instruction code = new Instruction();

            var first_name = tree.names[0];
            LexicalScope scope;
            int index = SearchNameAndScope(first_name.m_string, out scope);
            if (tree.names.Count == 1)
            {
                if (scope == LexicalScope.Global)
                    code = Instruction.ABx(OpType.OpType_SetGlobal, func_register, index);
                else if (scope == LexicalScope.Local)
                    code = Instruction.AB(OpType.OpType_Move, index, func_register);
                f.AddInstruction(code, -1);
            }
            else
            {
                var table_register = GenerateRegisterId();
                int key_register = GenerateRegisterId();

                if (scope == LexicalScope.Global)
                    code = Instruction.ABx(OpType.OpType_GetGlobal, table_register, index);
                else if (scope == LexicalScope.Local)
                    code = Instruction.AB(OpType.OpType_Move, table_register, index);
                f.AddInstruction(code, -1);

                Action<Token> load_key = (Token name)=>{
                    int l_index = f.AddConstString(name.m_string);
                    var l_code = Instruction.ABx(OpType.OpType_LoadConst, key_register, l_index);
                    f.AddInstruction(l_code, -1);
                };

                for (int i = 1; i < tree.names.Count - 1; ++i)
                {
                    load_key(tree.names[i]);
                    code = Instruction.ABC(OpType.OpType_GetTable,
                        table_register, key_register, table_register);
                    f.AddInstruction(code, -1);
                }
                load_key(tree.names.Last<Token>());
                code = Instruction.ABC(OpType.OpType_SetTable,
                    table_register,key_register,func_register);
                f.AddInstruction(code, -1);
            }
            // 回收寄存器
            ResetRegisterId(func_register);
        }
        void HandleReturnStatement(ReturnStatement tree)
        {
            int register_id = GetNextRegisterId();
            int value_count = 0;
            int is_any_value = 0;
            if(tree.exp_list != null)
            {
                is_any_value = tree.exp_list.return_any_value ? 1 : 0;
                value_count = tree.exp_list.exp_list.Count;
                HandleExpList(tree.exp_list, -1);
            }
            var f = GetCurrentFunction();
            var code = Instruction.ABC(OpType.OpType_Ret, register_id, value_count, is_any_value);
            f.AddInstruction(code, -1);
        }
        void HandleBreakStatement(BreakStatement tree)
        {
            // jump to loop tail
            var code = Instruction.Bx(OpType.OpType_Jmp, 0);
            int index = GetCurrentFunction().AddInstruction(code, -1);
            AddLoopJumpInfo(JumpType.JumpTail, index);
        }
        void HandleContinueStatement(ContinueStatement tree)
        {
            // jump to loop head
            var code = Instruction.Bx(OpType.OpType_Jmp, 0);
            int index = GetCurrentFunction().AddInstruction(code, -1);
            AddLoopJumpInfo(JumpType.JumpHead, index);
        }
        void HandleBinaryExp(BinaryExpression tree)
        {
            var start_register = GetNextRegisterId();
            var f = GetCurrentFunction();
            Instruction code;
            OpType op_type = OpType.OpType_InValid;
            var token_type = tree.op.m_type;
            if(token_type == (int)TokenType.AND || token_type == (int)TokenType.OR)
            {
                HandleExpRead(tree.left);

                // do not run right exp when then result or left exp satisfy operator
                op_type = (token_type == (int)TokenType.AND) ?
                    OpType.OpType_JmpFalse : OpType.OpType_JmpTrue;
                code = Instruction.ABx(op_type, GetNextRegisterId(), 0);
                int index = f.AddInstruction(code, -1);

                HandleExpRead(tree.right);

                // jump to skip right exp
                f.FillInstructionBx(index, f.OpCodeSize() - index);
                return;
            }
            HandleExpRead(tree.left);
            var left_register = GenerateRegisterId();
            HandleExpRead(tree.right);
            var right_register = GenerateRegisterId();

            switch (tree.op.m_type)
            {
                case '+': op_type = OpType.OpType_Add; break;
                case '-': op_type = OpType.OpType_Sub; break;
                case '*': op_type = OpType.OpType_Mul; break;
                case '/': op_type = OpType.OpType_Div; break;
                case '^': op_type = OpType.OpType_Pow; break;
                case '%': op_type = OpType.OpType_Mod; break;
                case '<': op_type = OpType.OpType_Less; break;
                case '>': op_type = OpType.OpType_Greater; break;
                case (int)TokenType.CONCAT: op_type = OpType.OpType_Concat; break;
                case (int)TokenType.EQ: op_type = OpType.OpType_Equal; break;
                case (int)TokenType.NE: op_type = OpType.OpType_UnEqual; break;
                case (int)TokenType.LE: op_type = OpType.OpType_LessEqual; break;
                case (int)TokenType.GE: op_type = OpType.OpType_GreaterEqual; break;
                default: Debug.Assert(false); break;
            }

            code = Instruction.ABC(op_type, left_register, left_register, right_register);
            f.AddInstruction(code, -1);

            ResetRegisterId(start_register);
        }
        void HandleUnaryExp(UnaryExpression tree)
        {
            HandleExpRead(tree.exp);
            var register = GetNextRegisterId();
            OpType op_type = OpType.OpType_InValid;
            switch(tree.op.m_type)
            {
                case '-': op_type = OpType.OpType_Neg; break;
                case '#': op_type = OpType.OpType_Len; break;
                case (int)TokenType.NOT: op_type = OpType.OpType_Not; break;
                default: Debug.Assert(false); break;
            }

            var f = GetCurrentFunction();
            var code = Instruction.A(op_type, register);
            f.AddInstruction(code, -1);
        }
        void HandleFuncCall(FuncCall tree)
        {
            var f = GetCurrentFunction();
            Instruction code;
            HandleExpRead(tree.caller);
            var caller_register = GenerateRegisterId();
            int arg_count = 0;
            if(tree.member_name != null)
            {
                arg_count++;
                // first_arg = caller_table
                var arg_register = GenerateRegisterId();
                code = Instruction.AB(OpType.OpType_Move, arg_register, caller_register);
                f.AddInstruction(code, -1);

                // caller = caller_table[member_name]
                int key_register = arg_register + 1;
                int index = f.AddConstString(tree.member_name.m_string);
                code = Instruction.ABx(OpType.OpType_LoadConst, key_register, index);
                f.AddInstruction(code, -1);
                code = Instruction.ABC(OpType.OpType_GetTable, caller_register, key_register, caller_register);
                f.AddInstruction(code, -1);
            }
            int is_any_arg = 0;
            if(tree.args != null)
            {
                arg_count += tree.args.exp_list.Count;
                is_any_arg = tree.args.return_any_value ? 1 : 0;
                HandleExpList(tree.args, -1);
            }

            // call function
            code = Instruction.ABC(OpType.OpType_Call, caller_register,arg_count,is_any_arg);
            f.AddInstruction(code, -1);

            ResetRegisterId(caller_register);
        }
        void HandleExpRead(SyntaxTree tree)
        {
            if (tree is Terminator)
            {
                var f = GetCurrentFunction();
                var value_register = GetNextRegisterId();
                var term = tree as Terminator;
                int token_type = term.token.m_type;
                Instruction code = new Instruction();
                if (token_type == (int)TokenType.NAME)
                {
                    LexicalScope scope;
                    int index = SearchNameAndScope(term.token.m_string, out scope);
                    if (scope == LexicalScope.Global)
                        code = Instruction.ABx(OpType.OpType_GetGlobal, value_register, index);
                    else if (scope == LexicalScope.Local)
                        code = Instruction.AB(OpType.OpType_Move, value_register, index);

                }
                else if (token_type == (int)TokenType.NIL)
                    code = Instruction.A(OpType.OpType_LoadNil, value_register);
                else if (token_type == (int)TokenType.TRUE)
                    code = Instruction.ABx(OpType.OpType_LoadBool, value_register, 1);
                else if (token_type == (int)TokenType.FALSE)
                    code = Instruction.ABx(OpType.OpType_LoadBool, value_register, 0);
                else if (token_type == (int)TokenType.DOTS)
                    code = Instruction.A(OpType.OpType_VarArg, value_register);
                else if (token_type == (int)TokenType.NUMBER)
                {
                    var index = f.AddConstNumber(term.token.m_number);
                    code = Instruction.ABx(OpType.OpType_LoadConst, value_register, index);
                }
                else if (token_type == (int)TokenType.STRING)
                {
                    var index = f.AddConstString(term.token.m_string);
                    code = Instruction.ABx(OpType.OpType_LoadConst, value_register, index);
                }
                else
                {
                    Debug.Assert(false);
                }
                f.AddInstruction(code, -1);
            }
            else if (tree is TableAccess)
            {
                var f = GetCurrentFunction();
                var table_access = tree as TableAccess;
                HandleExpRead(table_access.table);
                var table_register = GenerateRegisterId();
                HandleExpRead(table_access.index);
                var key_register = GenerateRegisterId();
                var code = Instruction.ABC(OpType.OpType_GetTable,
                    table_register, key_register, table_register);
                f.AddInstruction(code, -1);

                ResetRegisterId(table_register);
            }
            else if(tree is FuncCall)
            {
                HandleFuncCall(tree as FuncCall);
            }
            else if (tree is TableDefine)
            {
                HandleTableDefine(tree as TableDefine);
            }
            else if (tree is FunctionBody)
            {
                HandleFunctionBody(tree as FunctionBody);
            }
            else if(tree is BinaryExpression)
            {
                HandleBinaryExp(tree as BinaryExpression);
            }
            else if(tree is UnaryExpression)
            {
                HandleUnaryExp(tree as UnaryExpression);
            }
            else
            {
                Debug.Assert(false);
            }
        }
        void HandleVarWrite(SyntaxTree tree, int value_register)
        {
            var f = GetCurrentFunction();
            if(tree is Terminator)
            {
                var term = tree as Terminator;
                Debug.Assert(term.token.m_type == (int)TokenType.NAME);
                LexicalScope scope;
                int index = SearchNameAndScope(term.token.m_string, out scope);
                Instruction code = new Instruction();
                if (scope == LexicalScope.Global)
                    code = Instruction.ABx(OpType.OpType_SetGlobal, value_register, index);
                else if (scope == LexicalScope.Local)
                    code = Instruction.AB(OpType.OpType_Move, index, value_register);
                f.AddInstruction(code, -1);
            }
            else if(tree is TableAccess)
            {
                var index_access = tree as TableAccess;
                HandleExpRead(index_access.table);
                var table_register = GenerateRegisterId();
                HandleExpRead(index_access.index);
                var key_register = GenerateRegisterId();
                var code = Instruction.ABC(OpType.OpType_SetTable,
                    table_register, key_register, value_register);
                f.AddInstruction(code, -1);

                ResetRegisterId(table_register);
            }
            else
            {
                Debug.Assert(false);
            }
        }
        void HandleTableDefine(TableDefine tree)
        {
            var f = GetCurrentFunction();
            Instruction code;
            // new table
            int table_register = GenerateRegisterId();
            code = Instruction.A(OpType.OpType_NewTable, table_register);
            f.AddInstruction(code, -1);

            // add field
            int count = tree.last_field_append_table ? tree.fields.Count - 1 : tree.fields.Count;
            int arr_index = 1;//number index start from 1
            int key_register = table_register + 1;
            int value_register = table_register + 2;
            for(int i = 0; i < count; ++i)
            {
                var field = tree.fields[i];
                if(field.index == null)
                {
                    code = Instruction.ABx(OpType.OpType_LoadInt, key_register, arr_index++);
                    f.AddInstruction(code, -1);
                }
                else
                {
                    HandleExpRead(field.index);
                    GenerateRegisterId();
                }
                HandleExpRead(field.value);

                code = Instruction.ABC(OpType.OpType_SetTable, table_register, key_register, value_register);
                f.AddInstruction(code, -1);
                ResetRegisterId(key_register);
            }
            // last field
            if(tree.last_field_append_table)
            {
                var last_field = tree.fields[count];
                HandleExpRead(last_field.value);
                code = Instruction.AB(OpType.OpType_AppendTable, table_register, key_register);
                f.AddInstruction(code, -1);
            }

            ResetRegisterId(table_register);
        }
        void HandleExpList(ExpressionList tree, int expect_value_count)
        {
            int start_register = GetNextRegisterId();
            for(int i = 0; i < tree.exp_list.Count; ++i)
            {
                HandleExpRead(tree.exp_list[i]);
                GenerateRegisterId();
            }
            // ajust value count
            if (expect_value_count != -1)
            {
                int fix_value_count = tree.exp_list.Count;
                if (fix_value_count < expect_value_count)
                {
                    // need fill nil when need
                    if(tree.return_any_value)
                    {
                        var f = GetCurrentFunction();
                        var code = Instruction.A(OpType.OpType_SetTop, start_register + expect_value_count);
                        f.AddInstruction(code, -1);
                    }
                    else
                    {
                        FillNil(start_register + fix_value_count, start_register + expect_value_count, -1);
                    }
                }
            }
            ResetRegisterId(start_register);
        }

        void HandleNameList(NameList tree)
        {
            for(int i = 0; i < tree.names.Count; ++i)
            {
                InsertName(tree.names[i].m_string, GenerateRegisterId());
            }
        }

        void HandleAssignStatement(AssignStatement tree)
        {
            HandleExpList(tree.exp_list, tree.var_list.Count);
            // var list
            int register = GetNextRegisterId();
            ResetRegisterId(register + tree.var_list.Count);
            for(int i = 0; i < tree.var_list.Count; ++i)
            {
                HandleVarWrite(tree.var_list[i], register + i);
            }
            ResetRegisterId(register);
        }
        void HandleLocalNameListStatement(LocalNameListStatement tree)
        {
            if(tree.exp_list != null)
            {
                HandleExpList(tree.exp_list, tree.name_list.names.Count);
            }
            else
            {
                FillNil(GetNextRegisterId(), GetNextRegisterId() + tree.name_list.names.Count, -1);
            }
            HandleNameList(tree.name_list);
        }
        void HandleLocalFunctionStatement(LocalFunctionStatement tree)
        {
            InsertName(tree.name.m_string, GetNextRegisterId());
            HandleFunctionBody(tree.func_body);
            GenerateRegisterId();
        }

        void FillNil(int start_register, int end_register, int line)
        {
            var f = GetCurrentFunction();
            while(start_register < end_register)
            {
                var code = Instruction.A(OpType.OpType_LoadNil, start_register++);
                f.AddInstruction(code, line);
            }
        }
        void Throw(string msg)
        {
            throw new CodeGenerateException(msg);
        }
    }
}
