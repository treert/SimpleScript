using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleScript.DebugProtocol
{
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

    public class StepIn : DebugCmd
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

    public class Terminate : DebugCmd
    {
        public bool Exec(Hooker hooker, Thread th)
        {
            hooker.SetBreakMode(BreakMode.Ignore);
            hooker.SetPipeServer(null);
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

    public class BreakCmd : DebugCmd
    {
        public enum BreakCmdMode
        {
            Set,
            ResetOneFile,
            Delete,
            DeleteAll,
            List,
        }
        public BreakCmdMode m_cmd_mode = BreakCmdMode.Set;
        public List<BreakPoint> m_break_points = new List<BreakPoint>();
        public string m_file = "";// for reset
        public bool Exec(Hooker hooker, Thread th)
        {
            BreakOpRes res = new BreakOpRes();
            if (m_cmd_mode == BreakCmdMode.Set)
            {
                for (int i = 0; i < m_break_points.Count; ++i)
                {
                    hooker.AddBreakPoint(m_break_points[i]);
                }
                res.m_head_desc = "Add BreakPoint:";
                res.m_break_points = m_break_points;
            }
            else if (m_cmd_mode == BreakCmdMode.ResetOneFile)
            {
                hooker.ResetBreakPointForOneFile(m_file, m_break_points);
                res.m_head_desc = "Reset BreakPoint:";
                res.m_break_points = m_break_points;
            }
            else if (m_cmd_mode == BreakCmdMode.Delete)
            {
                res.m_head_desc = "Remove BreakPoint:";
                for (int i = 0; i < m_break_points.Count; ++i)
                {
                    var point = hooker.RemoveBreakPointByIndex(m_break_points[i].index);
                    if(point != null)
                    {
                        res.m_break_points.Add(m_break_points[i]);
                    }
                }
            }
            else if (m_cmd_mode == BreakCmdMode.DeleteAll)
            {
                hooker.ClearBreakPoint();
                res.m_head_desc = "Clear BreakPoint";
            }
            else if (m_cmd_mode == BreakCmdMode.List)
            {
                res.m_break_points.AddRange(hooker.GetBreakPoint());
                res.m_head_desc = "List BreakPoint ("+res.m_break_points.Count+"):";
            }
            hooker.SendResponse(res);

            return false;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(m_file);
            writer.Write((int)m_cmd_mode);
            writer.Write(m_break_points.Count);
            foreach (var point in m_break_points)
            {
                point.WriteTo(writer);
            }
        }

        public void ReadFrom(BinaryReader reader)
        {
            m_file = reader.ReadString();
            m_cmd_mode = (BreakCmdMode)reader.ReadInt32();
            int count = reader.ReadInt32();
            m_break_points.Clear();
            for (int i = 0; i < count; ++i)
            {
                var point = new BreakPoint();
                point.ReadFrom(reader);
                m_break_points.Add(point);
            }
        }
    }

    public class PrintCmd : DebugCmd
    {
        public string m_name = string.Empty;
        public int m_stack_idx = 1;
        public bool Exec(Hooker hooker, Thread th)
        {
            PrintRes res = new PrintRes();
            res.m_name = m_name;
            var segments = m_name.Split('.');
            object obj = th.GetObjByName(segments[0], m_stack_idx);
            for(int i = 1; i < segments.Length; ++i)
            {
                if(obj is IGetSet)
                {
                    obj = (obj as IGetSet).Get(segments[i]);
                }
                else
                {
                    obj = null;
                    break;
                }
            }
            
            if(obj != null)
            {
                res.m_type_name = obj.GetType().Name;
                res.m_value_str = obj.ToString();
            }
            else
            {
                res.m_type_name = "null";
                res.m_value_str = "nil";
            }
            hooker.SendResponse(res);
            return false;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(m_name);
            writer.Write(m_stack_idx);
        }

        public void ReadFrom(BinaryReader reader)
        {
            m_name = reader.ReadString();
            m_stack_idx = reader.ReadInt32();
        }
    }

    public class BackTraceCmd : DebugCmd
    {
        public bool Exec(Hooker hooker, Thread th)
        {
            var frames = th.GetBackTraceInfo();
            BackTraceRes res = new BackTraceRes();
            res.m_frames = frames;
            hooker.SendResponse(res);
            return false;
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

    public class GetFrameInfoCmd: DebugCmd
    {

        public int m_stack_idx = 1;

        public bool Exec(Hooker hooker, Thread th)
        {
            GetFrameInfoRes res = new GetFrameInfoRes();
            th.FillFrameInfo(res, m_stack_idx);
            hooker.SendResponse(res);
            return false;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(m_stack_idx);
        }

        public void ReadFrom(BinaryReader reader)
        {
            m_stack_idx = reader.ReadInt32();
        }
    }

    public class BreakOpRes : DebugResponse
    {
        public string m_head_desc = string.Empty;
        public List<BreakPoint> m_break_points = new List<BreakPoint>();
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(m_head_desc);
            writer.Write(m_break_points.Count);
            foreach (var point in m_break_points)
            {
                point.WriteTo(writer);
            }
        }

        public void ReadFrom(BinaryReader reader)
        {
            m_head_desc = reader.ReadString();
            int count = reader.ReadInt32();
            m_break_points.Clear();
            for (; count > 0; --count)
            {
                BreakPoint point = new BreakPoint();
                point.ReadFrom(reader);
                m_break_points.Add(point);
            }
        }

        public string ToResString()
        {
            StringBuilder sb = new StringBuilder();
            if(string.IsNullOrEmpty(m_head_desc) == false)
            {
                sb.AppendLine(m_head_desc);
            }
            foreach (var point in m_break_points)
            {
                sb.AppendLine(point.ToString());
            }
            return sb.ToString();
        }
    }

    public class OnBreakRes : DebugResponse
    {
        public string m_file;
        public int m_line;
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(m_file);
            writer.Write(m_line);
        }

        public void ReadFrom(BinaryReader reader)
        {
            m_file = reader.ReadString();
            m_line = reader.ReadInt32();
        }

        public string ToResString()
        {
            return string.Format("Break At {0}:{1}", m_file, m_line);
        }
    }

    public class BackTraceRes : DebugResponse
    {
        public List<Tuple<string, string, int>> m_frames = new List<Tuple<string, string, int>>();
        public string ToResString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Back Trace:");
            for (int i = 0; i < m_frames.Count; ++i)
            {
                var frame = m_frames[i];
                sb.AppendFormat("{0}. {1}:{2} {3}", i + 1, frame.Item1, frame.Item3, frame.Item2);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(m_frames.Count);
            foreach (var frame in m_frames)
            {
                writer.Write(frame.Item1);
                writer.Write(frame.Item2);
                writer.Write(frame.Item3);
            }
        }

        public void ReadFrom(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            m_frames.Clear();
            for (; count > 0; --count)
            {
                var file = reader.ReadString();
                var func_name = reader.ReadString();
                var line = reader.ReadInt32();
                m_frames.Add(new Tuple<string, string, int>(file, func_name, line));
            }
        }
    }

    public class GetFrameInfoRes: DebugResponse
    {
        public class ValueInfo
        {
            public string name;
            public string type;
            public string value;
        }

        public List<ValueInfo> m_locals = new List<ValueInfo>();
        public List<ValueInfo> m_upvalues = new List<ValueInfo>();

        public string ToResString()
        {
            throw new NotImplementedException();
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(m_locals.Count);
            foreach(var v in m_locals)
            {
                writer.Write(v.name);
                writer.Write(v.type);
                writer.Write(v.value);
            }
            writer.Write(m_upvalues.Count);
            foreach (var v in m_upvalues)
            {
                writer.Write(v.name);
                writer.Write(v.type);
                writer.Write(v.value);
            }
        }

        public void ReadFrom(BinaryReader reader)
        {
            int count;
            count = reader.ReadInt32();
            m_locals.Clear();
            for (var i = 0; i < count; ++i)
            {
                var v = new ValueInfo();
                v.name = reader.ReadString();
                v.type = reader.ReadString();
                v.value = reader.ReadString();
                m_locals.Add(v);
            }
            count = reader.ReadInt32();
            m_upvalues.Clear();
            for (var i = 0; i < count; ++i)
            {
                var v = new ValueInfo();
                v.name = reader.ReadString();
                v.type = reader.ReadString();
                v.value = reader.ReadString();
                m_upvalues.Add(v);
            }
        }
    }

    public class PrintRes: DebugResponse
    {
        public string m_name;
        public string m_type_name;
        public string m_value_str;

        public string ToResString()
        {
            return string.Format("{0} = {1}, type is {2}", m_name, m_value_str, m_type_name);
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(m_name);
            writer.Write(m_type_name);
            writer.Write(m_value_str);
        }

        public void ReadFrom(BinaryReader reader)
        {
            m_name = reader.ReadString();
            m_type_name = reader.ReadString();
            m_value_str = reader.ReadString();
        }
    }
}
