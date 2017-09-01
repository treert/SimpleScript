using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleScript.DebugProtocol
{
    class StreamUtils
    {
        private Stream _stream;
        private BinaryReader _reader;
        private BinaryWriter _writer;

        public StreamUtils(Stream stream)
        {
            _stream = stream;
            _reader = new BinaryReader(stream);
            _writer = new BinaryWriter(stream);

            Init();
        }

        Dictionary<string, Type> _name_map = new Dictionary<string, Type>();
        private void Init()
        {
            var assembly = typeof(DebugCmd).Assembly;
            var types = assembly.GetTypes();
            foreach(var type in types)
            {
                if(typeof(DebugCmd).IsAssignableFrom(type) || typeof(DebugResponse).IsAssignableFrom(type))
                {
                    _name_map.Add(type.Name, type);
                }
            }
        }

        public void WriteCmd(DebugCmd cmd)
        {
            try
            {
                _writer.Write(cmd.GetType().Name);
                cmd.WriteTo(_writer);
            }
            catch
            {
                Close();
            }
        }

        public DebugCmd ReadOneCmd()
        {
            DebugCmd cmd = null;
            try
            {
                string name = _reader.ReadString();
                Type type = null;
                if(_name_map.TryGetValue(name, out type))
                {
                    cmd = Activator.CreateInstance(type) as DebugCmd;
                    cmd.ReadFrom(_reader);
                }
                else
                {
                    throw new Exception("do not recognize "+ name);
                }
            }
            catch
            {
                cmd = null;
                Close();
            }
            return cmd;
        }

        public void WriteRes(DebugResponse res)
        {
            try
            {
                _writer.Write(res.GetType().Name);
                res.WriteTo(_writer);
            }
            catch
            {
                Close();
            }
        }

        public DebugResponse ReadOneRes()
        {
            DebugResponse res = null;
            try
            {
                string name = _reader.ReadString();
                Type type = null;
                if (_name_map.TryGetValue(name, out type))
                {
                    res = Activator.CreateInstance(type) as DebugResponse;
                    res.ReadFrom(_reader);
                }
                else
                {
                    throw new Exception("do not recognize " + name);
                }
            }
            catch
            {
                res = null;
                Close();
            }
            return res;
        }

        public bool IsWorking()
        {
            return _stream != null;
        }

        public void Close()
        {
            if(IsWorking())
            {
                _reader.Dispose();
                _writer.Dispose();
                _reader = null;
                _writer = null;
                _stream = null;
            }
        }
    }

    public class NetServerPipe : DebugPipeServer
    {
        public DebugCmd ReadCmd()
        {
            if(_stream != null)
            {
                var cmd = _stream.ReadOneCmd();
                if (cmd != null)
                {
                    return cmd;
                }
                else
                {
                    _stream.Close();
                    _stream = null;
                }
            }
            return new Terminate();
        }

        public void WriteRes(DebugResponse res)
        {
            if(_stream != null)
            {
                _stream.WriteRes(res);
            }
        }

        private StreamUtils _stream;

        public NetServerPipe(Stream stream)
        {
            _stream = new StreamUtils(stream);
        }
    }
}
