using System;
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
                    HandleOtherStatement(stmt);
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
            HandleExp(tree.exp);

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
            HandleExp(tree.exp1);
            int var_register = GenerateRegisterId();

            HandleExp(tree.exp2);
            int limit_register = GenerateRegisterId();

            if(tree.exp3 == null)
            {
                // default step is 1
                code = Instruction.AsBx(OpType.OpType_LoadInt, GetNextRegisterId(), 1);
                f.AddInstruction(code, -1);
            }
            else
            {
                HandleExp(tree.exp3);
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
            HandleExp(tree.exp);
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
                HandleExp(tree.exp);
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
        void HandleFunctionBody(FunctionBody tree)
        {

        }
        void HandleFunctionName(FunctionName tree)
        {
            int func_register = GenerateRegisterId();
            var f = GetCurrentFunction();
            Instruction code = new Instruction();

            bool has_member = tree.names.Count > 1 || tree.member_name != null;
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
                HandleExpressList(tree.exp_list);
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
        void HandleOtherStatement(SyntaxTree tree)
        {

        }
        void HandleExp(SyntaxTree tree)
        {

        }
        void HandleExpList(ExpressionList tree)
        {

        }

        void HandleExpressList(ExpressionList tree)
        {
            throw new NotImplementedException();
        }
        void HandleNameList(NameList tree)
        {
            for(int i = 0; i < tree.names.Count; ++i)
            {
                var register = GenerateRegisterId();
                InsertName(tree.names[i].m_string, register);
            }
        }

        void HandleAssignStatement(AssignStatement tree)
        {
	        throw new NotImplementedException();
        }
        void HandleLocalNameListStatement(LocalNameListStatement tree)
        {
            throw new NotImplementedException();
        }
        void HandleLocalFunctionStatement(LocalFunctionStatement tree)
        {
            InsertName(tree.name.m_string, GetNextRegisterId());
            HandleFunctionBody(tree.func_body);
        }
    }
}
