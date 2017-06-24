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
            _lex.Init(s);
            var tree = _parser.Parse(_lex);
            var func = _code_generator.Generate(tree);
            func.SetEnv("", m_global);// should set environment first 

            if(_thread.IsRuning())
            {
                // 这样可以兼容宿主协程，因为不存在执行栈帧来回穿插的情况
                var work_thread = GetOtherThread();
                work_thread.PushValue(func);
                work_thread.Run();
                work_thread.Clear();
                PutOtherThread(work_thread);
            }
            else
            {
                _thread.PushValue(func);
                _thread.Run();
                _thread.Clear();
            }
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

        /*****************************************************************/

        Lex _lex;
        Parser _parser;
        CodeGenerate _code_generator;
        Thread _thread;
        Stack<Thread> _other_threads;

        Thread GetOtherThread()
        {
            if(_other_threads.Count == 0)
            {
                return new Thread(this);
            }
            else
            {
                return _other_threads.Pop();
            }
        }

        void PutOtherThread(Thread th)
        {
            _other_threads.Push(th);
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
