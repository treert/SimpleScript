using System;
using System.Collections.Generic;
using System.Text;

namespace SScript
{
    public abstract class SyntaxTree
    {
        protected int _line = -1;
        public int line
        {
            // set { _line = value; }
            get { return _line; }
        }

        public virtual void Exec(Frame frame) { }
    }

    // 表达式语法结构，可以返回一个或者多个结果。极端情况会返回0个，比如 ...
    // 性能浪费何其严重，Σ( ° △ °|||)︴
    public abstract class ExpSyntaxTree : SyntaxTree
    {
        public override void Exec(Frame frame)
        {
            GetResults(frame);
        }
        public object GetOneResult(Frame frame)
        {
            var x = GetResults(frame);
            return x.Count > 0 ? x[0] : null;
        }
        public abstract List<object> GetResults(Frame frame);
    }

    public class ModuleTree : SyntaxTree
    {
        public ModuleTree()
        {
            _line = 0;
        }
        public BlockTree block;

        public FunctionBody ConvertToFuncBody()
        {
            FunctionBody ret = new FunctionBody(line);
            ret.block = block;
            return ret;
        }
    }

    public class BlockTree : SyntaxTree
    {
        public BlockTree(int line_)
        {
            _line = line_;
        }
        public List<SyntaxTree> statements = new List<SyntaxTree>();
    }

    public class ReturnStatement : ExpSyntaxTree
    {
        public ReturnStatement(int line_)
        {
            _line = line_;
        }
        public ExpressionList exp_list;

        public override List<object> GetResults(Frame frame)
        {
            return null;
        }
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

    public class Terminator : ExpSyntaxTree
    {
        public Token token;
        public Terminator(Token token_)
        {
            token = token_;
            _line = token_.m_line;
        }

        public override List<object> GetResults(Frame frame)
        {
            return null;
        }
    }

    public class BinaryExpression : ExpSyntaxTree
    {
        public ExpSyntaxTree left;
        public Token op;
        public ExpSyntaxTree right;
        public BinaryExpression(ExpSyntaxTree left_, Token op_, ExpSyntaxTree right_)
        {
            left = left_;
            op = op_;
            right = right_;
            _line = op_.m_line;
        }

        public override List<object> GetResults(Frame frame)
        {
            return null;
        }
    }

    public class UnaryExpression : ExpSyntaxTree
    {
        public UnaryExpression(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree exp;
        public Token op;

        public override List<object> GetResults(Frame frame)
        {
            return null;
        }
    }

    public class FunctionBody : ExpSyntaxTree
    {
        public FunctionBody(int line_)
        {
            _line = line_;
        }
        public ParamList param_list;
        public BlockTree block;

        public override List<object> GetResults(Frame frame)
        {
            return null;
        }
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
    public class TableDefine : ExpSyntaxTree
    {
        public TableDefine(int line_)
        {
            _line = line_;
        }
        public List<TableField> fields = new List<TableField>();

        public override List<object> GetResults(Frame frame)
        {
            return null;
        }
    }

    public class TableField : SyntaxTree
    {
        public TableField(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree index = null;
        public ExpSyntaxTree value;
    }

    public class TableAccess : ExpSyntaxTree
    {
        public TableAccess(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree table;
        public ExpSyntaxTree index;

        public override List<object> GetResults(Frame frame)
        {
            return null;
        }
    }

    public class FuncCall : ExpSyntaxTree
    {
        public FuncCall(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree caller;
        public ArgsList args;

        public override List<object> GetResults(Frame frame)
        {
            return null;
        }
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
            public ExpSyntaxTree w;
        }
        public List<ExpSyntaxTree> exp_list = new List<ExpSyntaxTree>();
        public ExpSyntaxTree kw_table = null;
        public List<KW> kw_exp_list = new List<KW>();
    }

    public class ExpressionList : ExpSyntaxTree
    {
        public ExpressionList(int line_)
        {
            _line = line_;
        }
        public List<ExpSyntaxTree> exp_list = new List<ExpSyntaxTree>();

        public override List<object> GetResults(Frame frame)
        {
            return null;
        }
    }

    public class ComplexString : ExpSyntaxTree
    {
        public ComplexString(int line_)
        {
            _line = line_;
        }
        public bool is_shell = false;// ` and ```
        public string shell_name = null;// 默认空的执行时取 Config.def_shell
        public List<ExpSyntaxTree> list = new List<ExpSyntaxTree>();

        public override List<object> GetResults(Frame frame)
        {
            return null;
        }
    }

    public class ComplexStringItem : ExpSyntaxTree
    {
        public ComplexStringItem(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree exp;
        public int len = 0;
        public string format = null;

        public override List<object> GetResults(Frame frame)
        {
            return null;
        }
    }

    public class QuestionExp : ExpSyntaxTree
    {
        public QuestionExp(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree a;// exp = a ? b : c
        public ExpSyntaxTree b;
        public ExpSyntaxTree c;// if c == null then exp = a ? b, 并且这儿 a 只判断是否是nil，专门用于默认值语法的。

        public override List<object> GetResults(Frame frame)
        {
            return null;
        }
    }
}
