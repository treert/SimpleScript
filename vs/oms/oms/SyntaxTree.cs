using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{
    interface Visitor
    {
        object Visit(Chunk tree, object data = null);
        object Visit(Block tree, object data = null);
        object Visit(ReturnStatement tree, object data = null);
        object Visit(BreakStatement tree, object data = null);
        object Visit(ContinueStatement tree, object data = null);
        object Visit(DoStatement tree, object data = null);
        object Visit(WhileStatement tree, object data = null);
        object Visit(IfStatement tree, object data = null);
        object Visit(ForStatement tree, object data = null);
        object Visit(ForInStatement tree, object data = null);
        object Visit(ForEachStatement tree, object data = null);
        object Visit(FunctionStatement tree, object data = null);
        object Visit(FunctionName tree, object data = null);
        object Visit(LocalFunctionStatement tree, object data = null);
        object Visit(LocalNameListStatement tree, object data = null);
        object Visit(AssignStatement tree, object data = null);
        object Visit(Terminator tree, object data = null);
        object Visit(BinaryExpression tree, object data = null);
        object Visit(UnaryExpression tree, object data = null);
        object Visit(FunctionBody tree, object data = null);
        object Visit(ParamList tree, object data = null);
        object Visit(TableDefine tree, object data = null);
        object Visit(TableField tree, object data = null);
        object Visit(TableAccess tree, object data = null);
        object Visit(FuncCall tree, object data = null);
        object Visit(ExpressionList tree, object data = null);
        object Visit(NameList tree, object data = null);
        //object Visit(SyntaxTree tree, object data = null);
    }


    abstract class SyntaxTree
    {
        public abstract object Accept(Visitor v, object data = null);
    }

    class Chunk:SyntaxTree
    {
        public Block block;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class Block:SyntaxTree
    {
        public List<SyntaxTree> statements = new List<SyntaxTree>();
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class ReturnStatement:SyntaxTree
    {
        public ExpressionList exp_list;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class BreakStatement:SyntaxTree
    {
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class ContinueStatement:SyntaxTree
    {
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class DoStatement:SyntaxTree
    {
        public Block block;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class WhileStatement:SyntaxTree
    {
        public SyntaxTree exp;
        public Block block;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class IfStatement:SyntaxTree
    {
        public SyntaxTree exp;
        public Block true_branch;
        public SyntaxTree false_branch;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class ForStatement:SyntaxTree
    {
        public Token name;
        public SyntaxTree exp1;
        public SyntaxTree exp2;
        public SyntaxTree exp3;
        public Block block;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class ForInStatement : SyntaxTree
    {
        public NameList name_list;
        public ExpressionList exp_list;
        public Block block;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class ForEachStatement : SyntaxTree
    {
        public Token k,v;
        public SyntaxTree exp;
        public Block block;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class FunctionStatement : SyntaxTree
    {
        public FunctionName func_name;
        public FunctionBody func_body;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class FunctionName:SyntaxTree
    {
        public List<Token> names = new List<Token>();
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class LocalFunctionStatement : SyntaxTree
    {
        public Token name;
        public FunctionBody func_body;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class LocalNameListStatement:SyntaxTree
    {
        public NameList name_list;
        public ExpressionList exp_list;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class AssignStatement:SyntaxTree
    {
        public List<SyntaxTree> var_list = new List<SyntaxTree>();
        public ExpressionList exp_list;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class NameList:SyntaxTree
    {
        public List<Token> names = new List<Token>();
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class Terminator:SyntaxTree
    {
        public Token token;
        public Terminator(Token token_)
        {
            token = token_;
        }
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
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
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class UnaryExpression:SyntaxTree
    {
        public SyntaxTree exp;
        public Token op;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class FunctionBody:SyntaxTree
    {
        public ParamList param_list;
        public Block block;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }
    class ParamList:SyntaxTree
    {
        public List<Token> name_list = new List<Token>();
        public bool is_var_arg = false;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }
    class TableDefine:SyntaxTree
    {
        public List<TableField> fields = new List<TableField>();
        public bool last_field_append_table = false;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class TableField:SyntaxTree
    {
        public SyntaxTree index;
        public SyntaxTree value;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class TableAccess:SyntaxTree
    {
        public SyntaxTree table;
        public SyntaxTree index;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class FuncCall:SyntaxTree
    {
        public SyntaxTree caller;
        public Token member_name;
        public ExpressionList args;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }

    class ExpressionList:SyntaxTree
    {
        public List<SyntaxTree> exp_list = new List<SyntaxTree>();
        public bool return_any_value = false;
        public int expect_value_count = -1;
        public override object Accept(Visitor v, object data = null)
        {
            return v.Visit(this, data);
        }
    }
}
