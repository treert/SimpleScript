using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    /// <summary>
    /// 资源管理
    /// 1. 加载解析源代码
    /// 2. 管理协程
    /// 3. 管理所有对象
    /// </summary>
    class VM
    {
        public void DoString(string s)
        {
            _lex.Init(s);
            var tree = _parser.Parse(_lex);
            var func = _code_generator.Generate(tree);
            var th = _GetThreadToUse();
            th.Reset(func);
            th.Resume();
        }

        public Table m_global = new Table();

        public void RegisterGlobalFunc(string name, CFunction cfunc)
        {
            m_global.SetValue(name,cfunc);
        }

        /*****************************************************************/

        

        LinkedList<Thread> _free_threads = new LinkedList<Thread>();
        HashSet<Thread> _used_threads = new HashSet<Thread>();
        Thread _GetThreadToUse()
        {
            Thread th = null;
            if(_free_threads.Count > 0)
            {
                th = _free_threads.First.Value;
                _free_threads.RemoveFirst();
            }
            else
            {
                th = new Thread(this);
            }
            _used_threads.Add(th);
            return th;
        }
        void _CollectThread(Thread th)
        {
            _free_threads.AddFirst(th);
        }

        Lex _lex = new Lex();
        Parser _parser = new Parser();
        CodeGenerate _code_generator = new CodeGenerate();
    }
}
