using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript.Test
{
    abstract class TestBase
    {
        private List<string> _errors= new List<string>();
        private int _expect_true_count = 0;
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
            if(!bool_)
            {
                Error("the {0} expr is not true", _expect_true_count);
            }
        }

        public void Error(string err_msg,params object[] args)
        {
            _errors.Add(String.Format(err_msg,args));
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
            foreach(var t in types)
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
                if(test.IsOK())
                {
                    ++_pass_case;
                    Console.WriteLine("{0} pass", t.Name);
                }
                else
                {
                    Console.WriteLine("{0} failed:", t.Name);
                    var errors = test.GetErrors();
                    foreach(var error in errors)
                    {
                        Console.WriteLine("\t"+error);
                    }
                }
            }
            catch(LexException e)
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
