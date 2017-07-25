using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace SimpleScript
{
    public static class ImportCodeGenerate
    {
        private static StringBuilder _buffer = new StringBuilder();
        private static List<string> _using_list = new List<string>();
        private static void AppendFormat(string format, params object[] args)
        {
            if(args.Length > 0)
            {
                _buffer.AppendFormat(format, args);
            }
            else
            {
                _buffer.Append(format);
            }
            
        }
        private static void Append(int indent, string format, params object[] args)
        {
            _buffer.Append(' ', indent*4);
            if(args.Length > 0) // WTF
            {
                _buffer.AppendFormat(format, args);
            }
            else
            {
                _buffer.Append(format);
            }
        }
        private static void AppendLine(int indent, string format, params object[] args)
        {
            Append(indent, format, args);
            AppendLine();
        }
        private static void AppendLine()
        {
            _buffer.AppendLine();
        }

        public static void Clear()
        {
            _buffer.Clear();
            _using_list.Clear();
        }


        public static void GenDelegateFactorySource(string filepath, Type[] list)
        {
            Clear();

            string[] func_name_list = new string[list.Length];
            for(int i = 0; i < list.Length; ++i)
            {
                func_name_list[i] = NameHelper.GetDelegateGeneraterName(list[i]);
            }

            string filename = Path.GetFileNameWithoutExtension(filepath);
            _using_list.Add("System");
            _using_list.Add("System.Collections.Generic");

            AppendLine(0, "public static class {0}",filename);
            AppendLine(0, "{");

            AppendLine(1, "public static void RegisterDelegateGenerater({0} vm)", typeof(VM));
            AppendLine(1, "{");
            for (int i = 0; i < list.Length; ++i)
            {
                string str_type = NameHelper.GetTypeStr(list[i]);
                string name = NameHelper.GetDelegateGeneraterName(list[i]);
                AppendLine(2, "vm.RegisterDelegateGenerater(typeof({0}), {1});", str_type, name);
            }
            AppendLine(1, "}");

            for (int i = 0; i < list.Length; i++)
            {
                Type t = list[i];
                string str_type = NameHelper.GetTypeStr(t);
                string name = NameHelper.GetDelegateGeneraterName(t);

                AppendLine(1, "public static Delegate {0}(Closure closure)", name);
                AppendLine(1, "{");

                Append(2,"{0} d = ", str_type);
                GenDelegateBody(t, 2);
                AppendLine(2, "return d;");

                AppendLine(1, "}");
            }

            AppendLine(0, "}");

            SaveToFile(filepath);
        }

        static void GenDelegateBody(Type t, int indent)
        {
            MethodInfo mi = t.GetMethod("Invoke");
            ParameterInfo[] pi = mi.GetParameters();
            int n = pi.Length;

            string[] arg_names = new string[pi.Length];
            for (int i = 0; i < pi.Length; ++i)
            {
                arg_names[i] = "param" + i;
            }
            string args_str = string.Join(", ", arg_names);

            AppendLine(0, "({0}) =>", args_str);

            if (mi.ReturnType == typeof(void))
            {
                AppendLine(indent, "{");
                AppendLine(indent + 1, "closure.Call({0});", args_str);
                AppendLine(indent, "};");
            }
            else
            {
                AppendLine(indent, "{");
                AppendLine(indent + 1, "object[] objs = closure.Call({0});", args_str);
                AppendLine(indent + 1, "return ({0})objs[0];", NameHelper.GetTypeStr(mi.ReturnType));
                AppendLine(indent, "};");
            }
        }

        public static void SaveToFile(string filepath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));

            //File.WriteAllText(filepath, _buffer.ToString(), Encoding.UTF8);
            using (StreamWriter textWriter = new StreamWriter(filepath, false, Encoding.UTF8))
            {
                StringBuilder usb = new StringBuilder();

                foreach (string str in _using_list)
                {
                    usb.AppendFormat("using {0};", str);
                    usb.AppendLine();
                }

                usb.AppendLine("using SimpleScript;");

                //if (ambig == ObjAmbig.All)
                //{
                //    usb.AppendLine("using Object = UnityEngine.Object;");
                //}

                usb.AppendLine();

                textWriter.Write(usb.ToString());
                textWriter.Write(_buffer.ToString());
                textWriter.Flush();
                textWriter.Close();
            }
        }
    }

    static class NameHelper
    {
        public static string GetDelegateGeneraterName(Type t)
        {
            Debug.Assert(t.IsSubclassOf(typeof(Delegate)));
            var name = t.ToString();
            return "Gen_" + name
                       .Replace('.','_')
                       .Replace('[','_')
                       .Replace(']','_')
                       .Replace('<','_')
                       .Replace('>','_')
                       .Replace('`','_')
                       .Replace(',','_')
                       .Replace('+','_');
        }

        public static string GetTypeStr(Type t)
        {
            if(t.IsArray)
            {
                t = t.GetElementType();
                string str = GetTypeStr(t);
                str += "[]";
                return str;
            }
            else if(t.IsGenericType)
            {
                return GetGenericName(t);
            }
            else
            {
                return _C(t.ToString());
            }
        }
        static string GetGenericName(Type t)
        {
            Type[] gArgs = t.GetGenericArguments();
            string typeName = t.FullName;
            string pureTypeName = typeName.Substring(0, typeName.IndexOf('`'));
            pureTypeName = _C(pureTypeName);

            if (typeName.Contains("+"))
            {
                int pos1 = typeName.IndexOf("+");
                int pos2 = typeName.IndexOf("[");

                if (pos2 > pos1)
                {
                    string add = typeName.Substring(pos1 + 1, pos2 - pos1 - 1);
                    return pureTypeName + "<" + string.Join(",", GetGenericName(gArgs)) + ">." + add;
                }
                else
                {
                    return pureTypeName + "<" + string.Join(",", GetGenericName(gArgs)) + ">";
                }
            }
            else
            {
                return pureTypeName + "<" + string.Join(",", GetGenericName(gArgs)) + ">";
            }
        }

        static string[] GetGenericName(Type[] types)
        {
            string[] results = new string[types.Length];

            for (int i = 0; i < types.Length; i++)
            {
                results[i] = GetTypeStr(types[i]);
            }

            return results;
        }

        public static string _C(string str)
        {
            if (str.Length > 1 && str[str.Length - 1] == '&')
            {
                str = str.Remove(str.Length - 1);// Why
            }

            if (str == "System.Single" || str == "Single")
            {
                return "float";
            }
            else if (str == "System.String" || str == "String")
            {
                return "string";
            }
            else if (str == "System.Int32" || str == "Int32")
            {
                return "int";
            }
            else if (str == "System.Int64" || str == "Int64")
            {
                return "long";
            }
            else if (str == "System.SByte" || str == "SByte")
            {
                return "sbyte";
            }
            else if (str == "System.Byte" || str == "Byte")
            {
                return "byte";
            }
            else if (str == "System.Int16" || str == "Int16")
            {
                return "short";
            }
            else if (str == "System.UInt16" || str == "UInt16")
            {
                return "ushort";
            }
            else if (str == "System.Char" || str == "Char")
            {
                return "char";
            }
            else if (str == "System.UInt32" || str == "UInt32")
            {
                return "uint";
            }
            else if (str == "System.UInt64" || str == "UInt64")
            {
                return "ulong";
            }
            else if (str == "System.Decimal" || str == "Decimal")
            {
                return "decimal";
            }
            else if (str == "System.Double" || str == "Double")
            {
                return "double";
            }
            else if (str == "System.Boolean" || str == "Boolean")
            {
                return "bool";
            }
            else if (str == "System.Object")
            {
                return "object";
            }

            if (str.Contains("+"))
            {
                return str.Replace('+', '.');
            }

            return str;
        }
    }
}
