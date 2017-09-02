using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// for simple. the protocol should design as one request one response
/// </summary>
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
        string ToResString();
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
            OnBreakRes res = new OnBreakRes();
            res.m_file = _cur_file;
            res.m_line = _cur_line;
            SendResponse(res);


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
            _cur_call_level = -1;
        }

        public void SetBreakMode(BreakMode mode)
        {
            _break_mode = mode;
        }

        #region attribute
        BreakMode _break_mode = BreakMode.Point;
        DebugPipeServer _pipe_server = null;

        string _cur_file = string.Empty;
        int _cur_line = 0;// line start from 1, 0 is before start
        int _cur_call_level = 0;

        LinkedList<BreakPoint> _breakpoints = new LinkedList<BreakPoint>();
        #endregion

        internal BreakPoint AddBreakPoint(BreakPoint point)
        {
            int index = 1;
            for(var iter = _breakpoints.First; iter != null; iter = iter.Next, ++index)
            {
                if(iter.Value.index > index)
                {
                    point.index = index;
                    _breakpoints.AddBefore(iter, point);
                    return point;
                }
            }

            point.index = index;
            _breakpoints.AddLast(point);
            return point;
        }

        internal BreakPoint RemoveBreakPointByIndex(int index)
        {
            var iter = _breakpoints.First;
            while(iter != null)
            {
                if(iter.Value.index == index)
                {
                    _breakpoints.Remove(iter);
                    return iter.Value;
                }
                iter = iter.Next;
            }
            return null;
        }

        internal void ClearBreakPoint()
        {
            _breakpoints.Clear();
        }

        internal void ResetBreakPointForOneFile(string file, List<BreakPoint> points)
        {
            for (var iter = _breakpoints.First; iter != null; )
            {
                if(iter.Value.file_name == file)
                {
                    var tmp = iter;
                    iter = iter.Next;
                    _breakpoints.Remove(tmp);
                }
                else
                {
                    iter = iter.Next;
                }
            }

            foreach(var p in points)
            {
                AddBreakPoint(p);
            }
        }

        internal LinkedList<BreakPoint> GetBreakPoint()
        {
            return _breakpoints;
        }

        bool CheckBreak(Thread th)
        {
            var tuple = th.GetCurrentCallFrameInfo();
            var file = tuple.Item1;
            var line = tuple.Item2;
            var call_level = tuple.Item3;

            bool need_break = false;
            if(line < 1)
            {
                return false;// some code line is set -1 or 0(Fuck ME)
            }
            else if(_break_mode == BreakMode.StopForOnce)
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
            else
            {
                // do not need do any thing
                // so can break many times in for
                //_cur_line = -1;
                //_cur_call_level = -1;// call level can not change, or step over will fail
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
        public int index;
        public bool Hit(string file_, int line_)
        {
            if(line == line_ && file_name.EndsWith(file_))
            {
                return true;
            }
            return false;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(file_name);
            writer.Write(line);
            writer.Write(index);
        }

        public void ReadFrom(BinaryReader reader)
        {
            file_name = reader.ReadString();
            line = reader.ReadInt32();
            index = reader.ReadInt32();
        }

        public override string ToString()
        {
            return string.Format("BreakPoint {0,3}: {1}:{2}", index, file_name, line);
        }
    }
}
