using MyScript.Test;
using MyScript;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Numerics;
using System.Diagnostics;
using MyScriptTest;
using System.Runtime.InteropServices;

static class ExtClass
{
    public static int ExF(this int a, int b)
    {
        a += 1;
        return a + b;
    }
}

class TestA
{
    public int a;
    public TestA(int a)
    {
        this.a = a;
    }

    [MyScript.ExtField]
    public int this[int idx, string x1, bool x2]
    {
        get
        {
            return idx + a;
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        TestManager.RunTest();

        TestMyScript();

        //TestMyNumber();

        //TestDouble();

        //TestEnum();

        //TestStringFormat();

        //TestThread.Test1();

        //TestTypeCast();

        //TestDelegate();

        //TestStruct();

        TestIO();
    }

    /// <summary>
    /// 一些结论：
    /// 1. Directory.GetFiles 的结果的前缀和传入的dir是一模一样的。
    /// </summary>
    static void TestIO()
    {
        Console.WriteLine("============== TestIO Start ===============");
        //Console.WriteLine(System.IO.Directory.GetFiles(@"").FirstOrDefault());// exception
        Console.WriteLine(System.IO.Directory.GetFiles(@"C:\").FirstOrDefault());
        Console.WriteLine(System.IO.Directory.GetFiles(@"./").FirstOrDefault());
        Console.WriteLine(System.IO.Directory.GetFiles(@".///").FirstOrDefault());// ? 返回路径包含了.///
        Console.WriteLine(System.IO.Directory.GetFiles(@".").FirstOrDefault());
        Console.WriteLine(System.IO.Directory.GetFiles(@"./bin\.././").FirstOrDefault());
        Console.WriteLine(System.IO.Path.GetExtension(""));
        Console.WriteLine(System.IO.Path.GetExtension(null));
        Console.WriteLine(System.IO.Path.GetFileName(null));
        Console.WriteLine(System.IO.Path.GetDirectoryName(null));
        Console.WriteLine($"{null}");
        Console.WriteLine(string.Join(",",null,"",null));
        Console.WriteLine(System.IO.Path.GetExtension("a.b.c.1"));
        Console.WriteLine("========  Dir  ========");
        Console.WriteLine(System.IO.Directory.GetDirectories(@"C:/").FirstOrDefault());
        Console.WriteLine(System.IO.Directory.GetDirectories(@"c:\").FirstOrDefault());
        Console.WriteLine(System.IO.Directory.GetDirectories(@".").FirstOrDefault());
        //Console.WriteLine(System.IO.Directory.GetFiles(@"xyzd").FirstOrDefault());// exception
        Console.WriteLine("============== TestIO Start ===============");
    }

    static void TestStringFormat()
    {
        Console.WriteLine($@"
{string.Format("{0:000000}", 1234), -10}b
{string.Format("{0: 哈哈 0#0####}", 1234)}
{1.5:哈哈 \n .00}
{(1.5).ToString("哈哈 .00")}
");
    }
    enum EA
    {
        A = -1,
        B = 1,
        C = int.MaxValue,
    }
    enum EB : long
    {
        A = -1,
        B = 1,
        C = int.MaxValue,
    }
    enum EC : short
    {
        A = -1,
        B = 1,
        C = short.MaxValue,
    }
    [Flags]
    enum EE:uint
    {
        A = 1,
        B = 2,
        C = 4,
    }
    static void TestEnum()
    {
        Console.WriteLine("============== TestEnum Start ===============");
        //Console.WriteLine(Enum.IsDefined(typeof(EE), 1L));// fxxk, enum内置类型不对也不行，这TM是个判断函数呀，ToObject都不报错。。
        Console.WriteLine(Enum.IsDefined(typeof(EE), 7u));
        EE e = EE.A | EE.B | EE.C;
        Console.WriteLine(Enum.IsDefined(typeof(EE), e));
        Console.WriteLine(typeof(EE).IsEnumDefined(e));
        Console.WriteLine($"{e} {(int)e}");
        e = (EE)Enum.ToObject(typeof(EE), 3);
        Console.WriteLine($"{e} {(int)e}");
        e = (EE)Enum.ToObject(typeof(EE), 9);
        Console.WriteLine($"{e} {(int)e}");
        //e = (EE)Enum.ToObject(typeof(EE), 9.3);// 异常
        //Console.WriteLine($"{e} {(int)e}");
        e = (EE)Enum.ToObject(typeof(EE), 9999999999999999999);
        Console.WriteLine($"{e} {(uint)e} {Convert.ToUInt64(e)}");

        Console.WriteLine($"{Enum.Parse(typeof(EE),"A" )}");
        Console.WriteLine($"{Enum.Parse(typeof(EE), "A,B")}");
        Console.WriteLine($"{Enum.Parse(typeof(EE), "3")}");
        Console.WriteLine($"{Enum.Parse(typeof(EE), "33")}");
        //Console.WriteLine($"{Enum.Parse(typeof(EE), "A,B,D")}");// error
        Console.WriteLine($"{Enum.Parse(typeof(EE), "B,A")}");
        Console.WriteLine("============== TestEnum End ===============");
    }
    static void TestDouble()
    {
        Console.WriteLine("============== TestDouble Start ===============");
        double a = double.PositiveInfinity;
        double b = 0.0;
        double c = -0.0;
        unsafe
        {
            Console.WriteLine(Convert.ToString(*(long*)&a, 2));
            Console.WriteLine(Convert.ToString(*(long*)&b, 2));
            Console.WriteLine(Convert.ToString(*(long*)&c, 2));
        }
        Console.WriteLine(0.0 == -0.0);
        Console.WriteLine(b == c);
        Console.WriteLine(" int & double convert");
        double fa = 1.5e100;
        Console.WriteLine(((int)fa).ToString("x"));
        Console.WriteLine(((long)fa).ToString("x"));
        Console.WriteLine(((uint)fa).ToString("x"));
        Console.WriteLine(((ulong)fa).ToString("x"));
        Console.WriteLine("num == Math.Floor(num) VS num == Math.Floor(num)");
        Stopwatch sw = new Stopwatch();
        long cnt = 100000000 * 1;
        int okcnt = 0;
        double num = 1;
        sw.Restart();
        for(long i = 0L; i <cnt; i++)
        {
            if(num == Math.Floor(num))
            {
                okcnt++;
            }
            num += 10000000000000.1;
        }
        sw.Stop();
        Console.WriteLine($"num={num} long={(long)num} okcnt={okcnt} cost {sw.ElapsedMilliseconds}");// 411
        num = 1;
        okcnt = 0;
        sw.Restart();
        for (long i = 0L; i < cnt; i++)
        {
            if (num == (long)(num))
            {
                okcnt++;
            }
            num += 10000000000000.1;
        }
        sw.Stop();
        // 蛋疼，结果不一样哟。
        Console.WriteLine($"num={num} long={(long)num} okcnt={okcnt} cost {sw.ElapsedMilliseconds}");// 241
        {
            long ii = unchecked((long)1.5e100);
            double dd = (double)ii;
            Console.WriteLine(ii.ToString("x"));
            Console.WriteLine(dd);
            Console.WriteLine(dd == (long)dd);
            double d2 = 1.5e100;
            Console.WriteLine(unchecked((long)1.5e100).ToString("x"));
            Console.WriteLine(((long)d2).ToString("x"));
            Console.WriteLine(unchecked((ulong)1.5e100).ToString("x"));
            Console.WriteLine(((ulong)d2).ToString("x"));
        }

        Console.WriteLine("============== TestDouble End ===============");
    }

    [StructLayout(LayoutKind.Explicit)]
    struct BadStruct
    {
        [FieldOffset(0)]
        public bool i;  //1Byte
        [FieldOffset(0)]
        public double c;//8byte
        [FieldOffset(0)]
        public bool b;  //1byte
    }

    static void TestStruct()
    {
        Console.WriteLine("============== TestStruct Start ===============");
        unsafe
        {
            
            Console.WriteLine(sizeof(BadStruct));
        }

        Console.WriteLine(System.Text.Encoding.UTF32.Preamble.Length);
         
        Console.WriteLine("============== TestStruct End ===============");
    }

    static void TestMyScript()
    {
        Console.WriteLine("============== Start Test MS ===============");
        VM vm = new VM();
        // 注入基础扩展
        vm.global_table["echo"] = new MyConsole();
        // 执行测试文件
        try
        {
            vm.DoFile("test.ms");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error Happen: {e.Message}");
        }
        Console.WriteLine("================ Test MS End ===============");
    }

    static void TestTypeCast()
    {
        Console.WriteLine("===== TestTypeCast Start =======");
        Console.WriteLine(typeof(List<object>).IsAssignableFrom(typeof(List<MyFunction>)));
        Console.WriteLine(typeof(List<object>).IsAssignableFrom(typeof(List<int>)));
        Console.WriteLine(typeof(List<object>).IsAssignableFrom(typeof(List<DateTime>)));
        Console.WriteLine(typeof(List<object>).IsAssignableFrom(typeof(List<BigInteger>)));
        Console.WriteLine(typeof(object[]));
        Console.WriteLine(typeof(object[]).IsAssignableFrom(typeof(int[])));
        Console.WriteLine(typeof(int[]).IsAssignableFrom(typeof(uint[])));// True
        Console.WriteLine(typeof(int[]).IsAssignableFrom(typeof(short[])));
        Console.WriteLine(typeof(List<>).IsAssignableFrom(typeof(List<int>)));// False
        Console.WriteLine(typeof(List<>).IsAssignableFrom(typeof(List<object>)));// False
        Console.WriteLine(typeof(List<SyntaxTree>).IsAssignableFrom(typeof(List<ExpSyntaxTree>)));// False
        Console.WriteLine(typeof(System.Collections.IList).IsAssignableFrom(typeof(List<object>)));
        Console.WriteLine("===== TestTypeCast End =======");
    }

    static void TestDelegate()
    {
        Console.WriteLine("===== TestDelegate End =======");
        Action da = () => { Console.WriteLine("A"); };
        Action db = () => { Console.WriteLine("B"); };
        (da + db)();
        var d1 = da + db + da + db;
        (d1 - da)();

        Console.WriteLine("===== TestDelegate End =======");
    }

    static void TestMyNumber()
    {
        Console.WriteLine("===== Test MyNumber Start =======");

        MyNumber a = 1;
        MyNumber b = 2;
        MyNumber c = 2;
        Dictionary<object, string> map = new Dictionary<object, string>()
            {
                {a,"a" },
                {b,"b" },
            };
        Console.WriteLine(map[a]);
        Console.WriteLine(map[b]);
        Console.WriteLine(map[c]);

        var aaa = a;


        Console.WriteLine(a++);
        Console.WriteLine(++a);
        Console.WriteLine(a);
        Console.WriteLine(aaa);

        Console.WriteLine(" BigInterger ");

        BigInteger b_a = 1;
        var aab = b_a;
        Console.WriteLine(b_a++);
        Console.WriteLine(++b_a);
        Console.WriteLine(b_a);
        Console.WriteLine(aab);

        Console.WriteLine(" Double ");
        Console.WriteLine($"3.4 % 2 = {3.4 % 2}");
        Console.WriteLine($"-3.4 % 2 = {-3.4 % 2}");
        Console.WriteLine($"3.4 % -2 = {3.4 % -2}");
        Console.WriteLine($"-3.4 % -2 = {-3.4 % -2}");
        Console.WriteLine($"-3.4 % 0 = {-3.4 % 0}");

        Console.WriteLine("===== Test MyNumber End =======");
    }

    static void OtherTest()
    {
        {
            Func<List<int>> f = () =>
            {
                Console.WriteLine("ff");
                return new List<int>() { 1, 2 };
            };

            Func<int> g = () =>
            {
                Console.WriteLine("gg");
                return 0;
            };


            Dictionary<object, object> dic = new Dictionary<object, object>();

            double a = 1.2;
            double b = 1.2;

            object oa = a;
            object ob = b;

            try
            {
                throw new MyScript.ScriptException();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine($"{oa == ob}");

            dic[a] = "12";
            Console.WriteLine(dic[ob]);

            Console.WriteLine(f()[g()]);

            ulong xx = (1uL << 63) + 1;
            Console.WriteLine(xx);
            Console.WriteLine((double)xx);
        }


        {
            double dd = 1.0 / 0.0;
            Console.WriteLine(dd);
            Console.WriteLine(double.IsNormal(double.Epsilon * 2.0e100));
            Console.WriteLine(double.IsNegativeInfinity(dd));
            Console.WriteLine(double.NaN > dd);
            Console.WriteLine(double.NaN <= dd);
            Console.WriteLine(MyScript.Utils.ToNumber(".1e10"));
            Console.WriteLine(MyScript.Utils.ToNumber("0.23E+3"));
            Console.WriteLine(MyScript.Utils.ToNumber(null));
            Console.WriteLine(MyScript.Utils.ToNumber(new object()));
            Console.WriteLine(MyScript.Utils.ToNumber('a'));
            Console.WriteLine(MyScript.Utils.ToNumber(""));
            Console.WriteLine(MyScript.Utils.ToNumber((byte)3));
            Console.WriteLine((double)(byte)3);

            Console.WriteLine((int)-2.5);
        }
        {
            Console.WriteLine("22222222222222222");
            var f = typeof(ExtClass).GetMethod("ExF");
            int a = 1;
            a.ExF(2);
            Console.WriteLine(a);
            Console.WriteLine(f.Invoke(null, new object[] { a, 2 }));
            Console.WriteLine(a);
            //Console.WriteLine(f.Invoke(a, new object[] { 2 }));
            //Console.WriteLine(a);
            Console.WriteLine((int)'✘');
            Console.WriteLine((int)'9');
        }
        {
            Console.WriteLine("1111111111111111111111");
            object a = (double)1;
            object b = (double)1;
            object bb = (int)1;
            Dictionary<object, int> d = new Dictionary<object, int>();
            d[a] = 1;
            d[b] = 2;
            Console.WriteLine(a == bb);
            Console.WriteLine(a.Equals(bb));
            d[bb] = 3;
            Console.WriteLine(a == b);
            Console.WriteLine(a.Equals(b));
            Console.WriteLine(d[a]);
            ulong c = 12345678901234567890L;
            double e = 12345678901234567891L;
            long cc = (long)e;
            Console.WriteLine(e.ToString("f1"));
            Console.WriteLine(c.ToString("f1"));
            Console.WriteLine(c == e);
            Console.WriteLine(cc == e);
            Console.WriteLine(double.TryParse("1234567891234567891234123123", out e));
        }
        {
            Console.WriteLine("1111111111111111");
            var d = new Dictionary<int, int>();
            d[1] = 2;
            d[2] = 3;
            object a = 1;
            System.Collections.IDictionary c = d;
            c[a] = 11;
            object eee = 1.2;
            //c[eee] = 333;
            Console.WriteLine(c[a]);
            a = 3;
            Console.WriteLine(c[a]);
            double h = double.NaN;
            int j = (int)h;
            Console.WriteLine((int)h);
            Console.WriteLine(j == h);

            int[,] aa = new int[1, 1];
            Array arr = (Array)aa;
            Console.WriteLine(aa.GetType());
            Console.WriteLine(arr.GetType());
            Console.WriteLine(aa is Array);
        }
        {
            Console.WriteLine("2222222222222222");
            List<Type> types = new List<Type>(){
                typeof(sbyte),typeof(byte),
                typeof(Int16),typeof(UInt16),
                typeof(Int32),typeof(UInt32),
                typeof(float),typeof(decimal),
                typeof(double),
                typeof(char),
            };
            double a = 127;
            foreach (var t in types)
            {
                Console.WriteLine(MyScript.ExtWrap.CheckAndConvertFromSSToSS(a, t));
            }
        }
        {
            var type = typeof(ETest);
            ETest a = ETest.A;
            object b = a;
            object c = 0;
            Console.WriteLine($"{type.IsEnum} {type.IsValueType}");
            Console.WriteLine(type.IsInstanceOfType(b));
            Console.WriteLine(type.IsInstanceOfType(c));
            var d = Enum.ToObject(type, 0);
            Console.WriteLine(d);
            Console.WriteLine(Enum.IsDefined(type, 1));
            Console.WriteLine(Enum.IsDefined(type, "0"));
            Console.WriteLine(Enum.IsDefined(type, 0));
        }
        {
            var type = typeof(TestA);
            var ctors = type.GetConstructors();
            Console.WriteLine(ctors.Length);
            var a = (TestA)ctors[0].Invoke(new object[] { 2 });
            Console.WriteLine(a.a);
            Console.WriteLine($"null={null}.");
        }
        {
            Console.WriteLine(Utils.CheckEquals(1, 1.0));
            Console.WriteLine(Utils.CheckEquals(1f, 1.0));
        }
        {
            Console.WriteLine($"{"abc",4}#{"abe",-4}#");
        }
    }
    enum ETest
    {
        A
    }
}