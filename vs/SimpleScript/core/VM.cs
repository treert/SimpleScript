using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimpleScript.DebugProtocol;
namespace SimpleScript
{
    /// <summary>
    /// 脚本虚拟机
    /// 1. 资源管理
    ///     1. 全局表
    ///     2. gc管理，new管理【现在完全没管这个】
    /// 2. 线程管理
    /// 3. 对外接口
    ///     1. DoString
    ///     2. DoFile
    ///     3. CompileString
    ///     4. CompileFile
    ///     5. CallClosure
    /// </summary>
    public class VM
    {
        public static string Version = "0.0.7";
        //**************** do ********************************/
        public void DoString(string s, String file_name = "")
        {
            var func = Parse(s, file_name);
            CallFunction(func);
        }

        public void DoFile(string file_name)
        {
            using (FileStream stream = new FileStream(file_name, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Function func = null;
                if (ReadBom(stream))
                {
                    // utf-8 bom source
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        var source = reader.ReadToEnd();
                        func = Parse(source, file_name);
                    }
                }
                else
                {
                    // compiled binary
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        func = Function.Deserialize(reader);
                    }
                }
                CallFunction(func);
            }
        }

        //**************** call ********************************/
        public object[] CallClosure(Closure closure, params object[] args)
        {
            return CallClosureWithThis(closure, null, args);
        }

        public object[] CallFunction(Function func, params object[] args)
        {
            var closure = NewClosure();
            closure.func = func;
            closure.env_table = m_global;

            return CallClosureWithThis(closure, null, args);
        }

        public object[] CallClosureWithThis(Closure closure, object this_, params object[] args)
        {
            var work_thread = GetWorkThread();
            try
            {
                work_thread.PushValue(closure);
                work_thread.PushValue(this_);
                for (int i = 0; i < args.Length; ++i)
                {
                    work_thread.PushValue(args[i]);
                }
                work_thread.Run();

                // get results
                int count = work_thread.GetStackSize();
                object[] ret = new object[count];
                for (int i = 0; i < count; ++i)
                {
                    ret[i] = work_thread.GetValue(i);
                }
                return ret;
            }
            catch (ScriptException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
            finally
            {
                work_thread.Clear();
                PutWorkThread(work_thread);
            }
        }

        internal void AsyncCall(Closure closure, params object[] args)
        {
            var work_thread = GetWorkThread();
            try
            {
                work_thread.PushValue(closure);
                for (int i = 0; i < args.Length; ++i)
                {
                    work_thread.PushValue(args[i]);
                }
                work_thread.Run();
            }
            catch (ScriptException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //**************** compile *****************************/
        public void ComileFile(string src_file, string out_file = "")
        {
            if (src_file == out_file)
            {
                throw new OtherException("out file is same as src file, file {0}", src_file);
            }
            if (string.IsNullOrEmpty(out_file))
            {
                out_file = src_file + "c";
            }

            using(FileStream src_stream = new FileStream(src_file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new StreamReader(src_stream))
            {
                if (ReadBom(src_stream) == false)
                {
                    throw new OtherException("file {0} has compiled", src_file);
                }
                using (FileStream out_stream = new FileStream(out_file, FileMode.Create))
                {
                    var source = reader.ReadToEnd();
                    CompileString(source, out_stream, src_file);
                }
            }
        }

        public void CompileString(string source, Stream out_stream, string file_name = "")
        {
            var func = Parse(source, file_name);
            out_stream.WriteByte(_header[0]);
            out_stream.WriteByte(_header[1]);
            out_stream.WriteByte(_header[2]);
            using (BinaryWriter writer = new BinaryWriter(out_stream))
            {
                func.Serialize(writer);
            }
        }

        bool ReadBom(Stream src_stream)
        {
            byte[] bom = new byte[3];
            src_stream.Read(bom, 0, 3);
            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            {
                // utf-8 bom source
                return true;
            }
            else if (bom[0] == _header[0] && bom[1] == _header[1] && bom[2] == _header[2])
            {
                // binary source
                return false;
            }
            else
            {
                throw new OtherException("Only support compile binary source or utf-8 bom source");
            }
        }
        //**************** parse *******************************/
        public Function Parse(string source, string file_name)
        {
            _lex.Init(source, file_name);
            var tree = _parser.Parse(_lex);
            var func = _code_generator.Generate(tree, file_name);
            return func;
        }

        public Function Parse(Stream stream, string file_name)
        {
            if(ReadBom(stream))
            {
                // utf-8 bom source
                using (StreamReader reader = new StreamReader(stream))
                {
                    var source = reader.ReadToEnd();
                    return Parse(source, file_name);
                }
            }
            else
            {
                // compiled binary
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    return Function.Deserialize(reader);
                }
            }
        }

        //**************** debug ************************************/
        public readonly Hooker m_hooker;

        public void CallDebugHook(Thread th)
        {
            m_hooker.Hook(th);
        }
        
        //**************** global table *****************************/
        public readonly Table m_global;
        public readonly ImportManager m_import_manager;
        public readonly DelegateGenerateManager m_delegate_generate_mananger;

        public void RegisterTypeHandler(Type t, IImportTypeHandler handler)
        {
            m_import_manager.RegisterHandler(t, handler);
        }

        public void RegisterDelegateGenerater(Type t, Func<Closure, Delegate> generater)
        {
            m_delegate_generate_mananger.RegisterGenerater(t, generater);
        }

        public void RegisterGlobalFunc(string name, CFunction cfunc)
        {
            SetGlobalThing(name, cfunc);
        }

        public void SetGlobalThing(string name, object obj)
        {
            if (string.IsNullOrWhiteSpace(name) == false)
            {
                var segments = name.Split('.');
                var table = m_global;
                for (int i = 0; i < segments.Length - 1; ++i)
                {
                    Table tmp = table.Get(segments[i]) as Table;
                    if (tmp == null)
                    {
                        tmp = NewTable();
                        table.Set(segments[i], tmp);
                    }
                    table = tmp;
                }
                table.Set(segments.Last(), obj);
            }
        }

        public object GetGlobalThing(string name)
        {
            if (string.IsNullOrWhiteSpace(name) == false)
            {
                var segments = name.Split('.');
                var table = m_global;
                for (int i = 0; i < segments.Length - 1; ++i)
                {
                    Table tmp = table.Get(segments[i]) as Table;
                    if (tmp == null)
                    {
                        return null;
                    }
                    table = tmp;
                }
                return table.Get(segments.Last());
            }
            return null;
        }

        /************** some new manager **********************************/
        public Table NewTable()
        {
            return new Table();
        }
        internal Closure NewClosure()
        {
            var closure = new Closure();
            closure.vm = this;
            return closure;
        }
        /******************************************************************/

        Lex _lex;
        Parser _parser;
        CodeGenerate _code_generator;
        Thread _thread;
        Stack<Thread> _other_threads;
        byte[] _header = new byte[3] { 0, (byte)'s', (byte)'s' };

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
            m_import_manager = new ImportManager();
            m_delegate_generate_mananger = new DelegateGenerateManager();
            m_hooker = new Hooker();
        }
    }
}
