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
    /// 2. 线程管理（一个VM只维护一个thread）
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

            int func_idx = _thread.GetTopIdx();
            _thread.PushValue(func);
            _thread.Run(func_idx);
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

        public VM()
        {
            _lex = new Lex();
            _parser = new Parser();
            _code_generator = new CodeGenerate();
            _thread = new Thread(this);

            m_global = NewTable();
        }
    }
}
