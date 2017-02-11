using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{

    /// <summary>
    /// 语义分析
    /// 1. 确定作用域
    /// 2. 确定break,continue对应的循环
    ///
    /// </summary>
    class Semantic:Visitor
    {
        void SetVarToWrite(SyntaxTree var_)
        {
            // only three type support write
            if (var_ is Terminator)
                (var_ as Terminator).is_read = false;
            else if (var_ is IndexAccessor)
                (var_ as IndexAccessor).is_read = false;
            else if (var_ is MemberAccessor)
                (var_ as MemberAccessor).is_read = false;
            
        }
        void EnterFunction()
        {

        }
        void LeaveFunction()
        {

        }
        bool HasVararg()
        {
            return true;
        }
        void EnterBlock()
        {

        }
        void LeaveBlock()
        {

        }
        void InsertName(string name_)
        {

        }

        LexicalScope SearchName(string name_)
        {
            return LexicalScope.UnKown;
        }

        SyntaxTree _cur_loop = null;
        void SetLoopAST(SyntaxTree loop)
        {
            _cur_loop = loop;
        }
        SyntaxTree GetLoopAST()
        {
            return _cur_loop;
        }
        void Throw(string msg)
        {
            throw new SematicException(msg);
        }
        public object Visit(Chunk tree, object data = null)
        {
            EnterFunction();
            tree.block.Accept(this);
            LeaveFunction();
            return null;
        }

        public object Visit(Block tree, object data = null)
        {
            EnterBlock();
            foreach (var stmt in tree.statements)
                stmt.Accept(this);
            LeaveBlock();
            return null;
        }

        public object Visit(ReturnStatement tree, object data = null)
        {
            if(tree.exp_list != null)
            {
                tree.exp_list.Accept(this);
            }
            return null;
        }

        public object Visit(BreakStatement tree, object data = null)
        {
            tree.loop = GetLoopAST();
            if (tree.loop == null)
                Throw("not in any loop");
            return null;
        }

        public object Visit(ContinueStatement tree, object data = null)
        {
            tree.loop = GetLoopAST();
            if (tree.loop == null)
                Throw("not in any loop");
            return null;
        }

        public object Visit(DoStatement tree, object data = null)
        {
            tree.block.Accept(this);
            return null;
        }

        public object Visit(WhileStatement tree, object data = null)
        {
            var old_loop = GetLoopAST();
            SetLoopAST(tree);
            tree.exp.Accept(this);
            tree.block.Accept(this);
            SetLoopAST(old_loop);
            return null;
        }

        public object Visit(IfStatement tree, object data = null)
        {
            tree.exp.Accept(this);
            tree.true_branch.Accept(this);
            if (tree.false_branch != null)
                tree.false_branch.Accept(this);
            return null;
        }

        public object Visit(ForStatement tree, object data = null)
        {
            var old_loop = GetLoopAST();
            SetLoopAST(tree);
            tree.exp1.Accept(this);
            tree.exp2.Accept(this);
            if (tree.exp3 != null)
                tree.exp3.Accept(this);
            {
                EnterBlock();
                InsertName(tree.name.m_string);
                tree.block.Accept(this);
                LeaveBlock();
            }
            SetLoopAST(old_loop);
            return null;
        }

        public object Visit(ForInStatement tree, object data = null)
        {
            var old_loop = GetLoopAST();
            SetLoopAST(tree);
            tree.exp_list.Accept(this);
            {
                EnterBlock();
                tree.name_list.Accept(this);
                tree.block.Accept(this);
                LeaveBlock();
            }
            SetLoopAST(old_loop);
            return null;
        }

        public object Visit(ForEachStatement tree, object data = null)
        {
            var old_loop = GetLoopAST();
            SetLoopAST(tree);
            tree.exp.Accept(this);
            {
                EnterBlock();
                if (tree.k != null)
                    InsertName(tree.k.m_string);
                InsertName(tree.v.m_string);
                tree.block.Accept(this);
                LeaveBlock();
            }
            SetLoopAST(old_loop);
            return null;
        }

        public object Visit(FunctionStatement tree, object data = null)
        {
            tree.func_name.Accept(this);
            if(tree.func_name.member_name != null)
            {
                tree.func_body.has_self = true;
            }
            tree.func_body.Accept(this);
            return null;
        }

        public object Visit(FunctionName tree, object data = null)
        {
            tree.scope = SearchName(tree.names[0].m_string);
            return null;
        }

        public object Visit(LocalFunctionStatement tree, object data = null)
        {
            InsertName(tree.name.m_string);
            tree.func_body.Accept(this);
            return null;
        }

        public object Visit(LocalNameListStatement tree, object data = null)
        {
            if (tree.exp_list != null)
            {
                tree.exp_list.Accept(this);
            }
            foreach (var name in tree.name_list.names)
            {
                InsertName(name.m_string);
            }
            return null;
        }

        public object Visit(AssignStatement tree, object data = null)
        {
            foreach(var var_ in tree.var_list)
            {
                SetVarToWrite(var_);
            }
            return null;
        }


        public object Visit(Terminator tree, object data = null)
        {
            if (tree.token.m_type == (int)TokenType.NAME)
                tree.scope = SearchName(tree.token.m_string);
            if (tree.token.m_type == (int)TokenType.DOTS && !HasVararg())
                Throw("function has no '...' param");

            return null;
        }

        public object Visit(BinaryExpression tree, object data = null)
        {

            return null;
        }

        public object Visit(UnaryExpression tree, object data = null)
        {
            return null;
        }

        public object Visit(FunctionBody tree, object data = null)
        {
            return null;
        }

        public object Visit(ParamList tree, object data = null)
        {
            return null;
        }

        public object Visit(TableDefine tree, object data = null)
        {
            return null;
        }

        public object Visit(TableIndexField tree, object data = null)
        {
            return null;
        }

        public object Visit(TableNameField tree, object data = null)
        {
            return null;
        }

        public object Visit(TableArrayField tree, object data = null)
        {
            return null;
        }

        public object Visit(IndexAccessor tree, object data = null)
        {
            return null;
        }

        public object Visit(MemberAccessor tree, object data = null)
        {
            return null;
        }

        public object Visit(NormalFuncCall tree, object data = null)
        {
            return null;
        }

        public object Visit(MemberFuncCall tree, object data = null)
        {
            return null;
        }

        public object Visit(ExpressionList tree, object data = null)
        {
            return null;
        }

        public object Visit(NameList tree, object data = null)
        {
            return null;
        }
    }
}
