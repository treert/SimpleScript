using System;
using System.Collections.Generic;
using System.Text;

namespace SScript
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
        public readonly Dictionary<string, Table> modules = new Dictionary<string, Table>();
        public readonly Lex lex = new Lex();
        public readonly Parser parser = new Parser();

        public Table DoString(string str)
        {
            return null;
        }

        public Table DoFile(string file_name)
        {
            return null;
        }

        public Table Import(string module_name)
        {
            return null;
        }
    }
}
