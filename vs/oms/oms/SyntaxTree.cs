using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{
    interface Visitor
    {
        void Visit(Chunk tree, ref object data);
        void Visit(Block tree, ref object data);
        void Visit(ReturnStatement tree, ref object data);
        void Visit(BreakStatement tree, ref object data);
        void Visit(ContinueStatement tree, ref object data);
        void Visit(DoStatement tree, ref object data);
        void Visit(WhileStatement tree, ref object data);
        void Visit(IfStatement tree, ref object data);
        void Visit(ForStatement tree, ref object data);
        void Visit(ForInStatement tree, ref object data);
        void Visit(ForEachStatement tree, ref object data);
        void Visit(FunctionStatement tree, ref object data);
        void Visit(FunctionName tree, ref object data);
        void Visit(LocalFunctionStatement tree, ref object data);
        void Visit(LocalNameListStatement tree, ref object data);
        void Visit(AssignStatement tree, ref object data);
        void Visit(VarList tree, ref object data);
        void Visit(Terminator tree, ref object data);
        void Visit(BinaryExpression tree, ref object data);
        void Visit(UnaryExpression tree, ref object data);
        void Visit(FunctionBody tree, ref object data);
        void Visit(ParamList tree, ref object data);
        void Visit(TableDefine tree, ref object data);
        void Visit(TableIndexField tree, ref object data);
        void Visit(TableNameField tree, ref object data);
        void Visit(TableArrayField tree, ref object data);
        void Visit(IndexAccessor tree, ref object data);
        void Visit(MemberAccessor tree, ref object data);
        void Visit(NormalFuncCall tree, ref object data);
        void Visit(MemberFuncCall tree, ref object data);
        void Visit(ExpressionList tree, ref object data);
        void Visit(NameList tree, ref object data);
        //void Visit(SyntaxTree tree, ref object data);
    }
    
    abstract class SyntaxTree
    {
        public abstract void Accept(Visitor v, ref object data);
    }

    class Chunk:SyntaxTree
    {
        public SyntaxTree block;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class Block:SyntaxTree
    {
        public List<SyntaxTree> statements = new List<SyntaxTree>();
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class ReturnStatement:SyntaxTree
    {
        public SyntaxTree exp_list;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class BreakStatement:SyntaxTree
    {
        // for semantic
        public SyntaxTree loop;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class ContinueStatement:SyntaxTree
    {
        // for semantic
        public SyntaxTree loop;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class DoStatement:SyntaxTree
    {
        public SyntaxTree block;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class WhileStatement:SyntaxTree
    {
        public SyntaxTree exp;
        public SyntaxTree block;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class IfStatement:SyntaxTree
    {
        public SyntaxTree exp;
        public SyntaxTree true_branch;
        public SyntaxTree false_branch;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class ForStatement:SyntaxTree
    {
        public Token name;
        public SyntaxTree exp1;
        public SyntaxTree exp2;
        public SyntaxTree exp3;
        public SyntaxTree block;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class ForInStatement : SyntaxTree
    {
        public SyntaxTree name_list;
        public SyntaxTree exp_list;
        public SyntaxTree block;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class ForEachStatement : SyntaxTree
    {
        public Token k,v;
        public SyntaxTree exp;
        public SyntaxTree block;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class FunctionStatement : SyntaxTree
    {
        public SyntaxTree func_name;
        public SyntaxTree func_body;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class FunctionName:SyntaxTree
    {
        public List<Token> names = new List<Token>();
        public Token member_name;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class LocalFunctionStatement : SyntaxTree
    {
        public Token name;
        public SyntaxTree func_body;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class LocalNameListStatement:SyntaxTree
    {
        public SyntaxTree name_list;
        public SyntaxTree exp_list;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }
    
    class AssignStatement:SyntaxTree
    {
        public SyntaxTree var_list;
        public SyntaxTree exp_list;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class VarList:SyntaxTree
    {
        public List<SyntaxTree> var_list = new List<SyntaxTree>();
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class NameList:SyntaxTree
    {
        public List<Token> names = new List<Token>();
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class Terminator:SyntaxTree
    {
        public Token token;
        public Terminator(Token token_)
        {
            token = token_;
        }
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class BinaryExpression:SyntaxTree
    {
        public SyntaxTree left;
        public Token op;
        public SyntaxTree right;
        public BinaryExpression(SyntaxTree left_,Token op_,SyntaxTree right_)
        {
            left = left_;
            op = op_;
            right = right_;
        }
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class UnaryExpression:SyntaxTree
    {
        public SyntaxTree exp;
        public Token op;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class FunctionBody:SyntaxTree
    {
        public SyntaxTree param_list;
        public SyntaxTree block;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }
    class ParamList:SyntaxTree
    {
        public List<Token> name_list = new List<Token>();
        public bool is_var_arg = false;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }
    class TableDefine:SyntaxTree
    {
        public List<SyntaxTree> fields = new List<SyntaxTree>();
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class TableIndexField:SyntaxTree
    {
        public SyntaxTree index;
        public SyntaxTree value;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class TableNameField:SyntaxTree
    {
        public Token name;
        public SyntaxTree value;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class TableArrayField:SyntaxTree
    {
        public SyntaxTree value;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class IndexAccessor:SyntaxTree
    {
        public SyntaxTree table;
        public SyntaxTree index;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class MemberAccessor:SyntaxTree
    {
        public SyntaxTree table;
        public Token member_name;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class NormalFuncCall:SyntaxTree
    {
        public SyntaxTree caller;
        public SyntaxTree args;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }

    class MemberFuncCall:SyntaxTree
    {
        public SyntaxTree caller;
        public Token member_name;
        public SyntaxTree args;
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }
    
    class ExpressionList:SyntaxTree
    {
        public List<SyntaxTree> exp_list = new List<SyntaxTree>();
        public override void Accept(Visitor v, ref object data)
        {
            v.Visit(this, ref data);
        }
    }
}
    