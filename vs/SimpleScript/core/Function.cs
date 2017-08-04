using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SimpleScript
{
    /// <summary>
    /// 静态函数结构，（可序列化成编译后代码）
    /// 1. 指令数组
    /// 2. 常量(字符串，数字)
    /// 3. 局部变量
    /// 4. 子函数
    /// </summary>
    [Serializable]
    public class Function
    {
        public Function()
        {
        }

#region Serialize
        public static Function Deserialize(BinaryReader reader)
        {
            // all strings, will be reuse
            int count = reader.ReadInt32();
            List<string> str_list = new List<string>(count);
            for(int i = 0; i < count; ++i)
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
                    if(reader.ReadBoolean())
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
                func._MaxRegisterCount = reader.ReadInt32();

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
            foreach(var str in GetAllStringsRecurse())
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
                foreach(var str in str_list)
                {
                    writer.Write(str);
                }
            }
            // function
            Stack<Function> stack = new Stack<Function>();
            stack.Push(this);
            while(stack.Count > 0)
            {
                var func = stack.Pop();
                // module_name
                writer.Write(func._file_name);
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
                foreach(var local_var in func._local_var_infos)
                {
                    writer.Write(local_var.register_idx);
                    writer.Write(local_var.begin_pc);
                    writer.Write(local_var.end_pc);
                    writer.Write(str_id_map[local_var.name]);
                }
                // codes line
                writer.Write(func._code_lines.Count);// For debug
                foreach(var line in func._code_lines)
                {
                    writer.Write(line);
                }
                // other
                writer.Write(func._fixed_arg_count);
                writer.Write(func._has_vararg);
                writer.Write(func._MaxRegisterCount);

                // childs, Must handle at last
                writer.Write(func._child_functions.Count);
                foreach(var child in func._child_functions)
                {
                    stack.Push(child);
                }
            }
        }

        IEnumerable<string> GetAllStringsRecurse()
        {
            foreach(var obj in _const_objs)
            {
                if (obj is string)
                    yield return obj as string;
            }
            foreach(var upvalue in _upvalues)
            {
                yield return upvalue.name;
            }
            foreach(var local_var in _local_var_infos)
            {
                yield return local_var.name;
            }
            foreach (var func in _child_functions)
            {
                foreach (var str in func.GetAllStringsRecurse())
                    yield return str;
            }
        }
#endregion

        public int OpCodeSize()
        {
            return _codes.Count;
        }

        public void FillInstructionBx(int index, int bx)
        {
            var i = _codes[index];
            i.SetBx(bx);
            _codes[index] = i;
        }
        // 添加指令，返回添加的指令的index
        public int AddInstruction(Instruction i, int line)
        {
            _codes.Add(i);
            _code_lines.Add(line);
            return _codes.Count - 1;
        }
        public Instruction GetInstruction(int idx)
        {
            return _codes[idx];
        }
        public int GetInstructionLine(int idx)
        {
            return _code_lines[idx];
        }
        public void SetFixedArgCount(int fixed_arg_count_)
        {
            _fixed_arg_count = fixed_arg_count_;
        }
        public int GetFixedArgCount()
        {
            return _fixed_arg_count;
        }
        public void SetHasVarArg()
        {
            _has_vararg = true;
        }
        public bool HasVararg()
        {
            return _has_vararg;
        }
        public int AddConstNumber(double num)
        {
            _const_objs.Add(num);
            return _const_objs.Count - 1;
        }
        public int AddConstString(string str)
        {
            _const_objs.Add(str);
            return _const_objs.Count - 1;
        }
        public object GetConstValue(int i)
        {
            return _const_objs[i];
        }
        public int AddChildFunction(Function child)
        {
            _child_functions.Add(child);
            return _child_functions.Count - 1;
        }
        public Function GetChildFunction(int idx)
        {
            return _child_functions[idx];
        }
        public void AddLocalVar(string name,int register, int begin_pc, int end_pc)
        {
            Debug.Assert(begin_pc < end_pc);
            _local_var_infos.Add(new LocalVarInfo(name, register, begin_pc, end_pc));
        }
        public string GetLocalVarNameByPc(int register, int pc)
        {
            // a little trick
            foreach(var info in _local_var_infos)
            {
                if(info.register_idx == register &&
                    info.begin_pc <= pc && pc < info.end_pc)
                {
                    return info.name;
                }
            }
            return null;
        }
        public int GetMaxRegisterCount()
        {
            return _MaxRegisterCount;
        }
        public void SetMaxRegisterCount(int count)
        {
            // !!! 实现好像有错误，这个貌似可以设置设成256最大值。
            _MaxRegisterCount = Math.Max(_MaxRegisterCount, count);
        }

        int _MaxRegisterCount = 0;// 需要的最大寄存器数量

        public string GetFileName()
        {
            return _file_name;
        }
        public void SetFileName(string moudle_name_)
        {
            _file_name = moudle_name_;
        }
        public string GetFuncName()
        {
            return _func_name;
        }
        public void SetFuncName(string func_name_)
        {
            _func_name = func_name_;
        }
        string _file_name = string.Empty;
        string _func_name = string.Empty;

        List<Function> _child_functions = new List<Function>();
        List<object> _const_objs = new List<object>();
        bool _has_vararg = false;
        int _fixed_arg_count = 0;
        List<Instruction> _codes = new List<Instruction>();
        List<int> _code_lines = new List<int>();

        // For debug
        struct LocalVarInfo
        {
            public string name;
            public int register_idx;
            public int begin_pc;
            public int end_pc;
            public LocalVarInfo(string name_, int register_idx_, int begin_pc_, int end_pc_)
            {
                name = name_;
                register_idx = register_idx_;
                begin_pc = begin_pc_;
                end_pc = end_pc_;
            }
        }
        List<LocalVarInfo> _local_var_infos = new List<LocalVarInfo>();

        #region upvalue

        public int AddUpValue(string name, int register, bool parent_local)
        {
            _upvalues.Add(new UpValueInfo(name, register, parent_local));
            return _upvalues.Count - 1;
        }
        public int GetUpValueCount()
        {
            return _upvalues.Count;
        }
        public UpValueInfo GetUpValueInfo(int idx)
        {
            return _upvalues[idx];
        }
        // For CodeGenerate
        public int SearchUpValue(string name)
        {
            for (int i = 0; i < _upvalues.Count; ++i)
            {
                if (_upvalues[i].name == name)
                    return i;
            }
            return -1;
        }

        public class UpValueInfo
        {
            public string name;
            public int register;
            public bool is_parent_local;
            public UpValueInfo(string name_, int register_, bool is_parent_local_)
            {
                name = name_;
                register = register_;
                is_parent_local = is_parent_local_;
            }
        }

        List<UpValueInfo> _upvalues = new List<UpValueInfo>();
        #endregion
    }
}
