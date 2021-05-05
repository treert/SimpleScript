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
        public readonly MyTable global_table = new();
        // todo@om
        public readonly Dictionary<string, MyTable> file_modules = new();
        public readonly Lex lex = new Lex();
        public readonly Parser parser = new Parser();

        public object? DoString(string str, MyTable module)
        {
            FunctionBody tree = Parse(str);
            var func = tree.CreateFunction(this, module);
            return func.Call();
        }

        /// <summary>
        /// todo@om
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public MyTable DoFile(string file)
        {
            var path = Path.GetFullPath(file);
            var source = File.ReadAllText(path);
            var tree = Parse(source, file);
            var func = tree.CreateFunction(this);
            func.Call();
            return func.module_table;
        }

        public MyTable Import(string module_name)
        {
            return null;
        }

        public FunctionBody Parse(string source, string source_name = "")
        {
            lex.Init(source, source_name);
            return parser.Parse(lex);
        }
    }
}
