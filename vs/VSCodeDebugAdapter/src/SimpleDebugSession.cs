using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/***
 * 准备支持的调试命令
 * 1. breakpoint 设置文件行断点
 * 2. next 下一行
 * 3. continue 继续执行
 * 4. print `expression`
 * 5. bactstrace
 */

namespace VSCodeDebugAdapter
{
    class SimpleDebugSession : DebugSession
    {
        public SimpleDebugSession()
            : base(false)
        {

        }

        public override void Initialize(Response response, dynamic args)
        {
            OperatingSystem os = Environment.OSVersion;
            if (os.Platform != PlatformID.MacOSX && os.Platform != PlatformID.Unix && os.Platform != PlatformID.Win32NT)
            {
                SendErrorResponse(response, 3000, "Mono Debug is not supported on this platform ({_platform}).", new { _platform = os.Platform.ToString() }, true, true);
                return;
            }

            SendResponse(response, new Capabilities()
            {
                // This debug adapter does not need the configurationDoneRequest.
                supportsConfigurationDoneRequest = false,

                // This debug adapter does not support function breakpoints.
                supportsFunctionBreakpoints = false,

                // This debug adapter doesn't support conditional breakpoints.
                supportsConditionalBreakpoints = false,

                // This debug adapter support a side effect free evaluate request for data hovers.
                supportsEvaluateForHovers = true,

                // This debug adapter does not support exception breakpoint filters
                exceptionBreakpointFilters = new dynamic[0]
            });

            // Mono Debug is ready to accept breakpoints immediately
            SendEvent(new InitializedEvent());
        }

        string _t_main_file = null;
        string[] _t_main_lines;
        int _t_cur_line = 0;
        string _t_work_dir = null;
        int _t_thread = 1;
        public override void Launch(Response response, dynamic args)
        {
            string programPath = getString(args, "program");
            if (programPath == null)
            {
                SendErrorResponse(response, 3001, "Property 'program' is missing or empty.", null);
                return;
            }
            programPath = ConvertClientPathToDebugger(programPath);
            if (!File.Exists(programPath) && !Directory.Exists(programPath))
            {
                SendErrorResponse(response, 3002, "Program '{path}' does not exist.", new { path = programPath });
                return;
            }
            _t_work_dir = Path.GetDirectoryName(programPath);

            _t_main_file = programPath;
            _t_main_lines = File.ReadAllLines(_t_main_file);

            SendResponse(response);

            SendEvent(new StoppedEvent(_t_thread, "entry"));
        }

        public override void Attach(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void Disconnect(Response response, dynamic arguments)
        {
            // stop debug clear all

            SendResponse(response);
        }

        Dictionary<string, HashSet<int>> _t_breakpoints = new Dictionary<string, HashSet<int>>();
        public override void SetBreakpoints(Response response, dynamic args)
        {
            string path = null;
            if (args.source != null)
            {
                string p = (string)args.source.path;
                if (p != null && p.Trim().Length > 0)
                {
                    path = p;
                }
            }
            if (path == null)
            {
                SendErrorResponse(response, 3010, "setBreakpoints: property 'source' is empty or misformed", null, false, true);
                return;
            }
            path = ConvertClientPathToDebugger(path);

            int[] clientLines = args.lines.ToObject<int[]>();

            var set = GetBreakLinesByFile(path);
            set.Clear();
            var breakpoints = new List<Breakpoint>();
            for (var i = 0; i < clientLines.Length; ++i)
            {
                var l = ConvertClientLineToDebugger(clientLines[i]);
                bool valid = false;
                if(l < _t_main_lines.Length)
                {
                    var line = _t_main_lines[l].Trim();
                    if(line.Length > 0)
                    {
                        set.Add(l);
                        valid = true;
                    }
                }
                breakpoints.Add(new Breakpoint(valid, clientLines[i]));
            }

            SendResponse(response, new SetBreakpointsResponseBody(breakpoints));
        }

        HashSet<int> GetBreakLinesByFile(string file)
        {
            file = file.Replace('\\', '/');
            if (_t_breakpoints.ContainsKey(file) == false)
            {
                _t_breakpoints.Add(file, new HashSet<int>());
            }
            return _t_breakpoints[file];
        }

        public override void Continue(Response response, dynamic arguments)
        {
            var set = GetBreakLinesByFile(_t_main_file);
            for(int l = _t_cur_line + 1; l < _t_main_lines.Length; ++l)
            {
                _t_cur_line = l;
                if (set.Contains(l))
                {
                    SendResponse(response);
                    SendEvent(new StoppedEvent(_t_thread, "breakpoint"));
                    return;
                }
            }

            SendResponse(response);
            SendEvent(new TerminatedEvent());
        }

        public override void Next(Response response, dynamic arguments)
        {
            test_run(response, "next");
        }

        void test_run(Response response, string reason)
        {
            int l = _t_cur_line + 1;
            if (l < _t_main_lines.Length)
            {
                _t_cur_line = l;
                SendResponse(response);
                SendEvent(new StoppedEvent(_t_thread, reason));
                return;
            }

            SendResponse(response);
            SendEvent(new TerminatedEvent());
        }

        public override void StepIn(Response response, dynamic arguments)
        {
            test_run(response, "step-in");
        }

        public override void StepOut(Response response, dynamic arguments)
        {
            test_run(response, "step-out");
        }

        public override void Pause(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void StackTrace(Response response, dynamic arguments)
        {
            var stackFrames = new List<StackFrame>();

            for (int i = 1; i <= 3; ++i )
            {
                var source = VSCodeDebugAdapter.Source.Create(Path.GetFileName(_t_main_file), ConvertDebuggerPathToClient(_t_main_file));
                stackFrames.Add(new StackFrame(i, "frame" + i, source, _t_cur_line+1, 0));
            }

            SendResponse(response, new StackTraceResponseBody(stackFrames));
        }

        Handles<object[]> _variableHandles = new Handles<object[]>();
        public override void Scopes(Response response, dynamic arguments)
        {
            int frameId = getInt(arguments, "frameId", 0);

            var scopes = new List<Scope>();

            

            scopes.Add(new Scope("Local", frameId * 10 + 1));
            scopes.Add(new Scope("Closure", frameId * 10 + 2));
            scopes.Add(new Scope("Global", frameId * 10 + 3));

            SendResponse(response, new ScopesResponseBody(scopes));
        }

        public override void Variables(Response response, dynamic arguments)
        {
            int reference = getInt(arguments, "variablesReference", -1);
            if (reference == -1)
            {
                SendErrorResponse(response, 3009, "variables: property 'variablesReference' is missing", null, false, true);
                return;
            }

            // waitforresponse

            var variables = new List<Variable>();
            // _variableHandles.Get(reference, null);

            variables.Add(new Variable("ref_id", ""+ reference, "int"));

            SendResponse(response, new VariablesResponseBody(variables));
        }

        public override void Threads(Response response, dynamic arguments)
        {
            var threads = new List<Thread>();
            threads.Add(new Thread(1, "thread 1"));

            SendResponse(response, new ThreadsResponseBody(threads));
        }

        public override void Evaluate(Response response, dynamic arguments)
        {
            var expression = getString(arguments, "expression");
            string error = null;

            if (expression == null)
            {
                error = "expression missing";
            }
            else
            {
                // todo check expression

                SendResponse(response, new EvaluateResponseBody("ecaluate: " + expression, 0));
            }

            SendErrorResponse(response, 3014, "Evaluate request failed ({_reason}).", new { _reason = error });
        }

        // some static util func
        private static bool getBool(dynamic container, string propertyName, bool dflt = false)
        {
            try
            {
                return (bool)container[propertyName];
            }
            catch (Exception)
            {
                // ignore and return default value
            }
            return dflt;
        }

        private static int getInt(dynamic container, string propertyName, int dflt = 0)
        {
            try
            {
                return (int)container[propertyName];
            }
            catch (Exception)
            {
                // ignore and return default value
            }
            return dflt;
        }

        private static string getString(dynamic args, string property, string dflt = null)
        {
            var s = (string)args[property];
            if (s == null)
            {
                return dflt;
            }
            s = s.Trim();
            if (s.Length == 0)
            {
                return dflt;
            }
            return s;
        }
    }
}
