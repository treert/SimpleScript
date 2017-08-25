using System;
using System.Collections.Generic;
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

                // This debug adapter does not support a side effect free evaluate request for data hovers.
                supportsEvaluateForHovers = false,

                // This debug adapter does not support exception breakpoint filters
                exceptionBreakpointFilters = new dynamic[0]
            });

            // Mono Debug is ready to accept breakpoints immediately
            SendEvent(new InitializedEvent());
        }

        public override void Launch(Response response, dynamic arguments)
        {
            // ?
            SendResponse(response);
        }

        public override void Attach(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void Disconnect(Response response, dynamic arguments)
        {
            SendResponse(response);
        }

        public override void SetBreakpoints(Response response, dynamic arguments)
        {
            var clientLines = arguments.lines.ToObject<int[]>();
            var breakpoints = new List<Breakpoint>();
            foreach (var l in clientLines)
            {
                breakpoints.Add(new Breakpoint(true, l));
            }

            SendResponse(response, new SetBreakpointsResponseBody(breakpoints));
        }

        public override void Continue(Response response, dynamic arguments)
        {
            // ? wait for singal
            SendResponse(response);
            // contine
        }

        public override void Next(Response response, dynamic arguments)
        {
            SendResponse(response);
            // next line
        }

        public override void StepIn(Response response, dynamic arguments)
        {
            SendEvent(new StoppedEvent(1, "step"));
        }

        public override void StepOut(Response response, dynamic arguments)
        {
            SendEvent(new StoppedEvent(1, "step"));
        }

        public override void Pause(Response response, dynamic arguments)
        {
            SendEvent(new StoppedEvent(1, "pause"));
        }

        public override void StackTrace(Response response, dynamic arguments)
        {
            var stackFrames = new List<StackFrame>();

            for (int i = 0; i < 3; ++i )
            {
                var source = VSCodeDebugAdapter.Source.Create("name","path");
                stackFrames.Add(new StackFrame(i, "frame" + i, source, 2, 1));
            }

            SendResponse(response, new StackTraceResponseBody(stackFrames));
        }

        public override void Scopes(Response response, dynamic arguments)
        {
            int frameId = getInt(arguments, "frameId", 0);

            var scopes = new List<Scope>();

            // om?todo handle exception
            // ...

            scopes.Add(new Scope("local_test_1", 1));
            scopes.Add(new Scope("local_test_2", 2));

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

            variables.Add(new Variable("local_test_1", "1", "int"));

            SendResponse(response, new VariablesResponseBody(variables));
        }

        public override void Threads(Response response, dynamic arguments)
        {
            var threads = new List<Thread>();
            threads.Add(new Thread(1, "main_thread"));

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
