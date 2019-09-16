using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleScript
{
    public abstract class SyntaxTree
    {
        protected int _line = -1;
        public int line
        {
            // set { _line = value; }
            get { return _line; }
        }
    }

    public class ModuleTree : SyntaxTree
    {
        public ModuleTree()
        {
            _line = 0;
        }
        public BlockTree block;
    }

    public class BlockTree : SyntaxTree
    {
        public BlockTree(int line_)
        {
            _line = line_;
        }
        public List<SyntaxTree> statements = new List<SyntaxTree>();
    }

    public class ReturnStatement : SyntaxTree
    {
        public ReturnStatement(int line_)
        {
            _line = line_;
        }
        public ExpressionList exp_list;
    }

    public class BreakStatement : SyntaxTree
    {
        public BreakStatement(int line_)
        {
            _line = line_;
        }
    }

    public class ContinueStatement : SyntaxTree
    {
        public ContinueStatement(int line_)
        {
            _line = line_;
        }
    }

    public class WhileStatement : SyntaxTree
    {
        public WhileStatement(int line_)
        {
            _line = line_;
        }
        public SyntaxTree exp;
        public BlockTree block;
    }

    public class IfStatement : SyntaxTree
    {
        public IfStatement(int line_)
        {
            _line = line_;
        }
        public SyntaxTree exp;
        public BlockTree true_branch;
        public SyntaxTree false_branch;
    }

    public class ForStatement : SyntaxTree
    {
        public ForStatement(int line_)
        {
            _line = line_;
        }
        public Token name;
        public SyntaxTree exp1;
        public SyntaxTree exp2;
        public SyntaxTree exp3;
        public BlockTree block;
    }

    public class ForInStatement : SyntaxTree
    {
        public ForInStatement(int line_)
        {
            _line = line_;
        }
        public NameList name_list;
        public ExpressionList exp_list;
        public BlockTree block;
    }

    public class ForeverStatement : SyntaxTree
    {
        public ForeverStatement(int line_)
        {
            _line = line_;
        }
        public BlockTree block;
    }

    public class TryStatement : SyntaxTree
    {
        public TryStatement(int line_)
        {
            _line = line_;
        }
        public BlockTree block;
        public Token catch_name;
        public BlockTree catch_block;
    }

    public class FunctionStatement : SyntaxTree
    {
        public FunctionStatement(int line_)
        {
            _line = line_;
        }
        public FunctionName func_name;
        public FunctionBody func_body;
    }

    public class FunctionName : SyntaxTree
    {
        public FunctionName(int line_)
        {
            _line = line_;
        }
        public List<Token> names = new List<Token>();
    }

    public abstract class ScopeStatement : SyntaxTree
    {
        public bool is_global = false;
    }

    public class ScopeFunctionStatement : ScopeStatement
    {
        public ScopeFunctionStatement(int line_)
        {
            _line = line_;
        }

        public Token name;
        public FunctionBody func_body;
    }

    public class ScopeNameListStatement : ScopeStatement
    {
        public ScopeNameListStatement(int line_)
        {
            _line = line_;
        }
        public NameList name_list;
        public ExpressionList exp_list;
    }

    public class AssignStatement : SyntaxTree
    {
        public AssignStatement(int line_)
        {
            _line = line_;
        }
        public List<SyntaxTree> var_list = new List<SyntaxTree>();
        public ExpressionList exp_list;
    }

    public class SpecialAssginStatement : SyntaxTree
    {
        public SpecialAssginStatement(int line_)
        {
            _line = line_;
        }
        public SyntaxTree var;
        public SyntaxTree exp;// ++ or -- when exp is null
        public TokenType op;

        public static bool NeedWork(TokenType type)
        {
            return type > TokenType.SpecialAssignBegin && type < TokenType.SpecialAssignEnd;
        }

        public static bool IsSelfMode(TokenType type)
        {
            return type > TokenType.SpecialAssignBegin && type < TokenType.SpecialAssignSelfEnd;
        }
    }

    public class NameList : SyntaxTree
    {
        public NameList(int line_)
        {
            _line = line_;
        }
        public List<Token> names = new List<Token>();
    }

    public class Terminator : SyntaxTree
    {
        public Token token;
        public Terminator(Token token_)
        {
            token = token_;
            _line = token_.m_line;
        }
    }

    public class BinaryExpression : SyntaxTree
    {
        public SyntaxTree left;
        public Token op;
        public SyntaxTree right;
        public BinaryExpression(SyntaxTree left_, Token op_, SyntaxTree right_)
        {
            left = left_;
            op = op_;
            right = right_;
            _line = op_.m_line;
        }
    }

    public class UnaryExpression : SyntaxTree
    {
        public UnaryExpression(int line_)
        {
            _line = line_;
        }
        public SyntaxTree exp;
        public Token op;
    }

    public class FunctionBody : SyntaxTree
    {
        public FunctionBody(int line_)
        {
            _line = line_;
        }
        public ParamList param_list;
        public BlockTree block;
    }

    public class ParamList : SyntaxTree
    {
        public ParamList(int line_)
        {
            _line = line_;
        }
        public List<Token> name_list = new List<Token>();
        public Token kw_name = null;
    }
    public class TableDefine : SyntaxTree
    {
        public TableDefine(int line_)
        {
            _line = line_;
        }
        public List<TableField> fields = new List<TableField>();
        public bool last_field_append_table = false;
    }

    public class TableField : SyntaxTree
    {
        public TableField(int line_)
        {
            _line = line_;
        }
        public SyntaxTree index = null;
        public SyntaxTree value;
    }

    public class TableAccess : SyntaxTree
    {
        public TableAccess(int line_)
        {
            _line = line_;
        }
        public SyntaxTree table;
        public SyntaxTree index;
    }

    public class FuncCall : SyntaxTree
    {
        public FuncCall(int line_)
        {
            _line = line_;
        }
        public SyntaxTree caller;
        public ArgsList args;
    }

    public class ArgsList : SyntaxTree
    {
        public ArgsList(int line_)
        {
            _line = line_;
        }
        public class KW
        {
            public Token k;
            public SyntaxTree w;
        }
        public List<SyntaxTree> exp_list = new List<SyntaxTree>();
        public SyntaxTree kw_table = null;
        public List<KW> kw_exp_list = new List<KW>();
    }

    public class ExpressionList : SyntaxTree
    {
        public ExpressionList(int line_)
        {
            _line = line_;
        }
        public List<SyntaxTree> exp_list = new List<SyntaxTree>();
    }

    public class ComplexString : SyntaxTree
    {
        public ComplexString(int line_)
        {
            _line = line_;
        }
        public bool is_shell = false;// ` and ```
        public string shell_name = null;// 默认空的执行时取 Config.def_shell
        public List<SyntaxTree> list = new List<SyntaxTree>();
    }

    public class ComplexStringItem : SyntaxTree
    {
        public ComplexStringItem(int line_)
        {
            _line = line_;
        }
        public SyntaxTree exp;
        public int len = 0;
        public string format = null;
    }

    public class QuestionExp : SyntaxTree
    {
        public QuestionExp(int line_)
        {
            _line = line_;
        }
        public SyntaxTree a;// exp = a ? b : c
        public SyntaxTree b;
        public SyntaxTree c;// if c == null then exp = a ? b, 并且这儿 a 只判断是否是nil，专门用于默认值语法的。
    }
}
