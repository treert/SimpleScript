using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleScript.DebugProtocol
{

    public interface DebugPipeServer
    {
        DebugCmd ReadCmd();
        void WriteRes(DebugResponse res);
    }

    public interface DebugCmd
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hooker"></param>
        /// <param name="th"></param>
        /// <returns> whether thread can run</returns>
        bool Exec(Hooker hooker, Thread th);
        void WriteTo(BinaryWriter writer);
        void ReadFrom(BinaryReader reader);
    }

    public interface DebugResponse
    {
        string ToString();
        void WriteTo(BinaryWriter writer);
        void ReadFrom(BinaryReader reader);
    }

    public enum BreakMode
    {
        Ignore,// no break
        Point,// stop at BreakPoint
        StepOver,// stop in next line, ignore child call
        StepIn,// stop in next line, take care of child call
        StepOut,// stop when current call end
        StopForOnce,// stop for once, then enter Point mode
    }

    public class Hooker
    {
        public void Hook(Thread th)
        {
            if(_break_mode == BreakMode.Ignore || _pipe_server == null)
            {
                return;
            }
            // check break for every mode
            bool need_break = CheckBreak(th);
            if(need_break == false)
            {
                return;
            }

            // break and wait for cmd
            var cmd = _pipe_server.ReadCmd();
            
            while(cmd.Exec(this, th) == false)
            {
                cmd = _pipe_server.ReadCmd();
            }
        }
        public void SendResponse(DebugResponse response)
        {
            _pipe_server.WriteRes(response);
        }

        public void SetPipeServer(DebugPipeServer server)
        {
            _pipe_server = server;
        }

        internal void SetBreakMode(BreakMode mode)
        {
            _break_mode = mode;
        }

        #region attribute
        BreakMode _break_mode = BreakMode.Point;
        DebugPipeServer _pipe_server = null;

        string _cur_file = string.Empty;
        int _cur_line = 0;// line start from 1, 0 is before start
        int _cur_call_level = 0;

        internal readonly List<BreakPoint> _breakpoints = new List<BreakPoint>();
        #endregion

        bool CheckBreak(Thread th)
        {
            var tuple = th.GetCurrentCallFrameInfo();
            var file = tuple.Item1;
            var line = tuple.Item2;
            var call_level = tuple.Item3;

            bool need_break = false;
            if(_break_mode == BreakMode.StopForOnce)
            {
                _break_mode = BreakMode.Point;
                need_break = true;// must break
            }
            else if (IsSameWithCurrentBreak(file, line, call_level))
            {
                return false;// one line can have several code
            }
            else if(HitBreakPoint(file, line, call_level))
            {
                need_break = true;// hit BreakPoint
            }
            else
            {
                // other situation
                switch (_break_mode)
                {
                    case BreakMode.StepIn:
                        need_break = true;
                        break;
                    case BreakMode.StepOut:
                        need_break = call_level < _cur_call_level;
                        break;
                    case BreakMode.StepOver:
                        need_break = call_level <= _cur_call_level;
                        break;
                }
            }
            
            if(need_break)
            {
                _cur_file = file;
                _cur_line = line;
                _cur_call_level = call_level;
            }
            return need_break;
        }

        bool HitBreakPoint(string file, int line, int call_level)
        {
            foreach(var point in _breakpoints)
            {
                if(point.Hit(file, line))
                {
                    return true;
                }
            }
            return false;
        }

        bool IsSameWithCurrentBreak(string file, int line, int call_level)
        {
            if (_cur_file == file && _cur_line == line  && _cur_call_level == call_level)
            {
                return true;
            }
            return false;
        }
    }

    public class BreakPoint
    {
        public string file_name;
        public int line;
        public bool Hit(string file_, int line_)
        {
            if(line == line_ && file_name.EndsWith(file_))
            {
                return true;
            }
            return false;
        }
    }

    public class Continue : DebugCmd
    {
        public bool Exec(Hooker hooker, Thread th)
        {
            hooker.SetBreakMode(BreakMode.Point);
            return true;
        }

        public void WriteTo(BinaryWriter writer)
        {
            // ...
        }

        public void ReadFrom(BinaryReader reader)
        {
            // ...
        }
    }

    public class StepOver : DebugCmd
    {
        public bool Exec(Hooker hooker, Thread th)
        {
            hooker.SetBreakMode(BreakMode.StepOver);
            return true;
        }

        public void WriteTo(BinaryWriter writer)
        {
            // ...
        }

        public void ReadFrom(BinaryReader reader)
        {
            // ...
        }
    }

    public class StepIn: DebugCmd
    {
        public bool Exec(Hooker hooker, Thread th)
        {
            hooker.SetBreakMode(BreakMode.StepIn);
            return true;
        }

        public void WriteTo(BinaryWriter writer)
        {
            // ...
        }

        public void ReadFrom(BinaryReader reader)
        {
            /*throw new NotImplementedException();*/
        }
    }

    public class StepOut : DebugCmd
    {
        
        public bool Exec(Hooker hooker, Thread th)
        {
            hooker.SetBreakMode(BreakMode.StepOut);
            return true;
        }

        public void WriteTo(BinaryWriter writer)
        {
            /*throw new NotImplementedException();*/
        }

        public void ReadFrom(BinaryReader reader)
        {
            /*throw new NotImplementedException();*/
        }
    }

    public class Terminate: DebugCmd
    {
        public bool Exec(Hooker hooker, Thread th)
        {
            hooker.SetBreakMode(BreakMode.Ignore);
            return true;
        }

        public void WriteTo(BinaryWriter writer)
        {
/*            throw new NotImplementedException();*/
        }

        public void ReadFrom(BinaryReader reader)
        {
/*            throw new NotImplementedException();*/
        }
    }

    public class BreakCmd: DebugCmd
    {
        public enum BreakCmdMode
        {
            Set,
            Replace,
            Delete,
            DeleteAll,
            GetInfo,
        }
        public BreakCmdMode m_cmd_mode = BreakCmdMode.Set;
        public List<BreakPoint> m_break_points = new List<BreakPoint>();
        public bool Exec(Hooker hooker, Thread th)
        {
            if(m_cmd_mode == BreakCmdMode.Set)
            {
                hooker._breakpoints.AddRange(m_break_points);
            }
            else if(m_cmd_mode == BreakCmdMode.Replace)
            {
                hooker._breakpoints.Clear();
                hooker._breakpoints.AddRange(m_break_points);
            }
            else if(m_cmd_mode == BreakCmdMode.Delete)
            {
                hooker._breakpoints.RemoveAll(point => m_break_points.Contains(point));
            }
            else if(m_cmd_mode == BreakCmdMode.DeleteAll)
            {
                hooker._breakpoints.Clear();
            }
            else if(m_cmd_mode == BreakCmdMode.GetInfo)
            {
                
            }
            
            return false;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write((int)m_cmd_mode);
            writer.Write(m_break_points.Count);
            foreach(var point in m_break_points)
            {
                writer.Write(point.file_name);
                writer.Write(point.line);
            }
        }

        public void ReadFrom(BinaryReader reader)
        {
            m_cmd_mode = (BreakCmdMode)reader.ReadInt32();
            int count = reader.ReadInt32();
            m_break_points.Clear();
            for(int i = 0; i < count; ++i)
            {
                var point = new BreakPoint();
                point.file_name = reader.ReadString();
                point.line = reader.ReadInt32();
                m_break_points.Add(point);
            }
        }
    }

    public class PrintCmd: DebugCmd
    {
        public string m_name = string.Empty;
        public bool Exec(Hooker hooker, Thread th)
        {
            throw new NotImplementedException();
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(m_name);
        }

        public void ReadFrom(BinaryReader reader)
        {
            m_name = reader.ReadString();
        }
    }

    public class BackTraceCmd: DebugCmd
    {
        public bool Exec(Hooker hooker, Thread th)
        {
            throw new NotImplementedException();
        }

        public void WriteTo(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public void ReadFrom(BinaryReader reader)
        {
            throw new NotImplementedException();
        }
    }

    public class GetBreakInfoRes: DebugResponse
    {



        public void WriteTo(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public void ReadFrom(BinaryReader reader)
        {
            throw new NotImplementedException();
        }
    }

    public class OnBreakRes: DebugResponse
    {



        public void WriteTo(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public void ReadFrom(BinaryReader reader)
        {
            throw new NotImplementedException();
        }
    }
}
