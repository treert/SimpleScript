using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using MyScript;

namespace MyScriptConsole
{
    class Program
    {
        static bool IsComplete(string source)
        {
            Lex lex = new Lex();
            lex.Init(source);
            try
            {
                Token tk = lex.GetNextToken();
                while (lex.IsEnded == false)
                {
                    tk = lex.GetNextToken();
                }
                return lex.CurStringType == StringBlockType.Begin && tk.Match(',') == false;
            }
            catch (LexUnexpectEndException)
            {
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.GetType().Name} {e.Message}");
            }
            return true;
        }
        static void Main(string[] args)
        {
            // 失望
            Console.InputEncoding = Encoding.Unicode;
            Console.OutputEncoding = Encoding.Unicode;
            Console.CursorVisible = true;
            Console.WriteLine("MyScript 0.9");
            VM vm = new VM();
            vm.global_table["echo"] = new MyConsole();
            MyScriptStdLib.LibString.Register(vm);
            MyTable module = new MyTable();
            StringBuilder sb = new StringBuilder();
            for(; ; )
            {
                if (sb.Length > 0) Console.Write('>');
                Console.Write("> ");
                string line = Console.ReadLine();
                if (line == null) return;
                sb.AppendLine(line);
                var source = sb.ToString();
                if (IsComplete(source) == false)
                {
                    continue;
                }
                try
                {
                    FunctionBody tree = vm.Parse(source);
                    if(tree.block.statements.Count == 1 && tree.block.statements[0] is ExpSyntaxTree)
                    {
                        source = "return " + source;
                        tree = vm.Parse(source);
                        var func = tree.CreateFunction(vm, module);
                        var obj = func.Call();
                        if (obj is not null) Console.WriteLine($"{obj}");
                    }
                    else
                    {
                        vm.DoString(source, module);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}");
                }
                sb.Clear();
            }
        }
    }
}
