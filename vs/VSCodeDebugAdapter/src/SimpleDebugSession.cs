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
            throw new NotImplementedException();
        }

        public override void StepOut(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void Pause(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override void Variables(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void Threads(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void Evaluate(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }
    }
}
