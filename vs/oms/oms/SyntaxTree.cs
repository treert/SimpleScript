using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{
    abstract class SyntaxTree
    {
    }

    class Chunk:SyntaxTree
    {
        public Block block;
    }

    class Block:SyntaxTree
    {
        public List<SyntaxTree> statements = new List<SyntaxTree>();
    }

    class ReturnStatement:SyntaxTree
    {
        public ExpressionList exp_list;
    }

    class BreakStatement:SyntaxTree
    {
    }

    class ContinueStatement:SyntaxTree
    {
    }

    class DoStatement:SyntaxTree
    {
        public Block block;
    }

    class WhileStatement:SyntaxTree
    {
        public SyntaxTree exp;
        public Block block;
    }

    class IfStatement:SyntaxTree
    {
        public SyntaxTree exp;
        public Block true_branch;
        public SyntaxTree false_branch;
    }

    class ForStatement:SyntaxTree
    {
        public Token name;
        public SyntaxTree exp1;
        public SyntaxTree exp2;
        public SyntaxTree exp3;
        public Block block;
    }

    class ForInStatement : SyntaxTree
    {
        public NameList name_list;
        public ExpressionList exp_list;
        public Block block;
    }

    class ForEachStatement : SyntaxTree
    {
        public Token k,v;
        public SyntaxTree exp;
        public Block block;
    }

    class FunctionStatement : SyntaxTree
    {
        public FunctionName func_name;
        public FunctionBody func_body;
    }

    class FunctionName:SyntaxTree
    {
        public List<Token> names = new List<Token>();
    }

    class LocalFunctionStatement : SyntaxTree
    {
        public Token name;
        public FunctionBody func_body;
    }

    class LocalNameListStatement:SyntaxTree
    {
        public NameList name_list;
        public ExpressionList exp_list;
    }

    class AssignStatement:SyntaxTree
    {
        public List<SyntaxTree> var_list = new List<SyntaxTree>();
        public ExpressionList exp_list;
    }

    class NameList:SyntaxTree
    {
        public List<Token> names = new List<Token>();
    }

    class Terminator:SyntaxTree
    {
        public Token token;
        public Terminator(Token token_)
        {
            token = token_;
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
    }

    class UnaryExpression:SyntaxTree
    {
        public SyntaxTree exp;
        public Token op;
    }

    class FunctionBody:SyntaxTree
    {
        public ParamList param_list;
        public Block block;
    }
    class ParamList:SyntaxTree
    {
        public List<Token> name_list = new List<Token>();
        public bool is_var_arg = false;
    }
    class TableDefine:SyntaxTree
    {
        public List<TableField> fields = new List<TableField>();
        public bool last_field_append_table = false;
    }

    class TableField:SyntaxTree
    {
        public SyntaxTree index;
        public SyntaxTree value;
    }

    class TableAccess:SyntaxTree
    {
        public SyntaxTree table;
        public SyntaxTree index;
    }

    class FuncCall:SyntaxTree
    {
        public SyntaxTree caller;
        public Token member_name;
        public ExpressionList args;
    }

    class ExpressionList:SyntaxTree
    {
        public List<SyntaxTree> exp_list = new List<SyntaxTree>();
        public bool return_any_value = false;
        public int expect_value_count = -1;
    }
}
