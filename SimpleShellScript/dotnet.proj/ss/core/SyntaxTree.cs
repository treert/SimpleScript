using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static implicit operator bool(SyntaxTree exsit)
        {
            return exsit != null;
        }

        public void Exec(Frame frame)
        {
            //try
            {
                _Exec(frame);
            }
        }

        protected virtual void _Exec(Frame frame) { }
    }

    // 表达式语法结构，可以返回一个或者多个结果。极端情况会返回0个，比如 ...
    // 性能浪费何其严重，Σ( ° △ °|||)︴
    public abstract class ExpSyntaxTree : SyntaxTree
    {
        protected override void _Exec(Frame frame)
        {
            _GetResults(frame);
        }
        public object GetOneResult(Frame frame)
        {
            var x = GetResults(frame);
            return x.Count > 0 ? x[0] : null;
        }

        public bool GetBool(Frame frame)
        {
            var x = GetResults(frame);
            return x.Count > 0 ? ValueUtils.ToBool(x[0]) : false;
        }

        public double GetNumber(Frame frame)
        {
            var x = GetResults(frame);
            if(x.Count == 0 || (x[0] is double) == false)
            {
                throw frame.NewRunException(line, "expect number result");
            }
            return (double)x[0];
        }

        public ITable GetTable(Frame frame)
        {
            var x = GetResults(frame);
            if (x.Count == 0 || (x[0] is ITable) == false)
            {
                throw frame.NewRunException(line, "expect IGetSet(Table) result");
            }
            return x[0] as ITable;
        }

        public List<object> GetResults(Frame frame)
        {
            return _GetResults(frame);
        }

        protected abstract List<object> _GetResults(Frame frame);
    }

    public class BlockTree : SyntaxTree
    {
        public BlockTree(int line_)
        {
            _line = line_;
        }
        public List<SyntaxTree> statements = new List<SyntaxTree>();

        protected override void _Exec(Frame frame)
        {
            frame.EnterBlock();
            {
                foreach (var it in statements)
                {
                    it.Exec(frame);
                }
            }
            frame.LeaveBlock();
        }
    }

    public class ReturnStatement : ExpSyntaxTree
    {
        public ReturnStatement(int line_)
        {
            _line = line_;
        }
        public ExpressionList exp_list;

        protected override List<object> _GetResults(Frame frame)
        {
            ReturnException ep = new ReturnException();
            if (exp_list)
            {
                ep.results = exp_list.GetResults(frame);
            }
            throw ep;
        }
    }

    public class BreakStatement : SyntaxTree
    {
        public BreakStatement(int line_)
        {
            _line = line_;
        }

        protected override void _Exec(Frame frame)
        {
            throw new BreakException(_line);
        }
    }

    public class ContinueStatement : SyntaxTree
    {
        public ContinueStatement(int line_)
        {
            _line = line_;
        }

        protected override void _Exec(Frame frame)
        {
            throw new ContineException(_line);
        }
    }

    public class WhileStatement : SyntaxTree
    {
        public WhileStatement(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree exp;
        public BlockTree block;

        protected override void _Exec(Frame frame)
        {
            while (true)
            {
                var obj = exp.GetOneResult(frame);
                if (ValueUtils.ToBool(obj))
                {
                    block.Exec(frame);
                }
                else
                {
                    break;
                }
            }
        }
    }

    public class IfStatement : SyntaxTree
    {
        public IfStatement(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree exp;
        public BlockTree true_branch;
        public SyntaxTree false_branch;

        protected override void _Exec(Frame frame)
        {
            var obj = exp.GetOneResult(frame);
            if (ValueUtils.ToBool(obj))
            {
                true_branch.Exec(frame);
            }
            else
            {
                false_branch?.Exec(frame);
            }
        }
    }

    public class ForStatement : SyntaxTree
    {
        public ForStatement(int line_)
        {
            _line = line_;
        }
        public Token name;
        public ExpSyntaxTree exp1;
        public ExpSyntaxTree exp2;
        public ExpSyntaxTree exp3;
        public BlockTree block;
        protected override void _Exec(Frame frame)
        {
            var start = exp1.GetNumber(frame);
            var end = exp2.GetNumber(frame);
            if(start <= end)
            {
                double step = exp3 ? exp3.GetNumber(frame) : 1;
                if(step <= 0)
                {
                    throw frame.NewRunException(line, $"for step {step} should greater than 0, or will cause forerver loop");
                }
                var cur_block = frame.cur_block;
                for(double it = start; it <= end; it += step)
                {
                    frame.cur_block = cur_block;
                    try
                    {
                        var b = frame.EnterBlock();
                        frame.AddLocalVal(name.m_string, it);
                        block.Exec(frame);
                    }
                    catch(ContineException)
                    {
                        continue;
                    }
                    catch (BreakException)
                    {
                        break;
                    }
                }
                frame.cur_block = cur_block;
            }
            else
            {
                double step = exp3 ? exp3.GetNumber(frame) : -1;
                if (step >= 0)
                {
                    throw frame.NewRunException(line, $"for step {step} should less than 0, or will cause forerver loop");
                }
                var cur_block = frame.cur_block;
                for (double it = start; it >= end; it += step)
                {
                    frame.cur_block = cur_block;
                    try
                    {
                        var b = frame.EnterBlock();
                        frame.AddLocalVal(name.m_string, it);
                        block.Exec(frame);
                    }
                    catch (ContineException)
                    {
                        continue;
                    }
                    catch (BreakException)
                    {
                        break;
                    }
                }
                frame.cur_block = cur_block;
            }
        }
    }

    public class ForInStatement : SyntaxTree
    {
        public ForInStatement(int line_)
        {
            _line = line_;
        }
        public NameList name_list;
        public ExpSyntaxTree exp;
        public BlockTree block;

        protected override void _Exec(Frame frame)
        {
            var obj = exp.GetOneResult(frame);
            if (obj == null) return;// 无事发生，虽然按理应该报个错啥的。
            IForIter iter = obj as IForIter;
            if(iter == null && obj is IForEach)
            {
                iter = (obj as IForEach).GetIter();
            }
            var cur_block = frame.cur_block;
            if(iter != null)
            {
                object k, v;
                while(iter.Next(out k, out v))
                {
                    frame.cur_block = cur_block;
                    try
                    {
                        frame.EnterBlock();
                        name_list.AddLocals(frame, k, v);
                        block.Exec(frame);
                    }
                    catch (ContineException)
                    {
                        continue;
                    }
                    catch (BreakException)
                    {
                        break;
                    }
                }
            }
            else if(obj is Function)
            {
                for(; ; )
                {
                    var results = (obj as Function).Call();
                    if(results.Count > 0 && results[0] != null)
                    {
                        frame.cur_block = cur_block;
                        try
                        {
                            frame.EnterBlock();
                            name_list.AddLocals(frame, results);
                            block.Exec(frame);
                        }
                        catch (ContineException)
                        {
                            continue;
                        }
                        catch (BreakException)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                throw frame.NewRunException(exp.line, $"for in does not support type {obj.GetType().Name}");
            }
            frame.cur_block = cur_block;
        }
    }

    public class ForeverStatement : SyntaxTree
    {
        public ForeverStatement(int line_)
        {
            _line = line_;
        }
        public BlockTree block;

        protected override void _Exec(Frame frame)
        {
            var cur_block = frame.cur_block;
            //int cnt = 0;
            for (; ; )
            {
                //if(cnt++ >= int.MaxValue)
                //{
                //    throw frame.NewRunException(line, "forever loop seens can not ended");
                //}
                frame.cur_block = cur_block;
                try
                {
                    frame.EnterBlock();
                    block.Exec(frame);
                }
                catch (ContineException)
                {
                    continue;
                }
                catch (BreakException)
                {
                    break;
                }
            }
            frame.cur_block = cur_block;
        }
    }

    public class ThrowStatement: SyntaxTree
    {
        public ThrowStatement(int line)
        {
            _line = line;
        }
        public ExpSyntaxTree exp;

        protected override void _Exec(Frame frame)
        {
            ThrowException ep = new ThrowException();
            ep.line = _line;
            ep.source_name = frame.func.code.source_name;
            if (exp)
            {
                ep.obj = exp.GetOneResult(frame);
            }
            throw ep;
        }
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

        protected override void _Exec(Frame frame)
        {
            try
            {
                block.Exec(frame);
            }
            catch (ContineException)
            {
                throw;
            }
            catch (BreakException)
            {
                throw;
            }
            catch(Exception e)
            {
                frame.EnterBlock();
                if (catch_name)
                {
                    frame.AddLocalVal(catch_name.m_string, e);
                }
                catch_block.Exec(frame);
                frame.LeaveBlock();
            }
        }
    }

    public class FunctionStatement : SyntaxTree
    {
        public FunctionStatement(int line_)
        {
            _line = line_;
        }
        public FunctionName func_name;
        public FunctionBody func_body;

        protected override void _Exec(Frame frame)
        {
            var fn = func_body.GetOneResult(frame);
            if(func_name.names.Count == 1)
            {
                frame.Write(func_name.names[0].m_string, fn);
            }
            else
            {
                var names = func_name.names;
                var obj = frame.Read(names[0].m_string);
                if(obj == null)
                {
                    obj = frame.Write(names[0].m_string, new Table());
                }
                if(obj is Table == false)
                {
                    throw frame.NewRunException(line, $"{names[0].m_string} is not Table which expect to be");
                }
                ITable t = obj as Table;
                for(int i = 1; i < names.Count-1; i++)
                {
                    var tt = t.Get(names[i].m_string);
                    if(tt == null)
                    {
                        tt = t.Set(names[i].m_string, new Table());
                    }
                    if(tt is ITable == false)
                    {
                        throw frame.NewRunException(names[i].m_line, $"expect {names[i].m_string} to be a IGetSet");
                    }
                    t = tt as ITable;
                }
                t.Set(names[names.Count - 1].m_string, fn);
            }
        }
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
        protected override void _Exec(Frame frame)
        {
            if (is_global)
            {
                frame.AddGlobalName(name.m_string);
            }
            else
            {
                frame.AddLocalName(name.m_string);
            }
            var fn = func_body.GetOneResult(frame);
            frame.Write(name.m_string, fn);
        }
    }

    public class ScopeNameListStatement : ScopeStatement
    {
        public ScopeNameListStatement(int line_)
        {
            _line = line_;
        }
        public NameList name_list;
        public ExpressionList exp_list;

        protected override void _Exec(Frame frame)
        {
            var results = Config.EmptyResults;
            if (exp_list)
            {
                results = exp_list.GetResults(frame);
            }
            for(int i = 0; i < name_list.names.Count; i++)
            {
                var name = name_list.names[i];
                var obj = results.Count > i ? results[i] : null;
                if (is_global)
                {
                    frame.AddGlobalName(name.m_string);
                    if(results.Count > i)
                    {
                        frame.func.vm.global_table.Set(name.m_string, obj);
                    }
                }
                else
                {
                    frame.AddLocalVal(name.m_string, obj);
                }
            }
        }
    }

    public class AssignStatement : SyntaxTree
    {
        public AssignStatement(int line_)
        {
            _line = line_;
        }
        public List<ExpSyntaxTree> var_list = new List<ExpSyntaxTree>();
        public ExpressionList exp_list;

        protected override void _Exec(Frame frame)
        {
            var results = exp_list.GetResults(frame);
            for(int i = 0; i < var_list.Count; i++)
            {
                var it = var_list[i];
                object val = results.Count > i ? results[i] : null;
                if (it is TableAccess)
                {
                    // TableAccess
                    var access = it as TableAccess;
                    var table = access.table.GetTable(frame);
                    var idx = access.index.GetOneResult(frame);
                    if(idx == null)
                    {
                        throw frame.NewRunException(line, "table index can not be null");
                    }
                    table.Set(idx, val);
                }
                else
                {
                    // Name
                    var ter = it as Terminator;
                    Debug.Assert(ter.token.Match(TokenType.NAME));
                    var name = ter.token.m_string;
                    frame.Write(name, val);
                }
            }
        }
    }

    public class SpecialAssginStatement : SyntaxTree
    {
        public SpecialAssginStatement(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree var;
        public ExpSyntaxTree exp;// ++ or -- when exp is null
        public TokenType op;

        public static bool NeedWork(TokenType type)
        {
            return type > TokenType.SpecialAssignBegin && type < TokenType.SpecialAssignEnd;
        }

        public static bool IsSelfMode(TokenType type)
        {
            return type > TokenType.SpecialAssignBegin && type < TokenType.SpecialAssignSelfEnd;
        }

        protected override void _Exec(Frame frame)
        {
            double delta = 1;
            if (exp != null)
            {
                delta = exp.GetNumber(frame);
            }
            if (var is TableAccess)
            {
                var access = var as TableAccess;
                var table = access.table.GetTable(frame);
                var idx = access.index.GetOneResult(frame);
                if (idx == null)
                {
                    throw frame.NewRunException(line, "table index can not be null");
                }
                var val = table.Get(idx);
                if (val is double)
                {
                    table.Set(idx, delta + (double)val);
                }
                else
                {
                    throw frame.NewRunException(access.line, $"expect a double");
                }
            }
            else
            {
                var ter = var as Terminator;
                Debug.Assert(ter.token.Match(TokenType.NAME));
                var name = ter.token.m_string;
                var val = frame.Read(name);
                if(val is double)
                {
                    frame.Write(name, delta + (double)val);
                }
                else
                {
                    throw frame.NewRunException(ter.line, $"expect {name}'s value to be a double");
                }
            }
        }
    }

    public class NameList : SyntaxTree
    {
        public NameList(int line_)
        {
            _line = line_;
        }
        public List<Token> names = new List<Token>();

        public void AddLocals(Frame frame, params object[] objs)
        {
            for(int i = 0; i < names.Count; i++)
            {
                frame.AddLocalVal(names[i].m_string, objs.Length > i ? objs[i] : null);
            }
        }

        public void AddLocals(Frame frame, List<object> objs)
        {
            for (int i = 0; i < names.Count; i++)
            {
                frame.AddLocalVal(names[i].m_string, objs.Count > i ? objs[i] : null);
            }
        }
    }

    public class Terminator : ExpSyntaxTree
    {
        public Token token;
        public Terminator(Token token_)
        {
            token = token_;
            _line = token_.m_line;
        }

        protected override List<object> _GetResults(Frame frame)
        {
            if (token.Match(TokenType.DOTS))
            {
                return frame.extra_args;
            }

            object obj = null;
            if (token.Match(TokenType.NAME))
            {
                obj = frame.Read(token.m_string);
            }
            else if (token.Match(TokenType.NIL))
            {
                obj = null;
            }
            else if (token.Match(TokenType.TRUE)){
                obj = true;
            }
            else if (token.Match(TokenType.FALSE))
            {
                obj = false;
            }
            else if (token.Match(TokenType.NUMBER))
            {
                obj = token.m_number;
            }
            else if (token.Match(TokenType.STRING))
            {
                obj = token.m_string;
            }
            else
            {
                Debug.Assert(false);
            }
            return new List<object>() { obj};
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

        void CheckNumberType(object l, object r, Frame frame, bool check_right_zero = false)
        {
            if(l is double == false)
            {
                throw frame.NewRunException(left.line, "expect bin_op left to be a number");
            }
            if(r is double == false)
            {
                throw frame.NewRunException(right.line, "expect bin_op right to be a number");
            }
            if (check_right_zero)
            {
                var t = (double)r;
                if(t == 0)
                {
                    throw frame.NewRunException(right.line, "bin_op right value is zero");
                }
            }
        }

        protected override List<object> _GetResults(Frame frame)
        {
            object ret = null,l,r;
            l = left.GetOneResult(frame);
            if (op.Match(TokenType.AND))
            {
                if (ValueUtils.ToBool(l))
                {
                    r = right.GetOneResult(frame);
                    ret = ValueUtils.ToBool(r);
                }
                else
                {
                    ret = false;
                }
            }
            else if (op.Match(TokenType.OR))
            {
                if (ValueUtils.ToBool(l))
                {
                    ret = true;
                }
                else
                {
                    r = right.GetOneResult(frame);
                    ret = ValueUtils.ToBool(r);
                }
            }
            else
            {
                r = right.GetOneResult(frame);
                if (op.Match('+'))
                {
                    CheckNumberType(l, r, frame);
                    ret = (double)l + (double)r;
                }
                else if (op.Match('-'))
                {
                    CheckNumberType(l, r, frame);
                    ret = (double)l - (double)r;
                }
                else if (op.Match('*'))
                {
                    CheckNumberType(l, r, frame);
                    ret = (double)l * (double)r;
                }
                else if (op.Match('/'))
                {
                    CheckNumberType(l, r, frame, true);
                    ret = (double)l / (double)r;
                }
                else if (op.Match('%'))
                {
                    CheckNumberType(l, r, frame, true);
                    ret = (double)l % (double)r;
                }
                else if (op.Match('<'))
                {
                    CheckNumberType(l, r, frame);
                    ret = (double)l < (double)r;
                }
                else if (op.Match('>'))
                {
                    CheckNumberType(l, r, frame);
                    ret = (double)l > (double)r;
                }
                else if (op.Match(TokenType.LE))
                {
                    CheckNumberType(l, r, frame);
                    ret = (double)l <= (double)r;
                }
                else if (op.Match(TokenType.GE))
                {
                    CheckNumberType(l, r, frame);
                    ret = (double)l >= (double)r;
                }
                else if (op.Match(TokenType.EQ))
                {
                    ret = l == r;
                }
                else if (op.Match(TokenType.NE))
                {
                    ret = l != r;
                }
                else if (op.Match(TokenType.CONCAT))
                {
                    ret = ValueUtils.ToString(l) + ValueUtils.ToString(r);
                }
                else
                {
                    Debug.Assert(false);
                }
                
            }

            return new List<object>() { ret };
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

        protected override List<object> _GetResults(Frame frame)
        {
            object ret = null;
            if (op.Match('-'))
            {
                ret = exp.GetNumber(frame);
            }
            else if (op.Match(TokenType.NOT))
            {
                ret = !exp.GetBool(frame);
            }
            else
            {
                Debug.Assert(false);
            }
            return new List<object>() { ret };
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
        public string source_name;

        protected override List<object> _GetResults(Frame frame)
        {
            Function fn = new Function();
            fn.code = this;
            fn.vm = frame.func.vm;
            fn.module_table = frame.func.module_table;
            fn.upvalues = frame.GetAllUpvalues();
            return new List<object>(){ fn};
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

        protected override List<object> _GetResults(Frame frame)
        {
            Table ret = new Table();
            for (var i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                if(f.index == null && i == fields.Count - 1)
                {
                    var vs = f.value.GetResults(frame);
                    foreach(var v in vs)
                    {
                        ret.Set(++i, v);
                    }
                    break;
                }

                object key = f.index ? f.index.GetOneResult(frame) : i+1;
                if(key == null)
                {
                    throw frame.NewRunException(f.line, "Table key can not be nil");
                }
                ret.Set(key, f.value.GetOneResult(frame));
            }
            return new List<object>() { ret };
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

        protected override List<object> _GetResults(Frame frame)
        {
            var t = table.GetTable(frame);
            var idx = index.GetOneResult(frame);
            if (idx == null)
            {
                throw frame.NewRunException(index.line, "table index can not be null");
            }
            var ret = t.Get(idx);
            return new List<object>() { ret };
        }

        public void Write(Frame frame)
        {
            var t = table.GetTable(frame);
            var idx = index.GetOneResult(frame);
            if (idx == null)
            {
                throw frame.NewRunException(index.line, "table index can not be null");
            }
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

        protected override List<object> _GetResults(Frame frame)
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

        protected override List<object> _GetResults(Frame frame)
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

        protected override List<object> _GetResults(Frame frame)
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

        protected override List<object> _GetResults(Frame frame)
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

        protected override List<object> _GetResults(Frame frame)
        {
            return null;
        }
    }
}
