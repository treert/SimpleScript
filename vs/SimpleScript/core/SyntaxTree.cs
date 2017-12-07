using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    abstract class SyntaxTree
    {
        protected int _line = -1;
        public int line {
            // set { _line = value; }
            get { return _line; }
        }
    }

    class Chunk:SyntaxTree
    {
        public Chunk()
        {
            _line = 0;
        }
        public Block block;
    }

    class Block:SyntaxTree
    {
        public Block(int line_)
        {
            _line = line_;
        }
        public List<SyntaxTree> statements = new List<SyntaxTree>();
    }

    class ReturnStatement:SyntaxTree
    {
        public ReturnStatement(int line_)
        {
            _line = line_;
        }
        public ExpressionList exp_list;
    }

    class BreakStatement:SyntaxTree
    {
        public BreakStatement(int line_)
        {
            _line = line_;
        }
    }

    class ContinueStatement:SyntaxTree
    {
        public ContinueStatement(int line_)
        {
            _line = line_;
        }
    }

    class DoStatement:SyntaxTree
    {
        public DoStatement(int line_)
        {
            _line = line_;
        }
        public Block block;
    }

    class WhileStatement:SyntaxTree
    {
        public WhileStatement(int line_)
        {
            _line = line_;
        }
        public SyntaxTree exp;
        public Block block;
    }

    class IfStatement:SyntaxTree
    {
        public IfStatement(int line_)
        {
            _line = line_;
        }
        public SyntaxTree exp;
        public Block true_branch;
        public SyntaxTree false_branch;
    }

    class ForStatement:SyntaxTree
    {
        public ForStatement(int line_)
        {
            _line = line_;
        }
        public Token name;
        public SyntaxTree exp1;
        public SyntaxTree exp2;
        public SyntaxTree exp3;
        public Block block;
    }

    class ForInStatement : SyntaxTree
    {
        public ForInStatement(int line_)
        {
            _line = line_;
        }
        public NameList name_list;
        public ExpressionList exp_list;
        public Block block;
    }

    class ForEachStatement : SyntaxTree
    {
        public ForEachStatement(int line_)
        {
            _line = line_;
        }
        public Token k,v;
        public SyntaxTree exp;
        public Block block;
    }

    class FunctionStatement : SyntaxTree
    {
        public FunctionStatement(int line_)
        {
            _line = line_;
        }
        public FunctionName func_name;
        public FunctionBody func_body;
    }

    class FunctionName:SyntaxTree
    {
        public FunctionName(int line_)
        {
            _line = line_;
        }
        public List<Token> names = new List<Token>();
    }

    class LocalFunctionStatement : SyntaxTree
    {
        public LocalFunctionStatement(int line_)
        {
            _line = line_;
        }
        public Token name;
        public FunctionBody func_body;
    }

    class LocalNameListStatement:SyntaxTree
    {
        public LocalNameListStatement(int line_)
        {
            _line = line_;
        }
        public NameList name_list;
        public ExpressionList exp_list;
    }

    class AssignStatement:SyntaxTree
    {
        public AssignStatement(int line_)
        {
            _line = line_;
        }
        public List<SyntaxTree> var_list = new List<SyntaxTree>();
        public ExpressionList exp_list;
    }

    class SpecialAssginStatement:SyntaxTree
    {
        public SpecialAssginStatement(int line_)
        {
            _line = line_;
        }
        public SyntaxTree var;
        public SyntaxTree exp;// ++ or -- when exp is null
        public bool is_add_op;
    }

    class NameList:SyntaxTree
    {
        public NameList(int line_)
        {
            _line = line_;
        }
        public List<Token> names = new List<Token>();
    }

    class Terminator:SyntaxTree
    {
        public Token token;
        public Terminator(Token token_)
        {
            token = token_;
            _line = token_.m_line;
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
            _line = op_.m_line;
        }
    }

    class UnaryExpression:SyntaxTree
    {
        public UnaryExpression(int line_)
        {
            _line = line_;
        }
        public SyntaxTree exp;
        public Token op;
    }

    class FunctionBody:SyntaxTree
    {
        public FunctionBody(int line_)
        {
            _line = line_;
        }
        public ParamList param_list;
        public Block block;
    }

    class ParamList:SyntaxTree
    {
        public ParamList(int line_)
        {
            _line = line_;
        }
        public List<Token> name_list = new List<Token>();
        public bool is_var_arg = false;
    }
    class TableDefine:SyntaxTree
    {
        public TableDefine(int line_)
        {
            _line = line_;
        }
        public List<TableField> fields = new List<TableField>();
        public bool last_field_append_table = false;
    }

    class TableField:SyntaxTree
    {
        public TableField(int line_)
        {
            _line = line_;
        }
        public SyntaxTree index;
        public SyntaxTree value;
    }

    class TableAccess:SyntaxTree
    {
        public TableAccess(int line_)
        {
            _line = line_;
        }
        public SyntaxTree table;
        public SyntaxTree index;
    }

    class FuncCall:SyntaxTree
    {
        public FuncCall(int line_)
        {
            _line = line_;
        }
        public SyntaxTree caller;
        public ExpressionList args;
    }

    class AsyncCall : FuncCall
    {
        public AsyncCall(int line_):base(line_)
        {
        }
    }

    class ExpressionList:SyntaxTree
    {
        public ExpressionList(int line_)
        {
            _line = line_;
        }
        public List<SyntaxTree> exp_list = new List<SyntaxTree>();
        public bool return_any_value = false;
        public int expect_value_count = -1;
    }
}
