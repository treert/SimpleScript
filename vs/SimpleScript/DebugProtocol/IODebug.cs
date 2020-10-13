using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SimpleScript.DebugProtocol;

namespace SimpleScript
{
    public class IOPipe: DebugPipeServer
    {

        string _help_info = string.Empty;

        public IOPipe()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("debug cmd list [no zuo no die]");
            for(int i = 0; i < _help_cmd_list.Length; ++i)
            {
                sb.AppendFormat("{0,2}. ", i);
                sb.AppendLine(_help_cmd_list[i]);
            }
            _help_info = sb.ToString();
        }

        public DebugCmd ReadCmd()
        {
            while(true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                try
                {
                    var cmd = ParseCmd(line);
                    if (cmd != null)
                    {
                        return cmd;
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }
        }

        public void WriteRes(DebugResponse res)
        {
            Console.WriteLine(res.ToResString());
        }

        string[] _help_cmd_list = new string[]
        {
            @"h                 # help                                     ",
            @"b $file $line     # breakpoint set --file main.oms --line 12  ",
            @"br delete $index  # breakpoint delete 1 2 3                  ",
            @"br clear          # breakpoint deleteall                     ",
            @"br list           # breakpoint list                          ",
            @"c                 # continue                                 ",
            @"n                 # step over                                ",
            @"s                 # step in                                  ",
            @"f                 # step out                                 ",
            @"bt                # backtrace                                ",
            @"p $name           # print a.b                                ",
            @"t                 # terminate debug                          ",
        };

        DebugCmd ParseCmd(string line)
        {
            string[] args = System.Text.RegularExpressions.Regex.Split(line, @"\s+");
            if(args.Length == 0)
            {
                return null;
            }
            if(args[0] == "b" && args.Length == 3)
            {
                BreakCmd cmd = new BreakCmd();
                BreakPoint point = new BreakPoint();
                point.file_name = args[1];
                point.line = Convert.ToInt32(args[2]);
                cmd.m_cmd_mode = BreakCmd.BreakCmdMode.Set;
                cmd.m_break_points.Add(point);
                return cmd;
            }
            else if(args[0] == "br" && args.Length >= 2)
            {
                BreakCmd cmd = new BreakCmd();
                if (args[1] == "list")
                {
                    cmd.m_cmd_mode = BreakCmd.BreakCmdMode.List;
                    return cmd;
                }
                else if(args[1] == "clear")
                {
                    cmd.m_cmd_mode = BreakCmd.BreakCmdMode.DeleteAll;
                    return cmd;
                }
                else if(args[1] == "delete" && args.Length >= 3)
                {
                    cmd.m_cmd_mode = BreakCmd.BreakCmdMode.Delete;
                    for(int i = 2; i < args.Length; ++i)
                    {
                        BreakPoint point = new BreakPoint();
                        point.index = Convert.ToInt32(args[i]);
                        cmd.m_break_points.Add(point);
                    }
                    return cmd;
                }
            }
            else if(args[0] == "c")
            {
                Continue cmd = new Continue();
                return cmd;
            }
            else if(args[0] == "n")
            {
                StepOver cmd = new StepOver();
                return cmd;
            }
            else if(args[0] == "s")
            {
                StepIn cmd = new StepIn();
                return cmd;
            }
            else if(args[0] == "f")
            {
                StepOut cmd = new StepOut();
                return cmd;
            }
            else if(args[0] == "bt")
            {
                BackTraceCmd cmd = new BackTraceCmd();
                return cmd;
            }
            else if(args[0] == "p" && args.Length == 2)
            {
                PrintCmd cmd = new PrintCmd();
                cmd.m_name = args[1];
                return cmd;
            }
            else if(args[0] == "t")
            {
                Terminate cmd = new Terminate();
                return cmd;
            }
            else if(args[0] == "h")
            {
                Console.Write(_help_info);
            }
            else
            {
                Console.WriteLine("what do you want? type 'h' for help");
            }
            
            return null;
        }
    }
}
