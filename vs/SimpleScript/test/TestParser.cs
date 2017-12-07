using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript.Test
{
    delegate void DoSomething();
    static class TestUtils
    {
        static Lex lex = new Lex();
        static Parser parser = new Parser();
        public static SyntaxTree Parse(string s)
        {
            lex.Init(s);
            return parser.Parse(lex);
        }
        public static bool IsEOF()
        {
            return lex.GetNextToken().m_type == (int)TokenType.EOS;
        }
    }
    interface ASTCheck
    {
        bool Check(SyntaxTree t);
    }

    abstract class SyntaxVisitor
    {
        protected void VisitAnySyntaxTree(SyntaxTree tree)
        {
            if (PreVisit(tree)) return;

            if (tree is Chunk)
                VisitChunk(tree as Chunk);
            else if (tree is Block)
                VisitBlock(tree as Block);
            else if (tree is ReturnStatement)
                VisitReturnStatement(tree as ReturnStatement);
            else if (tree is BreakStatement)
                VisitBreakStatement(tree as BreakStatement);
            else if (tree is ContinueStatement)
                VisitContinueStatement(tree as ContinueStatement);
            else if (tree is DoStatement)
                VisitDoStatement(tree as DoStatement);
            else if (tree is WhileStatement)
                VisitWhileStatement(tree as WhileStatement);
            else if (tree is IfStatement)
                VisitIfStatement(tree as IfStatement);
            else if (tree is ForStatement)
                VisitForStatement(tree as ForStatement);
            else if (tree is ForInStatement)
                VisitForInStatement(tree as ForInStatement);
            else if (tree is ForEachStatement)
                VisitForEachStatement(tree as ForEachStatement);
            else if (tree is FunctionStatement)
                VisitFunctionStatement(tree as FunctionStatement);
            else if (tree is FunctionName)
                VisitFunctionName(tree as FunctionName);
            else if (tree is LocalFunctionStatement)
                VisitLocalFunctionStatement(tree as LocalFunctionStatement);
            else if (tree is LocalNameListStatement)
                VisitLocalNameListStatement(tree as LocalNameListStatement);
            else if (tree is AssignStatement)
                VisitAssignStatement(tree as AssignStatement);
            else if (tree is SpecialAssginStatement)
                VisitSpecialAssginStatement(tree as SpecialAssginStatement);
            else if (tree is Terminator)
                VisitTerminator(tree as Terminator);
            else if (tree is BinaryExpression)
                VisitBinaryExpression(tree as BinaryExpression);
            else if (tree is UnaryExpression)
                VisitUnaryExpression(tree as UnaryExpression);
            else if (tree is FunctionBody)
                VisitFunctionBody(tree as FunctionBody);
            else if (tree is ParamList)
                VisitParamList(tree as ParamList);
            else if (tree is TableDefine)
                VisitTableDefine(tree as TableDefine);
            else if (tree is TableField)
                VisitTableField(tree as TableField);
            else if (tree is TableAccess)
                VisitTableAccess(tree as TableAccess);
            else if (tree is FuncCall)
                VisitFuncCall(tree as FuncCall);
            else if (tree is ExpressionList)
                VisitExpressionList(tree as ExpressionList);
            else if (tree is NameList)
                VisitNameList(tree as NameList);
            else
                throw new Exception("unsupport type " + tree.GetType().FullName);
        }

        protected abstract void VisitChunk(Chunk tree);
        protected abstract void VisitBlock(Block tree);
        protected abstract void VisitReturnStatement(ReturnStatement tree);
        protected abstract void VisitBreakStatement(BreakStatement tree);
        protected abstract void VisitContinueStatement(ContinueStatement tree);
        protected abstract void VisitDoStatement(DoStatement tree);
        protected abstract void VisitWhileStatement(WhileStatement tree);
        protected abstract void VisitIfStatement(IfStatement tree);
        protected abstract void VisitForStatement(ForStatement tree);
        protected abstract void VisitForInStatement(ForInStatement tree);
        protected abstract void VisitForEachStatement(ForEachStatement tree);
        protected abstract void VisitFunctionStatement(FunctionStatement tree);
        protected abstract void VisitFunctionName(FunctionName tree);
        protected abstract void VisitLocalFunctionStatement(LocalFunctionStatement tree);
        protected abstract void VisitLocalNameListStatement(LocalNameListStatement tree);
        protected abstract void VisitAssignStatement(AssignStatement tree);
        protected abstract void VisitSpecialAssginStatement(SpecialAssginStatement tree);
        protected abstract void VisitTerminator(Terminator tree);
        protected abstract void VisitBinaryExpression(BinaryExpression tree);
        protected abstract void VisitUnaryExpression(UnaryExpression tree);
        protected abstract void VisitFunctionBody(FunctionBody tree);
        protected abstract void VisitParamList(ParamList tree);
        protected abstract void VisitTableDefine(TableDefine tree);
        protected abstract void VisitTableField(TableField tree);
        protected abstract void VisitTableAccess(TableAccess tree);
        protected abstract void VisitFuncCall(FuncCall tree);
        protected abstract void VisitExpressionList(ExpressionList tree);
        protected abstract void VisitNameList(NameList tree);

        protected abstract bool PreVisit(SyntaxTree tree);
    }

    class ASTFinder : SyntaxVisitor
    {
        public static T Find<T>(SyntaxTree root, ASTCheck check = null) where T : SyntaxTree
        {
            var finder = new ASTFinder(root,typeof(T), check);
            return (T)finder.Find();
        }

        private SyntaxTree _root = null;
        private Type _type = null;
        private ASTCheck _check = null;

        private SyntaxTree _result = null;
        public ASTFinder(SyntaxTree root_,Type type_,ASTCheck check_ = null)
        {
            _root = root_;
            _type = type_;
            _check = check_;
        }
        public SyntaxTree Find()
        {
            VisitAnySyntaxTree(_root);
            return _result;
        }
        private void _TrySetResult(SyntaxTree t)
        {
            if(_type.IsInstanceOfType(t))
            {
                if (_check != null)
                {
                    if (_check.Check(t))
                    {
                        _result = t;
                        return;
                    }
                }
                else
                {
                    _result = t;
                    return;
                }
            }
        }

        protected override bool PreVisit(SyntaxTree tree)
        {
            // if has find, stop vist
            _TrySetResult(tree);
            return _result != null;
        }

        protected override void VisitChunk(Chunk tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.block);
        }
        protected override void VisitBlock(Block tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            foreach (var statement in tree.statements)
            {
                VisitAnySyntaxTree(statement);
            }
        }
        protected override void VisitReturnStatement(ReturnStatement tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

        }
        protected override void VisitBreakStatement(BreakStatement tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

        }
        protected override void VisitContinueStatement(ContinueStatement tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

        }
        protected override void VisitDoStatement(DoStatement tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.block);
        }
        protected override void VisitWhileStatement(WhileStatement tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.exp);
            VisitAnySyntaxTree(tree.block);
        }
        protected override void VisitIfStatement(IfStatement tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.exp);
            VisitAnySyntaxTree(tree.true_branch);
            if (tree.false_branch != null)
                VisitAnySyntaxTree(tree.false_branch);
        }
        protected override void VisitForStatement(ForStatement tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.exp1);
            VisitAnySyntaxTree(tree.exp2);
            if (tree.exp3 != null)
                VisitAnySyntaxTree(tree.exp3);
            VisitAnySyntaxTree(tree.block);
        }
        protected override void VisitForInStatement(ForInStatement tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.name_list);
            VisitAnySyntaxTree(tree.exp_list);
            VisitAnySyntaxTree(tree.block);
        }
        protected override void VisitForEachStatement(ForEachStatement tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.exp);
            VisitAnySyntaxTree(tree.block);
        }
        protected override void VisitFunctionStatement(FunctionStatement tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.func_name);
            VisitAnySyntaxTree(tree.func_body);
        }
        protected override void VisitFunctionName(FunctionName tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;
        }
        protected override void VisitLocalFunctionStatement(LocalFunctionStatement tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.func_body);
        }
        protected override void VisitLocalNameListStatement(LocalNameListStatement tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.name_list);
            VisitAnySyntaxTree(tree.exp_list);
        }
        protected override void VisitAssignStatement(AssignStatement tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            foreach (var exp in tree.var_list)
            {
                VisitAnySyntaxTree(exp);
            }
            VisitAnySyntaxTree(tree.exp_list);
        }
        protected override void VisitSpecialAssginStatement(SpecialAssginStatement tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.var);
            if(tree.exp != null)
            {
                VisitAnySyntaxTree(tree.exp);
            }
        }

        protected override void VisitTerminator(Terminator tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;
        }
        protected override void VisitBinaryExpression(BinaryExpression tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.left);
        }
        protected override void VisitUnaryExpression(UnaryExpression tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.exp);
        }
        protected override void VisitFunctionBody(FunctionBody tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            if(tree.param_list != null)
                VisitAnySyntaxTree(tree.param_list);
            VisitAnySyntaxTree(tree.block);
        }
        protected override void VisitParamList(ParamList tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

        }
        protected override void VisitTableDefine(TableDefine tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            foreach (var field in tree.fields)
            {
                VisitAnySyntaxTree(field);
            }
        }
        protected override void VisitTableField(TableField tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            if (tree.index != null)
                VisitAnySyntaxTree(tree.index);
            VisitAnySyntaxTree(tree.value);
        }
        protected override void VisitTableAccess(TableAccess tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.table);
            VisitAnySyntaxTree(tree.index);
        }
        protected override void VisitFuncCall(FuncCall tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            VisitAnySyntaxTree(tree.caller);
            VisitAnySyntaxTree(tree.args);
        }
        protected override void VisitExpressionList(ExpressionList tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            foreach (var exp in tree.exp_list)
            {
                VisitAnySyntaxTree(exp);
            }
        }
        protected override void VisitNameList(NameList tree)
        {
            _TrySetResult(tree);
            if (_result != null) return;

        }
    }
    class TestParser_exp1 : TestBase
    {
        public override void Run()
        {
            var root = TestUtils.Parse("a = 1 + 2 + 3");
            var exp_list = ASTFinder.Find<ExpressionList>(root);
            ExpectTrue(exp_list.exp_list.Count == 1);
            var bin_exp = exp_list.exp_list[0] as BinaryExpression;
            ExpectTrue(bin_exp.op.m_type == '+');

            // 1+2
            bin_exp = bin_exp.left as BinaryExpression;
            ExpectTrue(bin_exp.op.m_type == '+');
            var num = bin_exp.right as Terminator;
            ExpectTrue(num.token.m_number == 2 && num.token.m_type == (int)TokenType.NUMBER);
        }
    }

    class TestParser_exp2:TestBase
    {
        public override void Run()
        {
            var root = TestUtils.Parse("a = 1 + 2 + 3 * 4");
            var exp_list = ASTFinder.Find<ExpressionList>(root);
            ExpectTrue(exp_list.exp_list.Count == 1);

            // (1+2) + (3*4)
            var bin_exp = exp_list.exp_list[0] as BinaryExpression;
            ExpectTrue(bin_exp.op.m_type == '+');

            // 3*4
            bin_exp = bin_exp.right as BinaryExpression;
            ExpectTrue(bin_exp.op.m_type == '*');
            var num = bin_exp.right as Terminator;
            ExpectTrue(num.token.m_number == 4 && num.token.m_type == (int)TokenType.NUMBER);
        }
    }

    class TestParser_exp3:TestBase
    {
        public override void Run()
        {
            var root = TestUtils.Parse("a = 2+5*6*7 and 3");
            var exp_list = ASTFinder.Find<ExpressionList>(root);
            ExpectTrue(exp_list.exp_list.Count == 1);
            var bin_exp = exp_list.exp_list[0] as BinaryExpression;
            ExpectTrue(bin_exp.op.m_type == (int)TokenType.AND);

            // 2+2*2*2 
            bin_exp = bin_exp.left as BinaryExpression;
            ExpectTrue(bin_exp.op.m_type == '+');
            // (2*2)*2
            bin_exp = bin_exp.right as BinaryExpression;
            ExpectTrue(bin_exp.op.m_type == '*');
            var num = bin_exp.right as Terminator;
            ExpectTrue(num.token.m_number == 7 && num.token.m_type == (int)TokenType.NUMBER);
        }
    }

    class TestParser_exp4:TestBase
    {
        public override void Run()
        {
            var root = TestUtils.Parse("a = (-1 + 1) * 2 / 1 ^ 2 and 1 or 2");
            var exp_list = ASTFinder.Find<ExpressionList>(root);
            ExpectTrue(exp_list.exp_list.Count == 1);

            // or
            var bin_exp = exp_list.exp_list[0] as BinaryExpression;
            ExpectTrue(bin_exp.op.m_type == (int)TokenType.OR);
            ExpectTrue(bin_exp.right is Terminator);

            // and
            bin_exp = bin_exp.left as BinaryExpression;
            ExpectTrue(bin_exp.op.m_type == (int)TokenType.AND);

            // ((-1 + 1) * 2) / (1 ^ 2)
            bin_exp = bin_exp.left as BinaryExpression;
            ExpectTrue(bin_exp.op.m_type == '/');

            // 1^2
            var right = bin_exp.right as BinaryExpression;
            ExpectTrue(right.op.m_type == '^');

            // (-1 + 1) * 2
            var left = bin_exp.left as BinaryExpression;
            ExpectTrue(left.op.m_type == '*');
        }
    }
    class TestParser_parser1 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("a = -123 ^ 2 ^ -2 * 1 / 2 % 2 * 2 ^ 10 + 10 - 5 .. 'str' == 'str' and true or false or not not false");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser2 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("function f(a, b, c, ...) f(a, b, c); t.a, t.b, t.c = a, b, c return a, b, c; end");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser3 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("t = {['str'] = 1 ^ 2, abc = 'str' .. 2, id, 1 + 2;}");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser4 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("a = (1 + 2) * 3 / 4");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser5 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("local name");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser6 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("table[index] = 1");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser7 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("t.a.b.c = 1");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser8 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("f(a, b, c)");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser9 : TestBase
    {
        public override void Run()
        {
            try
            {
                // do not support this, because it is easy to write wrong code
                TestUtils.Parse("f:m()");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser10 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("f{1, 2, 3}");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser11 : TestBase
    {
        public override void Run()
        {
            try
            {
                // lua支持这种语法，ss暂时不支持
                TestUtils.Parse("f:m'str'");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser12 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("f(1, 2, 3).m{1, 2, 3}.m[123].m = 1");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser13 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("function f() do end end");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser14 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("function f() while true do return end end");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser15 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("function f() return (true and dosomething) end");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser16 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("function f() local function f() end local a, b, c = 1, 2 end");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser17 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("function f() function a.b.c.d() return end end");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser18 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("function f() for a = 1, 2, 3 do end for a, b in pairs(t) do end end");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser19 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("function f() if 1 + 1 then elseif not true then else end end");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser20 : TestBase
    {
        public override void Run()
        {
            // lua报异常，oms就不报。可以搞个警告。
            TestUtils.Parse("return return");
            ExpectTrue(TestUtils.IsEOF());
        }
    }
    class TestParser_parser21 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("t = {} t.a = ");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser22 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("f().m():m().m");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser23 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("do end end");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser24 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("while true, false do end");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser25 : TestBase
    {
        public override void Run()
        {
            try
            {
                // oms不支持repeat语句
                TestUtils.Parse("repeat until a;");
                // Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser26 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("if true false then end");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser27 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("if true then elseif false then else");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser28 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("for a.b = 1, 2 do break end");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser29 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("for a = 1, 2, 3, 4 do continue end");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser30 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("for a.b in pairs(t) do end");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser31 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("function a.b.c:m.c() end");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser32 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("local function a.b() end");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser33 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("local function a(m, ..., n) end");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser34 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("local a = 1, 2,");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser35 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("t = {a.b = 1}");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser36 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("local a end");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser37 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("(f + 1):xx{1,2}");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser38 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("foreach k,v in f.get() do end");
            ExpectTrue(TestUtils.IsEOF());
        }
    }

    class TestParser_parser39 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("foreach v in f do end");
            ExpectTrue(TestUtils.IsEOF());
        }
    }

    class TestParser_parser40 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("foreach v in f.get(),g do end");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
    class TestParser_parser41 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("foreach a,b,v in f do end");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }
}
