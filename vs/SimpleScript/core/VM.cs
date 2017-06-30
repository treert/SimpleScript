using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    /// <summary>
    /// 脚本虚拟机
    /// 1. 资源管理
    ///     1. 全局表
    ///     2. gc管理，new管理【现在完全没管这个】
    /// 2. 线程管理
    /// 3. 对外接口
    ///     1. dostring
    /// </summary>
    public class VM
    {
        public void DoString(string s)
        {
            var func = Parse(s);
            CallFunction(func);
        }

        public Function Parse(string source, string module_name = "")
        {
            _lex.Init(source, module_name);
            var tree = _parser.Parse(_lex);
            var func = _code_generator.Generate(tree);
            return func;
        }

        public object[] CallFunction(Function func, params object[] args)
        {
            var closure = NewClosure();
            closure.func = func;
            closure.env_table = m_global;

            return CallClosure(closure, args);
        }

        public object[] CallClosure(Closure closure, params object[] args)
        {
            var work_thread = GetWorkThread();

            work_thread.PushValue(closure);
            for (int i = 0; i < args.Length; ++i )
            {
                work_thread.PushValue(args[i]);
            }
            work_thread.Run();

            // get results
            int count = work_thread.GetTopIdx();
            object[] ret = new object[count];
            for (int i = 0; i < count; ++i )
            {
                ret[i] = work_thread.GetValue(i);
            }
            work_thread.Clear();

            PutWorkThread(work_thread);
            return ret;
        }

        public Table m_global;

        public void RegisterGlobalFunc(string name, CFunction cfunc)
        {
            m_global.SetValue(name,cfunc);
        }

        /*****************************************************************/
        public Table NewTable()
        {
            return new Table();
        }
        internal Closure NewClosure()
        {
            return new Closure();
        }
        /*****************************************************************/

        Lex _lex;
        Parser _parser;
        CodeGenerate _code_generator;
        Thread _thread;
        Stack<Thread> _other_threads;

        Thread GetWorkThread()
        {
            if(!_thread.IsRuning())
            {
                return _thread;
            }
            // 这样可以兼容宿主协程，因为不存在执行栈帧来回穿插的情况
            if(_other_threads.Count == 0)
            {
                return new Thread(this);
            }
            else
            {
                return _other_threads.Pop();
            }
        }

        void PutWorkThread(Thread th)
        {
            if(th != _thread)
            {
                _other_threads.Push(th);
            }
        }

        public VM()
        {
            _lex = new Lex();
            _parser = new Parser();
            _code_generator = new CodeGenerate();
            _thread = new Thread(this);
            _other_threads = new Stack<Thread>();

            m_global = NewTable();
        }
    }
}
