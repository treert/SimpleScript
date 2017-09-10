using SimpleScript.DebugProtocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

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
            : base(true)
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
        //int _t_cur_line = 0;
        string _t_work_dir = null;
        int _t_thread = 1;

        Process _process;
        TcpClient _tcp_connect;
        NetStream _net_stream;
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

            // start client
            {
                int port = Utilities.FindFreePort(0);
                if(port <= 0)
                {
                    SendErrorResponse(response, 3003, "Can not launch ss.exe with a port", null);
                    return;
                }
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = Utilities.FindExeDirectory() + "\\ss.exe";
                //info.Arguments = string.Format("-d {0}", Path.GetFileName(_t_main_file), port);
                info.Arguments = string.Format("-d {0} -p {1}", Path.GetFileName(_t_main_file), port);
                info.WorkingDirectory = _t_work_dir;
                _process = Process.Start(info);

                // Connect
                try
                {
                    System.Threading.Thread.Sleep(20);
                    _tcp_connect = new TcpClient();
                    _tcp_connect.Connect(IPAddress.Parse("127.0.0.1"), port);
                    _net_stream = new NetStream(_tcp_connect.GetStream());
                }
                catch
                {
                    SendErrorResponse(response, 3004, "Can not connect to ss.exe");
                    ClearNetConnect();
                    return;
                }
            }

            SendResponse(response);

            SendEvent(new StoppedEvent(_t_thread, "entry"));

            // immediate handle one break
            WaitForResponse();
        }

        public override void Attach(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void Disconnect(Response response, dynamic arguments)
        {
            // stop debug clear all
            if (_process != null)
            {
                //_process.Kill();
                _process = null;
            }

            ClearNetConnect();

            SendResponse(response);
        }

        public void ClearNetConnect()
        {

            if(_tcp_connect != null)
            {
                _tcp_connect.Close();
                _tcp_connect = null;
            }
            if(_net_stream != null)
            {
                _net_stream.Close();
                _net_stream = null;
            }
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

            var file = path.Replace(_t_work_dir , "").Trim('/','\\');

            BreakCmd cmd = new BreakCmd();
            cmd.m_file = file;
            cmd.m_cmd_mode = BreakCmd.BreakCmdMode.ResetOneFile;

            var breakpoints = new List<Breakpoint>();
            for (var i = 0; i < clientLines.Length; ++i)
            {
                var l = ConvertClientLineToDebugger(clientLines[i]);
                var p = new BreakPoint();
                p.file_name = file;
                p.line = l;
                cmd.m_break_points.Add(p);
                breakpoints.Add(new Breakpoint(true, clientLines[i]));
            }
            SendOneCmdAndWaitForResponse(cmd);

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
            SimpleScript.DebugProtocol.Continue cmd = new SimpleScript.DebugProtocol.Continue();
            SendOneCmdAndWaitForResponse(cmd);
            SendResponse(response);
        }

        public override void Next(Response response, dynamic arguments)
        {
            StepOver cmd = new StepOver();
            SendOneCmdAndWaitForResponse(cmd);
            SendResponse(response);
        }

        DebugResponse SendOneCmdAndWaitForResponse(DebugCmd cmd)
        {
            try
            {
                _net_stream.WriteCmd(cmd);

                return WaitForResponse();
            }
            catch
            {
                _process = null; // For debug, cmd windows do not close
                Stop();
                return null;
            }
        }

        DebugResponse WaitForResponse()
        {
            var res = _net_stream.ReadOneRes();
            if (res == null)
            {
                SendEvent(new TerminatedEvent());
                return res;
            }
            if (res.GetType() == typeof(OnBreakRes))
            {
                var r = res as OnBreakRes;
                SendEvent(new StoppedEvent(_t_thread, "breakpoint"));
            }

            return res;
        }

        public override void StepIn(Response response, dynamic arguments)
        {
            SimpleScript.DebugProtocol.StepIn cmd = new SimpleScript.DebugProtocol.StepIn();
            SendOneCmdAndWaitForResponse(cmd);
            SendResponse(response);
        }

        public override void StepOut(Response response, dynamic arguments)
        {
            SimpleScript.DebugProtocol.StepOut cmd = new SimpleScript.DebugProtocol.StepOut();
            SendOneCmdAndWaitForResponse(cmd);
            SendResponse(response);
        }

        public override void Pause(Response response, dynamic arguments)
        {
            SendErrorResponse(response, 5001, "Do not Support Pause Cmd");
        }

        public override void StackTrace(Response response, dynamic arguments)
        {
            BackTraceCmd cmd = new BackTraceCmd();
            BackTraceRes res =  SendOneCmdAndWaitForResponse(cmd) as BackTraceRes;

            var stackFrames = new List<StackFrame>();

            if(res != null)
            {
                for (int i = 0; i < res.m_frames.Count; ++i)
                {
                    var frame = res.m_frames[i];
                    var file = frame.Item1;
                    file = Path.Combine(_t_work_dir, file);
                    var line = frame.Item3;
                    var func_name = frame.Item2;
                    var source = VSCodeDebugAdapter.Source.Create(Path.GetFileName(file), ConvertDebuggerPathToClient(file));
                    stackFrames.Add(new StackFrame(i+1, func_name, source, line, 0));
                }
            }

            SendResponse(response, new StackTraceResponseBody(stackFrames));
        }

        Handles<object[]> _variableHandles = new Handles<object[]>();
        GetFrameInfoRes _cur_frame_variables; 
        public override void Scopes(Response response, dynamic arguments)
        {
            int frameId = getInt(arguments, "frameId", 0);
            GetFrameInfoCmd cmd = new GetFrameInfoCmd();
            cmd.m_stack_idx = frameId;
            _cur_frame_variables = SendOneCmdAndWaitForResponse(cmd) as GetFrameInfoRes;
            
            var scopes = new List<Scope>();
            scopes.Add(new Scope("Local", 1));
            scopes.Add(new Scope("Closure", 2));

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
            

            var variables = new List<Variable>();
            if(_cur_frame_variables != null)
            {
                List<GetFrameInfoRes.ValueInfo> values;
                if(reference == 1)
                {
                    values = _cur_frame_variables.m_locals;
                }
                else
                {
                    values = _cur_frame_variables.m_upvalues;
                }
                foreach(var val in values)
                {
                    variables.Add(new Variable(val.name, val.value, val.type));
                }
            }

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
                int frameId = getInt(arguments, "frameId", -1);
                if(frameId == -1)
                {
                    error = "frameId miss";
                }

                PrintCmd cmd = new PrintCmd();
                cmd.m_name = expression;
                cmd.m_stack_idx = frameId;

                PrintRes res = SendOneCmdAndWaitForResponse(cmd) as PrintRes;
                if(res != null)
                {
                    SendResponse(response, new EvaluateResponseBody(res.ToResString(), 0));
                }
                else
                {
                    error = "can not ecaluate, network seems break";
                }
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
