using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace SimpleScript
{
    static class NameHelper
    {
        public static string GetDelegateGeneraterName(Type t)
        {
            Debug.Assert(t.IsSubclassOf(typeof(Delegate)));
            var name = t.ToString();
            return "Gen_" + name
                       .Replace('.', '_')
                       .Replace('[', '_')
                       .Replace(']', '_')
                       .Replace('<', '_')
                       .Replace('>', '_')
                       .Replace('`', '_')
                       .Replace(',', '_')
                       .Replace('+', '_');
        }

        public static string GetTypeStr(Type t)
        {
            if (t.IsArray)
            {
                t = t.GetElementType();
                string str = GetTypeStr(t);
                str += "[]";
                return str;
            }
            else if (t.IsGenericType)
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
