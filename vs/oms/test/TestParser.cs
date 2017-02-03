using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms.test
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
    class ASTFinder:Visitor
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
            object tmp = null;
            _root.Accept(this, ref tmp);
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
        public void Visit(Chunk tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.block.Accept(this, ref data);
        }
        public void Visit(Block tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            foreach(var statement in tree.statements)
            {
                statement.Accept(this, ref data);
            }
        }
        public void Visit(ReturnStatement tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;
        }
        public void Visit(BreakStatement tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;
        }
        public void Visit(ContinueStatement tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;
        }
        public void Visit(DoStatement tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.block.Accept(this, ref data);
        }
        public void Visit(WhileStatement tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.exp.Accept(this, ref data);
            tree.block.Accept(this, ref data);
        }
        public void Visit(IfStatement tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.exp.Accept(this, ref data);
            tree.true_branch.Accept(this, ref data);
            if (tree.false_branch != null)
                tree.false_branch.Accept(this, ref data);
        }
        public void Visit(ForStatement tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.exp1.Accept(this, ref data);
            tree.exp2.Accept(this, ref data);
            if (tree.exp3 != null)
                tree.exp3.Accept(this, ref data);
            tree.block.Accept(this, ref data);
        }
        public void Visit(ForInStatement tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.name_list.Accept(this, ref data);
            tree.exp_list.Accept(this, ref data);
            tree.block.Accept(this, ref data);
        }
        public void Visit(ForEachStatement tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.exp.Accept(this, ref data);
            tree.block.Accept(this, ref data);
        }
        public void Visit(FunctionStatement tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.func_name.Accept(this, ref data);
            tree.func_body.Accept(this, ref data);
        }
        public void Visit(FunctionName tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;
        }
        public void Visit(LocalFunctionStatement tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.func_body.Accept(this, ref data);
        }
        public void Visit(LocalNameListStatement tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.name_list.Accept(this, ref data);
            tree.exp_list.Accept(this, ref data);
        }
        public void Visit(AssignStatement tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.var_list.Accept(this, ref data);
            tree.exp_list.Accept(this, ref data);
        }
        public void Visit(VarList tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            foreach(var var_ in tree.var_list)
            {
                var_.Accept(this, ref data);
            }
        }
        public void Visit(Terminator tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;
        }
        public void Visit(BinaryExpression tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.left.Accept(this, ref data);
        }
        public void Visit(UnaryExpression tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.exp.Accept(this, ref data);
        }
        public void Visit(FunctionBody tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.param_list.Accept(this, ref data);
            tree.block.Accept(this, ref data);
        }
        public void Visit(ParamList tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;
        }
        public void Visit(TableDefine tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            foreach(var field in tree.fields)
            {
                field.Accept(this, ref data);
            }
        }
        public void Visit(TableIndexField tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.index.Accept(this, ref data);
            tree.value.Accept(this, ref data);
        }
        public void Visit(TableNameField tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.value.Accept(this, ref data);
        }
        public void Visit(TableArrayField tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.value.Accept(this, ref data);
        }
        public void Visit(IndexAccessor tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.table.Accept(this, ref data);
            tree.index.Accept(this, ref data);
        }
        public void Visit(MemberAccessor tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.table.Accept(this, ref data);
        }
        public void Visit(NormalFuncCall tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.caller.Accept(this, ref data);
            tree.args.Accept(this, ref data);
        }
        public void Visit(MemberFuncCall tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            tree.caller.Accept(this, ref data);
            tree.args.Accept(this, ref data);
        }
        public void Visit(ExpressionList tree, ref object data)
        {
            _TrySetResult(tree);
            if (_result != null) return;

            foreach(var exp in tree.exp_list)
            {
                exp.Accept(this, ref data);
            }
        }
        public void Visit(NameList tree, ref object data)
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
            var root = TestUtils.Parse("a = 1 + 2 ^ 3 ^ 4");
            var exp_list = ASTFinder.Find<ExpressionList>(root);
            ExpectTrue(exp_list.exp_list.Count == 1);
            var bin_exp = exp_list.exp_list[0] as BinaryExpression;
            ExpectTrue(bin_exp.op.m_type == '+');

            // (2)^(3^4)
            bin_exp = bin_exp.right as BinaryExpression;
            ExpectTrue(bin_exp.op.m_type == '^');
            // 3^4
            bin_exp = bin_exp.right as BinaryExpression;
            ExpectTrue(bin_exp.op.m_type == '^');
            var num = bin_exp.left as Terminator;
            ExpectTrue(num.token.m_number == 3 && num.token.m_type == (int)TokenType.NUMBER);
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
            TestUtils.Parse("a = -123 ^ 2 ^ -2 * 1 / 2 % 2 * 2 ^ 10 + 10 - 5 .. 'str' == 'str' and true or false");
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
            TestUtils.Parse("f:m()");
            ExpectTrue(TestUtils.IsEOF());
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
                // lua支持这种语法，oms暂时不支持
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
            TestUtils.Parse("f(1, 2, 3):m{1, 2, 3}.m[123].m = 1");
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
            TestUtils.Parse("function f() repeat return until (1 + 1) end");
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
            TestUtils.Parse("function f() function a.b.c:d() return end end");
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
            // lua报异常，oms就不报 
            TestUtils.Parse("f().m():m().m");
            ExpectTrue(TestUtils.IsEOF());
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
                TestUtils.Parse("for a.b = 1, 2 do end");
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
                TestUtils.Parse("for a = 1, 2, 3, 4 do end");
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
            TestUtils.Parse("f 1");
            ExpectTrue(TestUtils.IsEOF());
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
