using MyScript.Test;
using MyScript;
using System;
using System.Collections.Generic;
using System.Reflection;

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
        Console.WriteLine("Hello World!");
        TestManager.RunTest();

        {
            Console.WriteLine("Start Test MS 1.0");
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
                Console.WriteLine($"Error Happen\n{e.Message}");
            }
            Console.WriteLine("End");
        }
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
            List<object> a = new List<object>() { 1, 2, 3 };
            Console.WriteLine($"={a.GetValueOrDefault(1)}");
            a = null;
            Console.WriteLine($"={a.GetValueOrDefault(1)}");
        }
        {
            Console.WriteLine(Utils.Compare(1, 1.0));
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