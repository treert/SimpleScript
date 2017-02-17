using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{
    /// <summary>
    /// 生成函数Funtion
    /// </summary>
    class CodeGenerate
    {
        class LocalNameInfo
        {
            // Name register id
            public int register_id = 0;
            // Name begin instruction
            public int begin_pc = 0;

            public LocalNameInfo(int register_id_ = 0, int begin_pc_ = 0)
            {
                register_id = register_id_;
                begin_pc = begin_pc_;
            }
        };

        int SearchLocalName(string name)
        {
            return 0;
        }

        void Reset()
        {

        }
        void EnterFunction()
        {

        }
        void LeaveFunction()
        {

        }
        Function GetCurrentFunction()
        {
            return null;
        }
        int GetChildFunctionIndex()
        {
            return 0;
        }
        void EnterBlock()
        {

        }
        void LeaveBlock()
        {

        }
        void EnterLoop(SyntaxTree loop)
        {
            return;
        }
        void LeaveLoop()
        {

        }
        SyntaxTree GetCurrentLoop()
        {
            return null;
        }
        int GetNextRegisterId()
        {
            return 0;
        }
        int ResetRegisterId(int register)
        {
            return 0;
        }
        int GenerateRegisterId()
        {
            return 0;
        }
        void InsertName(string name, int register)
        {

        }
        int PrepareUpvalue(string name)
        {
            return 0;
        }
        void HandleChunk(Chunk tree)
        {
            EnterFunction();
            {
                var f = GetCurrentFunction();
                EnterBlock();
                HandleBlock(tree.block);
                LeaveBlock();
            }
            LeaveFunction();
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
            EnterBlock();
            EnterLoop(tree);

            var register_id = GetNextRegisterId();
            HandleExpRead(tree.exp);

            // jump to loop tail when expression return false
            var f = GetCurrentFunction();
            var code = Instruction.AsBx(OpType.OpType_JmpFalse, register_id, 0);
            int index = f.AddInstruction(code, -1);
            // todo jump to tail

            HandleBlock(tree.block);

            // todo jump to head

            LeaveLoop();
            LeaveBlock();
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
                code = Instruction.AsBx(OpType.OpType_LoadInt, GetNextRegisterId(), 1);
                f.AddInstruction(code, -1);
            }
            else
            {
                HandleExpRead(tree.exp3);
            }
            int step_register = GenerateRegisterId();


            EnterLoop(tree);
            {
                EnterBlock();

                // check 'for', continue loop or not
                code = Instruction.A(OpType.OpType_ForStep, var_register);
                f.AddInstruction(code, -1);

                // todo next code jump to loop tail

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
            }
            // todo jump to loop head
            LeaveLoop();

            LeaveBlock();
        }
        void HandleForEachStatement(ForEachStatement tree)
        {
            EnterBlock();

            var f = GetCurrentFunction();
            Instruction code;

            // init table
            HandleExpRead(tree.exp);
            var table_register = GenerateRegisterId();

            EnterLoop(tree);
            {
                EnterBlock();

                // todo think about it...

                // alloca registers for k,v
                int v_register = GenerateRegisterId();
                int k_register = GenerateRegisterId();
                InsertName(tree.v.m_string, v_register);
                if (tree.k != null)
                    InsertName(tree.k.m_string, k_register);

                int temp_table = v_register;

                // call iterate function
                Action<int, int> move = (int dst, int src) =>
                {
                    var l_code = Instruction.AB(OpType.OpType_Move, dst, src);
                    f.AddInstruction(l_code, -1);
                };
                move(temp_table, table_register);

                code = Instruction.A(OpType.OpType_TableNext, temp_table);
                f.AddInstruction(code, -1);

                // break the loop when the first name value is nil
                code = Instruction.AsBx(OpType.OpType_JmpNil, v_register, 0);
                f.AddInstruction(code, -1);
                // todo jump to loop tail

                HandleBlock(tree.block);

                LeaveBlock();
            }
            // todo jump to loop head
            LeaveLoop();

            LeaveBlock();
        }
        void HandleForInStatement(ForInStatement tree)
        {
            EnterBlock();

            var f = GetCurrentFunction();
            Instruction code;

            // init iterrate function
            HandleExpList(tree.exp_list);
            var func_register = GenerateRegisterId();
            var state_register = GenerateRegisterId();
            var var_register = GenerateRegisterId();

            EnterLoop(tree);
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

                code = Instruction.A(OpType.OpType_Call, temp_func);
                f.AddInstruction(code, -1);

                code = Instruction.A(OpType.OpType_SetTop, name_end);
                f.AddInstruction(code, -1);

                // break the loop when the first name value is nil
                code = Instruction.AsBx(OpType.OpType_JmpNil, name_start, 0);
                f.AddInstruction(code, -1);
                // todo jump to loop tail

                // var = name1
                move(var_register, name_start);

                HandleBlock(tree.block);

                LeaveBlock();
            }
            // todo jump to loop head
            LeaveLoop();

            LeaveBlock();
        }
        void HandleIfStatement(IfStatement tree)
        {
            var f = GetCurrentFunction();
            Instruction code;
            {
                HandleExpRead(tree.exp);
                int register = GetNextRegisterId();
                code = Instruction.AsBx(OpType.OpType_JmpFalse, register, 0);
                // todo jump to false branch
                EnterBlock();
                HandleBlock(tree.true_branch);
                LeaveBlock();

                // todo jump to if end
                code = Instruction.SBx(OpType.OpType_Jmp, 0);

                int index = f.OpCodeSize();
                // todo jump to false branch
            }
            if(tree.false_branch != null)
            {
                if (tree.false_branch is IfStatement)
                    HandleIfStatement(tree.false_branch as IfStatement);
                else
                {
                    EnterBlock();
                    HandleBlock(tree.false_branch as Block);
                    LeaveBlock();
                }
            }
            // todo jump to if end
        }
        void HandleFunctionStatement(FunctionStatement tree)
        {
            HandleFunctionBody(tree.func_body);
            HandleFunctionName(tree.func_name);
        }
        void HandleParamList(ParamList tree)
        {
            var f = GetCurrentFunction();
            // todo ajust any args
            // todo increse function fix args count

            int register = GetNextRegisterId();
            for (int i = 0; i < tree.name_list.Count; ++i)
            {
                InsertName(tree.name_list[i].m_string, register + i);
            }
        }
        void HandleFunctionBody(FunctionBody tree)
        {
            EnterFunction();
            var f = GetCurrentFunction();
            var child_index = GetChildFunctionIndex();
            {
                EnterBlock();
                if(tree.has_self)
                {
                    var self_register = GenerateRegisterId();
                    InsertName("self", self_register);

                    //todo add fixed arg count by 1
                }
                if (tree.param_list != null)
                    HandleParamList(tree.param_list);
                HandleBlock(tree.block);
                LeaveBlock();
            }
            LeaveFunction();

            var code = Instruction.AsBx(OpType.OpType_Closure, GetNextRegisterId(), child_index);
            f.AddInstruction(code, -1);
        }
        void HandleFunctionName(FunctionName tree)
        {
            int func_register = GenerateRegisterId();
            var f = GetCurrentFunction();
            Instruction code = new Instruction();

            bool has_member = tree.names.Count > 1;
            var first_name = tree.names[0];
            if(!has_member)
            {
                if (tree.scope == LexicalScope.Global)
                {
                    int index = f.AddConstString(first_name.m_string);
                    code = Instruction.AsBx(OpType.OpType_SetGlobal, func_register, index);
                }
                else if(tree.scope == LexicalScope.Upvalue)
                {
                    int index = PrepareUpvalue(first_name.m_string);
                    code = Instruction.AB(OpType.OpType_SetUpvalue, func_register, index);
                }
                else if (tree.scope == LexicalScope.Local)
                {
                    int index = SearchLocalName(first_name.m_string);
                    code = Instruction.AB(OpType.OpType_Move, index, func_register);
                }
                f.AddInstruction(code, -1);
            }
            else
            {
                var table_register = GenerateRegisterId();
                int key_register = GenerateRegisterId();

                if (tree.scope == LexicalScope.Global)
                {
                    int index = f.AddConstString(first_name.m_string);
                    code = Instruction.AsBx(OpType.OpType_GetGlobal, table_register, index);
                }
                else if (tree.scope == LexicalScope.Upvalue)
                {
                    int index = PrepareUpvalue(first_name.m_string);
                    code = Instruction.AB(OpType.OpType_GetUpvalue, table_register, index);
                }
                else if (tree.scope == LexicalScope.Local)
                {
                    int index = SearchLocalName(first_name.m_string);
                    code = Instruction.AB(OpType.OpType_Move, table_register, index);
                }
                f.AddInstruction(code, -1);

                Action<Token> load_key = (Token name)=>{
                    int index = f.AddConstString(name.m_string);
                    var l_code = Instruction.AsBx(OpType.OpType_LoadConst, key_register, index);
                    f.AddInstruction(l_code, -1);
                };

                for (int i = 1; i < tree.names.Count - 1; ++i)
                {
                    load_key(tree.names[i]);
                    code = Instruction.A(OpType.OpType_GetTable, table_register);
                    f.AddInstruction(code, -1);
                }
                load_key(tree.names.Last<Token>());
                code = Instruction.ABC(OpType.OpType_SetTable, table_register,key_register,func_register);
                f.AddInstruction(code, -1);
            }

        }
        void HandleReturnStatement(ReturnStatement tree)
        {
            int register_id = GetNextRegisterId();
            int value_count = 0;
            if(tree.exp_list != null)
            {
                value_count = tree.exp_list.return_value_count;
                HandleExpList(tree.exp_list);
            }
            var f = GetCurrentFunction();
            var instruction = Instruction.AsBx(OpType.OpType_Ret, register_id, value_count);
            f.AddInstruction(instruction,-1);
        }
        void HandleBreakStatement(BreakStatement tree)
        {
            // todo jump loop
        }
        void HandleContinueStatement(ContinueStatement tree)
        {
            // todo jump loop
        }
        void HandleBinaryExp(BinaryExpression tree)
        {
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
                code = Instruction.AsBx(op_type, GetNextRegisterId(), 0);
                int index = f.AddInstruction(code, -1);

                HandleExpRead(tree.right);

                // todo fill jmp code
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
            if(tree.member_name != null)
            {
                // set table as first arg
                var arg_register = GenerateRegisterId();
                code = Instruction.AB(OpType.OpType_Move, arg_register, caller_register);
                f.AddInstruction(code, -1);

                // get caller from table
                int key_register = arg_register + 1;
                int index = f.AddConstString(tree.member_name.m_string);
                code = Instruction.AsBx(OpType.OpType_LoadConst, key_register, index);
                f.AddInstruction(code, -1);
                code = Instruction.ABC(OpType.OpType_GetTable, caller_register, key_register, caller_register);
                f.AddInstruction(code, -1);
            }
            HandleExpList(tree.args);

            // todo call arg count need fix
            code = Instruction.A(OpType.OpType_Call, caller_register);
            f.AddInstruction(code, -1);
        }
        void HandleExpRead(SyntaxTree tree)
        {
            if (tree is Terminator)
            {
                var f = GetCurrentFunction();
                var value_register = GetNextRegisterId();
                var term = tree as Terminator;
                Debug.Assert(term.token.m_type == (int)TokenType.NAME);
                if (term.scope == LexicalScope.Global)
                {
                    var index = f.AddConstString(term.token.m_string);
                    var code = Instruction.AsBx(OpType.OpType_GetGlobal, value_register, index);
                    f.AddInstruction(code, -1);
                }
                else if (term.scope == LexicalScope.Local)
                {
                    var index = SearchLocalName(term.token.m_string);
                    var code = Instruction.AB(OpType.OpType_Move, value_register, index);
                }
                else
                {
                    Debug.Assert(term.scope == LexicalScope.Upvalue);
                    var index = PrepareUpvalue(term.token.m_string);
                    var code = Instruction.AB(OpType.OpType_GetUpvalue, value_register, index);
                }
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
            }
            else if(tree is FuncCall)
            {
                HandleFuncCall(tree as FuncCall);
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
                if(term.scope == LexicalScope.Global)
                {
                    var index = f.AddConstString(term.token.m_string);
                    var code = Instruction.AsBx(OpType.OpType_SetGlobal, value_register, index);
                    f.AddInstruction(code, -1);
                }
                else if(term.scope == LexicalScope.Local)
                {
                    var index = SearchLocalName(term.token.m_string);
                    var code = Instruction.AB(OpType.OpType_Move, index, value_register);
                }
                else
                {
                    Debug.Assert(term.scope == LexicalScope.Upvalue);
                    var index = PrepareUpvalue(term.token.m_string);
                    var code = Instruction.AB(OpType.OpType_SetUpvalue, value_register, index);
                }
            }
            else if(tree is TableAccess)
            {
                var index_access = tree as TableAccess;
                HandleExpRead(index_access.index);
                var key_register = GenerateRegisterId();
                HandleExpRead(index_access.table);
                var table_register = GenerateRegisterId();
                var code = Instruction.ABC(OpType.OpType_SetTable,
                    table_register, key_register, value_register);
                f.AddInstruction(code, -1);
            }
            else
            {
                Debug.Assert(false);
            }
        }
        void HandleExpList(ExpressionList tree)
        {
            int start_register = GetNextRegisterId();
            for(int i = 0; i < tree.exp_list.Count; ++i)
            {
                HandleExpRead(tree.exp_list[i]);
                GenerateRegisterId();
            }
            // todo ajust value count
            if(tree.expect_value_count != -1)
            {

            }
        }

        void HandleNameList(NameList tree)
        {
            int register = GetNextRegisterId();
            for(int i = 0; i < tree.names.Count; ++i)
            {
                InsertName(tree.names[i].m_string, register + i);
            }
        }

        void HandleAssignStatement(AssignStatement tree)
        {
            HandleExpList(tree.exp_list);
            // var list
            int register = GetNextRegisterId();
            ResetRegisterId(register + tree.var_list.Count);
            for(int i = 0; i < tree.var_list.Count; ++i)
            {
                HandleVarWrite(tree.var_list[i], register + i);
            }
        }
        void HandleLocalNameListStatement(LocalNameListStatement tree)
        {
            if(tree.exp_list != null)
            {
                HandleExpList(tree.exp_list);
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
