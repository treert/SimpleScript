using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MyScript
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
            return x.Count > 0 ? Utils.ToBool(x[0]) : false;
        }

        public string GetString(Frame frame)
        {
            var x = GetResults(frame);
            return x.Count > 0 ? Utils.ToString(x[0]) : "";
        }

        public double GetValidNumber(Frame frame)
        {
            var x = GetResults(frame);
            double f = Utils.ToNumber(x.GetValueOrDefault(0));
            // @om 这个接口做下double有效性判断
            if (double.IsNaN(f))
            {
                throw frame.NewRunException(line, "exp can not convert to valid double");
            }
            return f;
        }

        public double GetNumber(Frame frame)
        {
            var x = GetResults(frame);
            double f = Utils.ToNumber(x.GetValueOrDefault(0));
            return f;
        }

        public List<object> GetResults(Frame frame)
        {
            // @om 要不要做异常行号计算？
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
                if (Utils.ToBool(obj))
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
            if (Utils.ToBool(obj))
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
            var start = exp1.GetValidNumber(frame);
            var end = exp2.GetValidNumber(frame);
            if (start <= end)
            {
                double step = exp3 ? exp3.GetValidNumber(frame) : 1;
                if (step <= 0)
                {
                    throw frame.NewRunException(line, $"for step {step} should greater than 0, or will cause forerver loop");
                }
                var cur_block = frame.cur_block;
                for (double it = start; it <= end; it += step)
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
            else
            {
                double step = exp3 ? exp3.GetValidNumber(frame) : -1;
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

            var cur_block = frame.cur_block;
            if (obj is IForKeys)
            {
                var iter = obj as IForKeys;
                var keys = iter.GetKeys();
                foreach (var k in keys)
                {
                    frame.cur_block = cur_block;
                    try
                    {
                        frame.EnterBlock();
                        name_list.AddLocals(frame, k, iter.Get(k));
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
            else if (obj is Function)
            {
                for (; ; )
                {
                    var results = (obj as Function).Call();
                    if (results.GetValueOrDefault(0) != null)
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
            // 想了想，统一支持下 IEnumerate
            else if (obj is IEnumerable)
            {
                foreach (var a in (obj as IEnumerable))
                {
                    frame.cur_block = cur_block;
                    try
                    {
                        frame.EnterBlock();
                        name_list.AddLocals(frame, a);
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

    public class ThrowStatement : SyntaxTree
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
            catch (Exception e)
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
            if (func_name.names.Count == 1)
            {
                frame.Write(func_name.names[0].m_string, fn);
            }
            else
            {
                var names = func_name.names;
                var obj = frame.Read(names[0].m_string);
                if (obj == null)
                {
                    obj = frame.Write(names[0].m_string, new Table());
                }
                if (obj is Table == false)
                {
                    throw frame.NewRunException(line, $"{names[0].m_string} is not Table which expect to be");
                }
                Table t = obj as Table;
                for (int i = 1; i < names.Count - 1; i++)
                {
                    var tt = t.Get(names[i].m_string);
                    if (tt == null)
                    {
                        tt = t.Set(names[i].m_string, new Table());
                    }
                    if (tt is Table == false)
                    {
                        throw frame.NewRunException(names[i].m_line, $"expect {names[i].m_string} to be a IGetSet");
                    }
                    t = tt as Table;
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
            var results = Utils.EmptyResults;
            if (exp_list)
            {
                results = exp_list.GetResults(frame);
            }
            for (int i = 0; i < name_list.names.Count; i++)
            {
                var name = name_list.names[i];
                var obj = results.Count > i ? results[i] : null;
                if (is_global)
                {
                    frame.AddGlobalName(name.m_string);
                    if (results.Count > i)
                    {
                        frame.func.vm.global_table[name.m_string] = obj;
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
            for (int i = 0; i < var_list.Count; i++)
            {
                var it = var_list[i];
                object val = results.Count > i ? results[i] : null;
                if (it is TableAccess)
                {
                    // TableAccess
                    var access = it as TableAccess;
                    var table = access.table.GetOneResult(frame);
                    var idx = access.index.GetOneResult(frame);
                    if (idx == null)
                    {
                        throw frame.NewRunException(line, "table index can not be null");
                    }

                    ExtUtils.Set(table, idx, val);
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
            object table = null, idx = null, val;
            string name = null;
            // 读
            if (var is TableAccess)
            {
                var access = var as TableAccess;
                table = access.table.GetOneResult(frame);
                if (table == null)
                {
                    throw frame.NewRunException(access.table.line, "table can not be null when run TableAccess");
                }
                idx = access.index.GetOneResult(frame);
                if (idx == null)
                {
                    throw frame.NewRunException(access.index.line, "index can not be null when run TableAccess");
                }
                val = ExtUtils.Get(table, idx);
            }
            else
            {
                var ter = var as Terminator;
                Debug.Assert(ter.token.Match(TokenType.NAME));
                name = ter.token.m_string;
                val = frame.Read(name);
            }

            // 运算
            if (op == TokenType.CONCAT_SELF)
            {
                // .=
                string str = exp.GetString(frame);
                val = Utils.ToString(val) + str;
            }
            else
            {
                double delta = 1;
                if (exp != null)
                {
                    delta = exp.GetValidNumber(frame);
                }
                if (op == TokenType.DEC_ONE || op == TokenType.DEC_SELF)
                {
                    delta *= -1;
                }
                // @om 要不要检查下NaN
                val = Utils.ToNumber(val) + delta;
            }

            // 写
            if (var is TableAccess)
            {
                ExtUtils.Set(table, idx, val);
            }
            else
            {
                frame.Write(name, val);
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
            for (int i = 0; i < names.Count; i++)
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
            else if (token.Match(Keyword.NIL))
            {
                obj = null;
            }
            else if (token.Match(Keyword.TRUE))
            {
                obj = true;
            }
            else if (token.Match(Keyword.FALSE))
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
            return new List<object>() { obj };
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
            if (l is double == false)
            {
                throw frame.NewRunException(left.line, "expect bin_op left to be a number");
            }
            if (r is double == false)
            {
                throw frame.NewRunException(right.line, "expect bin_op right to be a number");
            }
            if (check_right_zero)
            {
                var t = (double)r;
                if (t == 0)
                {
                    throw frame.NewRunException(right.line, "bin_op right value is zero");
                }
            }
        }

        protected override List<object> _GetResults(Frame frame)
        {
            object ret = null;
            if (op.Match(Keyword.AND))
            {
                ret = left.GetBool(frame) && right.GetBool(frame);
            }
            else if (op.Match(Keyword.OR))
            {
                ret = left.GetBool(frame) || right.GetBool(frame);
            }
            else
            {
                if (op.Match('+'))
                {
                    ret = left.GetValidNumber(frame) + right.GetValidNumber(frame);
                }
                else if (op.Match('-'))
                {
                    ret = left.GetValidNumber(frame) - right.GetValidNumber(frame);
                }
                else if (op.Match('*'))
                {
                    ret = left.GetValidNumber(frame) * right.GetValidNumber(frame);
                }
                else if (op.Match('/'))
                {
                    ret = left.GetValidNumber(frame) / right.GetValidNumber(frame);
                }
                else if (op.Match('%'))
                {
                    ret = left.GetValidNumber(frame) % right.GetValidNumber(frame);
                }
                else if (op.Match('<'))
                {
                    var l = left.GetOneResult(frame);
                    var r = right.GetOneResult(frame);
                    ret = Utils.Compare(l, r) < 0;
                }
                else if (op.Match('>'))
                {
                    var l = left.GetOneResult(frame);
                    var r = right.GetOneResult(frame);
                    ret = Utils.Compare(l, r) > 0;
                }
                else if (op.Match(TokenType.LE))
                {
                    var l = left.GetOneResult(frame);
                    var r = right.GetOneResult(frame);
                    ret = Utils.Compare(l, r) <= 0;
                }
                else if (op.Match(TokenType.GE))
                {
                    var l = left.GetOneResult(frame);
                    var r = right.GetOneResult(frame);
                    ret = Utils.Compare(l, r) >= 0;
                }
                else if (op.Match(TokenType.EQ))
                {
                    var l = left.GetOneResult(frame);
                    var r = right.GetOneResult(frame);
                    ret = Utils.CheckEquals(l, r);
                }
                else if (op.Match(TokenType.NE))
                {
                    var l = left.GetOneResult(frame);
                    var r = right.GetOneResult(frame);
                    ret = !Utils.CheckEquals(l, r);
                }
                else if (op.Match(TokenType.CONCAT))
                {
                    ret = left.GetString(frame) + right.GetString(frame);
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
                ret = exp.GetValidNumber(frame);
            }
            else if (op.Match(Keyword.NOT))
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
            return new List<object>() { fn };
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
                if (f.index == null && i == fields.Count - 1)
                {
                    var vs = f.value.GetResults(frame);
                    foreach (var v in vs)
                    {
                        ret.Set(++i, v);
                    }
                    break;
                }

                object key = f.index ? f.index.GetOneResult(frame) : i + 1;
                if (key == null)
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

    // todo 这儿可以做一个优化: a.b.c.d = 1，连着创建2个Table。实际使用时，可能很方便。
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
            var t = table.GetOneResult(frame);
            var idx = index.GetOneResult(frame);
            if (idx == null)
            {
                throw frame.NewRunException(index.line, "table index can not be null");
            }
            var ret = ExtUtils.Get(t, idx);
            return new List<object>() { ret };
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
            ICall func = caller.GetOneResult(frame) as ICall;
            if (func == null)
            {
                throw frame.NewRunException(caller.line, "expect fn or ext_fn to call");
            }

            var args = this.args.GetArgs(frame);
            object that = caller is TableAccess ? (caller as TableAccess).table : null;

            return func.Call(args);
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

        public Args GetArgs(Frame frame)
        {
            Args args = new Args();
            for (int i = 0; i < exp_list.Count - 1; i++)
            {
                args.args.Add(exp_list[i].GetOneResult(frame));
            }
            if (exp_list.Count > 0)
            {
                var rets = exp_list.Last().GetResults(frame);
                args.args.AddRange(rets);
            }
            if (kw_table != null)
            {
                Table table = kw_table.GetOneResult(frame) as Table;
                if (table != null)
                {
                    // todo
                }
                // 要不要报个错
            }
            foreach (var kw in kw_exp_list)
            {
                var val = kw.w.GetOneResult(frame);
                args.name_args[kw.k.m_string] = val;
            }
            return args;
        }
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
            List<object> list = new List<object>(exp_list.Count);
            for (int i = 0; i < exp_list.Count - 1; i++)
            {
                list.Add(exp_list[i].GetOneResult(frame));
            }
            if (exp_list.Count > 0)
            {
                var rets = exp_list.Last().GetResults(frame);
                list.AddRange(rets);
            }
            return list;
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
            StringBuilder sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.Append(item.GetString(frame));
            }
            if (is_shell)
            {
                // todo
                throw new NotImplementedException();
            }
            else
            {
                return new List<object>() { sb.ToString() };
            }
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
            var obj = exp.GetOneResult(frame);
            string str = Utils.ToString(obj, format, len);
            return new List<object>() { str };
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

            if (c == null)
            {
                var aa = a.GetResults(frame);
                if (aa.Count == 0 || aa[0] == null)
                {
                    return b.GetResults(frame);
                }
                else
                {
                    return aa;
                }
            }
            else
            {
                var aa = a.GetBool(frame);
                return aa ? b.GetResults(frame) : c.GetResults(frame);
            }
        }
    }
}
