using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleScript
{
    public partial class Function
    {
        public static Function Deserialize(BinaryReader reader)
        {
            // all strings, will be reuse
            int count = reader.ReadInt32();
            List<string> str_list = new List<string>(count);
            for (int i = 0; i < count; ++i)
            {
                str_list.Add(reader.ReadString());
            }

            Function ret = new Function();
            Stack<Function> stack = new Stack<Function>();
            stack.Push(ret);
            // function
            while (stack.Count > 0)
            {
                var func = stack.Pop();
                // module_name
                func._file_name = reader.ReadString();
                func._func_name = reader.ReadString();
                // codes
                count = reader.ReadInt32();
                func._codes.Capacity = count;
                for (int i = 0; i < count; ++i)
                {
                    func._codes.Add(Instruction.ConvertFrom(reader.ReadInt32()));
                }
                // consts
                count = reader.ReadInt32();
                func._const_objs.Capacity = count;
                for (int i = 0; i < count; ++i)
                {
                    if (reader.ReadBoolean())
                    {
                        func._const_objs.Add(reader.ReadDouble());
                    }
                    else
                    {
                        func._const_objs.Add(str_list[reader.ReadInt32()]);
                    }
                }
                // upvalues
                count = reader.ReadInt32();
                func._upvalues.Capacity = count;
                for (int i = 0; i < count; ++i)
                {
                    int register = reader.ReadInt32();
                    bool is_parent_local = reader.ReadBoolean();
                    string name = str_list[reader.ReadInt32()];
                    func._upvalues.Add(new UpValueInfo(name, register, is_parent_local));
                }
                // local vars 
                count = reader.ReadInt32();
                func._local_var_infos.Capacity = count;
                for (int i = 0; i < count; ++i)
                {
                    int register = reader.ReadInt32();
                    int begin_pc = reader.ReadInt32();
                    int end_pc = reader.ReadInt32();
                    string name = str_list[reader.ReadInt32()];
                    func._local_var_infos.Add(new LocalVarInfo(name, register, begin_pc, end_pc));
                }
                // codes line
                count = reader.ReadInt32();
                func._code_lines.Capacity = count;
                for (int i = 0; i < count; ++i)
                {
                    func._code_lines.Add(reader.ReadInt32());
                }
                // other
                func._fixed_arg_count = reader.ReadInt32();
                func._has_vararg = reader.ReadBoolean();
                func._max_register_count = reader.ReadInt32();

                // childs, Must handle at last
                count = reader.ReadInt32();
                func._child_functions.Capacity = count;
                for (int i = 0; i < count; ++i)
                {
                    Function child = new Function();
                    func._child_functions.Add(child);
                    stack.Push(child);
                }
            }
            return ret;
        }

        public void Serialize(BinaryWriter writer)
        {
            // all strings, will be reuse
            Dictionary<string, int> str_id_map = new Dictionary<string, int>();
            List<string> str_list = new List<string>();
            foreach (var str in GetAllStringsRecurse())
            {
                if (str_id_map.ContainsKey(str) == false)
                {
                    str_id_map.Add(str, str_id_map.Count);
                    str_list.Add(str);
                }
            }
            // serialize data begin
            // strings
            {
                writer.Write(str_list.Count);
                foreach (var str in str_list)
                {
                    writer.Write(str);
                }
            }
            // function
            Stack<Function> stack = new Stack<Function>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                var func = stack.Pop();
                // module_name
                writer.Write(func._file_name);
                writer.Write(func._func_name);
                //  codes
                writer.Write(func._codes.Count);
                foreach (var code in func._codes)
                {
                    writer.Write(code.GetCode());
                }
                // consts
                writer.Write(func._const_objs.Count);
                foreach (var obj in func._const_objs)
                {
                    if (obj is double)
                    {
                        writer.Write(true);
                        writer.Write((double)obj);
                    }
                    else
                    {
                        writer.Write(false);
                        writer.Write(str_id_map[(string)obj]);
                    }
                }
                // upvalues
                writer.Write(func._upvalues.Count);
                foreach (var upvalue in func._upvalues)
                {
                    writer.Write(upvalue.register);
                    writer.Write(upvalue.is_parent_local);
                    writer.Write(str_id_map[upvalue.name]);// For debug
                }
                // local vars 
                writer.Write(func._local_var_infos.Count);// For debug
                foreach (var local_var in func._local_var_infos)
                {
                    writer.Write(local_var.register_idx);
                    writer.Write(local_var.begin_pc);
                    writer.Write(local_var.end_pc);
                    writer.Write(str_id_map[local_var.name]);
                }
                // codes line
                writer.Write(func._code_lines.Count);// For debug
                foreach (var line in func._code_lines)
                {
                    writer.Write(line);
                }
                // other
                writer.Write(func._fixed_arg_count);
                writer.Write(func._has_vararg);
                writer.Write(func._max_register_count);

                // childs, Must handle at last
                writer.Write(func._child_functions.Count);
                foreach (var child in func._child_functions)
                {
                    stack.Push(child);
                }
            }
        }

        IEnumerable<string> GetAllStringsRecurse()
        {
            foreach (var obj in _const_objs)
            {
                if (obj is string)
                    yield return obj as string;
            }
            foreach (var upvalue in _upvalues)
            {
                yield return upvalue.name;
            }
            foreach (var local_var in _local_var_infos)
            {
                yield return local_var.name;
            }
            foreach (var func in _child_functions)
            {
                foreach (var str in func.GetAllStringsRecurse())
                    yield return str;
            }
        }

    }
}
