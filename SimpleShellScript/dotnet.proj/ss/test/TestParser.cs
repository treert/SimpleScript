using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleScript.Test
{
    class ASTFinder
    {
        // 利用反射，遍历字段来获取SyntaxTree
        public static T Find<T>(object root)where T:SyntaxTree
        {
            if (root == null) return null;
            if (root is T)
            {
                return root as T;
            }
            var fields = root.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            T ret = null;
            foreach(var f in fields)
            {
                if (f.FieldType.IsPrimitive || f.FieldType.IsEnum) continue;
                if (f.FieldType.IsArray)
                {
                    Array arr = f.GetValue(root) as Array;
                    foreach(var af in arr)
                    {
                        ret = Find<T>(af);
                        if (ret != null) return ret;
                    }
                }
                else
                {
                    ret = Find<T>(f.GetValue(root));
                    if (ret != null) return ret;
                }
            }
            return null;
        }
    }
    static class TestUtils
    {
        static Lex lex = new Lex();
        static Parser parser = new Parser();
        public static SyntaxTree Parse(string s)
        {
            lex.Init(s);
            return parser.Parse(lex);
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

    class TestParser_exp2 : TestBase
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

    class TestParser_exp3 : TestBase
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

    class TestParser_exp4 : TestBase
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
        }
    }
    class TestParser_parser2 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("fn f(a, b, c){ t.a, t.b, t.c = a, b, c return a, b, c; }");
        }
    }
    class TestParser_parser3 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("t = {['str'] = 1 ^ 2, abc = 'str' .. 2, id, 1 + 2;}");
        }
    }
    class TestParser_parser4 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("a = (1 + 2) * 3 / 4");
        }
    }
    class TestParser_parser5 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("local name");
        }
    }
    class TestParser_parser6 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("table[index] = 1");
        }
    }
    class TestParser_parser7 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("t.a.b.c = 1");
        }
    }
    class TestParser_parser8 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("f(a, b, c)");
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
            try
            {
                // do not support this, because it is easy to write wrong code
                TestUtils.Parse("f{1, 2, 3}");
                Error("no exception");
            }
            catch (ParserException) { }
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
            TestUtils.Parse("f(1, 2, 3).m({1, 2, 3}).m[123].m = 1");
        }
    }
    class TestParser_parser13 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("fn f() {}");
        }
    }
    class TestParser_parser14 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("fn f() { while true { return } }");
        }
    }
    class TestParser_parser15 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("fn f() { return (nil ? dosomething) }");
        }
    }
    class TestParser_parser16 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("fn f() { local fn f() {} local a, b, c = 1, 2 }");
        }
    }
    class TestParser_parser17 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("fn f() { fn a.b.c.d() { return } }");
        }
    }
    class TestParser_parser18 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("fn f() { for a = 1, 2, 3 { } for a, b in pairs(t) {} }");
        }
    }
    class TestParser_parser19 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("fn f(){ if 1 + 1 {} elseif not true {} else {} }");
        }
    }
    class TestParser_parser20 : TestBase
    {
        public override void Run()
        {
            // lua报异常，ss就不报。可以搞个警告。
            TestUtils.Parse("return return");
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
                // ss 不支持 f:call() 语法糖
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
                TestUtils.Parse("{} }");
                Error("no exception");
            }
            catch (ParserException) { }
            catch (LexException) { }
        }
    }
    class TestParser_parser24 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("while true, false {}");
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
                // ss不支持repeat语句，但是repeat可以当成简单变量名。
                TestUtils.Parse("repeat {} until a;");
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
                TestUtils.Parse("if true false {}");
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
                TestUtils.Parse("if true {} elseif false {} else");
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
                TestUtils.Parse("for a.b = 1, 2 { break }");
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
                TestUtils.Parse("for a = 1, 2, 3, 4 { continue }");
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
                TestUtils.Parse("for a.b in pairs(t) { }");
                Error("no exception");
            }
            catch (ParserException) { }
        }
    }

    class TestParser_for_loop : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("for {}");
        }
    }

    class TestParser_parser31 : TestBase
    {
        public override void Run()
        {
            try
            {
                TestUtils.Parse("fn a.b.c:m(){}");
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
                TestUtils.Parse("local fn a.b() {}");
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
                // 参数里加...的语法丢弃了，默认就有，不需要额外写
                TestUtils.Parse("local fn a(m, ...) {}");
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
                TestUtils.Parse("local a }");
                Error("no exception");
            }
            catch (ScriptException) { }
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

    class TestParser_fn_call_1 : TestBase
    {
        public override void Run()
        {
            TestUtils.Parse("t.f(a,b,c,*kw,a=a)");
        }
    }
}
