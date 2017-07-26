using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    public static class LibBase
    {
        static int Print(Thread th)
        {
            int arg_count = th.GetCFunctionArgCount();

            for (int i = 0; i < arg_count; ++i)
            {
                object obj = th.GetCFunctionArg(i);
                if (obj == null)
                    Console.Write("nil");
                else if (obj is bool)
                    Console.Write("{0}", obj);
                else if (obj is double)
                    Console.Write("{0}", obj);
                else if (obj is string)
                    Console.Write(obj);
                else
                    Console.Write("{0}:{1}", obj.GetType().Name, obj);

                if (i != arg_count - 1)
                {
                    Console.Write("\t");
                }
            }
            Console.WriteLine();
            return 0;
        }

        static int Module(Thread th)
        {
            string name = th.GetCFunctionArg(0) as string;
            if(name != null)
            {
                // todo 没有容错
                var segments = name.Split('.');
                var table = th.VM.m_global;
                var vm = th.VM;
                for (int i = 0; i < segments.Length; ++i)
                {
                    Table tmp = table.GetValue(segments[i]) as Table;
                    if(tmp == null)
                    {
                        tmp = vm.NewTable();
                        table.SetValue(segments[i], tmp);
                    }
                    table = tmp;
                }
                th.SetModuleEnv(table);
                th.PushValue(table);
                return 1;
            }
            return 0;
        }

        static int Import(Thread th)
        {
            string name = th.GetCFunctionArg(0) as string;
            if (name != null)
            {
                var vm = th.VM;
                string name2 = th.GetCFunctionArg(1) as string;
                if(name2 == null)
                {
                    var handler = vm.GetGlobalThing(name) as IImportTypeHandler;
                    if (handler == null)
                    {
                        // create it
                        Type t = Type.GetType(name);
                        if(t != null)
                        {
                            handler = vm.m_import_manager.GetOrCreateHandler(t);
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    th.PushValue(handler);
                    return 1;
                }
                else
                {
                    var handler = vm.GetGlobalThing(name2) as IImportTypeHandler;
                    if (handler == null)
                    {
                        // create it
                        Type t = Type.GetType(name);
                        if (t != null)
                        {
                            handler = vm.m_import_manager.GetOrCreateHandler(t);
                            vm.SetGlobalThing(name2, handler);
                        }
                        else
                        {
                            return 0;
                        }
                    }

                    th.PushValue(handler);
                    return 1;
                }
            }
            return 0;
        }

        static IImportTypeHandler _Import(VM vm, Type t, string name = null)
        {
            var handler = vm.m_import_manager.GetOrCreateHandler(t);
            if (string.IsNullOrWhiteSpace(name) == false)
            {
                var segments = name.Split('.');
                var table = vm.m_global;
                for (int i = 0; i < segments.Length - 1; ++i)
                {
                    Table tmp = table.GetValue(segments[i]) as Table;
                    if (tmp == null)
                    {
                        tmp = vm.NewTable();
                        table.SetValue(segments[i], tmp);
                    }
                    table = tmp;
                }
                table.SetValue(segments.Last(), handler);
            }
            return handler;
        }

        public static void Register(VM vm)
        {
            vm.RegisterGlobalFunc("print", Print);
            vm.RegisterGlobalFunc("module", Module);
            vm.RegisterGlobalFunc("import", Import);
        }
    }
}
