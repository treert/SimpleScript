using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript.Test
{
    abstract class TestBase
    {
        private List<string> _errors = new List<string>();
        int _expect_true_count = 0;
        int _parse_count = 0;
        public List<string> GetErrors()
        {
            return _errors;
        }
        public bool IsOK()
        {
            return _errors.Count == 0;
        }
        public void ExpectTrue(bool bool_)
        {
            ++_expect_true_count;
            if (!bool_)
            {
                Error($"the {_expect_true_count} expr is not true");
            }
        }
        /// <summary>
        /// 放在这儿似乎不妥，呃
        /// </summary>
        /// <param name="source"></param>
        public void CanParse(string source)
        {
            ++_parse_count;
            try
            {
                TestUtils.Parse(source);
            }
            catch (ParserException)
            {
                Error($"the {_parse_count} parse failed");
            }
        }

        public void CanNotParse(string source)
        {
            ++_parse_count;
            try
            {
                TestUtils.Parse(source);
                Error($"the {_parse_count} parse has no exception");
            }
            catch (ParserException)
            {
                
            }
        }

        public void Error(string err_msg)
        {
            _errors.Add(err_msg);
        }

        public abstract void Run();
    }

    public class TestManager
    {
        public static void RunTest()
        {
            _total_case = _pass_case = 0;
            var assembly = typeof(TestManager).Assembly;
            var types = assembly.GetTypes();
            var base_type = typeof(TestBase);
            foreach (var t in types)
            {
                if (t.IsSubclassOf(base_type))
                {
                    _TestOne(t);
                }
            }
            Console.WriteLine("{0} cases: {1} passed, {2} failed",
                _total_case, _pass_case, _total_case - _pass_case);
        }
        private static int _total_case;
        private static int _pass_case;
        private static void _TestOne(Type t)
        {
            ++_total_case;
            try
            {
                var test = Activator.CreateInstance(t) as TestBase;
                test.Run();
                if (test.IsOK())
                {
                    ++_pass_case;
                    //Console.WriteLine("{0} pass", t.Name);
                }
                else
                {
                    Console.WriteLine("{0} failed:", t.Name);
                    var errors = test.GetErrors();
                    foreach (var error in errors)
                    {
                        Console.WriteLine("\t" + error);
                    }
                }
            }
            catch (LexException e)
            {
                Console.WriteLine("{0} catch lex exception {1}", t.Name, e.Message);
            }
            catch (ParserException e)
            {
                Console.WriteLine("{0} catch parser exception {1}", t.Name, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} catch exception {1}", t.Name, e);
            }
        }
    }
}
