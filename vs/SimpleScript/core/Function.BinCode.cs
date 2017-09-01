using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleScript
{
    static class MyStringBuilder
    {
        static StringBuilder _buffer = new StringBuilder();
        public static void AppendFormat(string format, params object[] args)
        {
            if (args.Length > 0)
            {
                _buffer.AppendFormat(format, args);
            }
            else
            {
                _buffer.Append(format);
            }

        }
        public static void Append(int indent, string format, params object[] args)
        {
            _buffer.Append(' ', indent * 2);
            if (args.Length > 0) // WTF
            {
                _buffer.AppendFormat(format, args);
            }
            else
            {
                _buffer.Append(format);
            }
        }
        public static void AppendLine(int indent, string format, params object[] args)
        {
            Append(indent, format, args);
            AppendLine();
        }
        public static void AppendLine()
        {
            _buffer.AppendLine();
        }

        public static void Clear()
        {
            _buffer.Clear();
        }

        public static new string ToString()
        {
            return _buffer.ToString();
        }
    }

    public partial class Function
    {
        public string ToBinCode()
        {
            MyStringBuilder.Clear();
            MyStringBuilder.AppendLine(0, "FileName: {0}", _file_name);

            Handle(this, 0);

            return MyStringBuilder.ToString();
        }

        private static void Handle(Function func , int indent)
        {
            MyStringBuilder.AppendLine(indent, "FuncName: {0}", func._func_name);
            MyStringBuilder.Append(indent, "Args: (");
            for(int i = 0; i < func._fixed_arg_count; ++i)
            {
                if (i != 0) MyStringBuilder.AppendFormat(", ");
                MyStringBuilder.AppendFormat(func.GetLocalVarNameByPc(i, 0));
            }
            if(func._has_vararg)
            {
                if(func._fixed_arg_count > 0) MyStringBuilder.AppendFormat(", ");
                MyStringBuilder.AppendFormat("...");
            }
            MyStringBuilder.AppendFormat(")");
            MyStringBuilder.AppendLine();

            // code
            MyStringBuilder.AppendLine(indent, "Code: {0}", func._codes.Count);
            for(int i = 0; i < func._codes.Count; ++i)
            {
                // write code
                var code = func._codes[i];
                HandleOneCode(func, indent, code);
            }

            // childs
            if(func._child_functions.Count > 0)
            {
                MyStringBuilder.AppendLine();
            }
            for (int i = 0; i < func._child_functions.Count; ++i)
            {
                MyStringBuilder.AppendLine(indent, "ChildFuncs: {0}/{1}", i+1, func._child_functions.Count);
                Handle(func._child_functions[i], indent + 1);
            }
        }

        private static void HandleOneCode(Function func, int indent, Instruction code)
        {
            MyStringBuilder.Append(indent, "{0} ", code.GetOp());

            switch(code.GetOp())
            {
                case OpType.OpType_InValid:
                    break;
                case OpType.OpType_LoadNil:
                    break;
                case OpType.OpType_LoadBool:
                    break;
                case OpType.OpType_LoadInt:
                    break;
                case OpType.OpType_LoadConst:
                    break;
                case OpType.OpType_Move:
                    break;
                case OpType.OpType_GetUpvalue:
                    break;
                case OpType.OpType_SetUpvalue:
                    break;
                case OpType.OpType_GetGlobal:
                    break;
                case OpType.OpType_SetGlobal:
                    break;
                case OpType.OpType_Closure:
                    break;
                case OpType.OpType_Call:
                    break;
                case OpType.OpType_VarArg:
                    break;
                case OpType.OpType_Ret:
                    break;
                case OpType.OpType_JmpFalse:
                    break;
                case OpType.OpType_JmpTrue:
                    break;
                case OpType.OpType_JmpNil:
                    break;
                case OpType.OpType_Jmp:
                    break;
                case OpType.OpType_Neg:
                    break;
                case OpType.OpType_Not:
                    break;
                case OpType.OpType_Len:
                    break;
                case OpType.OpType_Add:
                    break;
                case OpType.OpType_Sub:
                    break;
                case OpType.OpType_Mul:
                    break;
                case OpType.OpType_Div:
                    break;
                case OpType.OpType_Pow:
                    break;
                case OpType.OpType_Mod:
                    break;
                case OpType.OpType_Concat:
                    break;
                case OpType.OpType_Less:
                    break;
                case OpType.OpType_Greater:
                    break;
                case OpType.OpType_Equal:
                    break;
                case OpType.OpType_UnEqual:
                    break;
                case OpType.OpType_LessEqual:
                    break;
                case OpType.OpType_GreaterEqual:
                    break;
                case OpType.OpType_NewTable:
                    break;
                case OpType.OpType_AppendTable:
                    break;
                case OpType.OpType_SetTable:
                    break;
                case OpType.OpType_GetTable:
                    break;
                case OpType.OpType_TableIter:
                    break;
                case OpType.OpType_TableIterNext:
                    break;
                case OpType.OpType_ForInit:
                    break;
                case OpType.OpType_ForCheck:
                    break;
                case OpType.OpType_FillNilFromTopToA:
                    break;
                case OpType.OpType_CloseUpvalue:
                    break;
                default:
                    break;
            }

            MyStringBuilder.AppendLine();
        }
    }
}
