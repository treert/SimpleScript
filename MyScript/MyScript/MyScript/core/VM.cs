using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    /// <summary>
    /// 管理各个Module
    /// 管理全局表
    /// 
    /// 维护一些用户接口
    /// </summary>
    public class VM
    {
        public readonly Table global_table = new Table();
        public readonly Dictionary<string, Dictionary<string, object>> modules = new Dictionary<string, Dictionary<string, object>>();
        public readonly Lex lex = new Lex();
        public readonly Parser parser = new Parser();

        public Table DoString(string str)
        {
            var f = Parse(str);
            var ret = InitModule(f);
            return ret;
        }

        public Table DoFile(string file)
        {
            return DoString(File.ReadAllText(file));
        }

        public Table Import(string module_name)
        {
            return null;
        }

        public FunctionBody Parse(string str)
        {
            lex.Init(str);
            return parser.Parse(lex);
        }

        public Table InitModule(FunctionBody tree)
        {
            Function func = new Function();
            func.vm = this;
            func.module_table = new Table();
            func.code = tree;
            func.upvalues = new Dictionary<string, LocalValue>();
            func.Call();
            return func.module_table;
        }
    }
}
