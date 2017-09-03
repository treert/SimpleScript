using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;


namespace SimpleScript
{
    public class ImportTypeHandler : IUserData, IImportTypeHandler
    {
        Type _type = null;

        Dictionary<string, FieldInfo> _fields = new Dictionary<string, FieldInfo>();
        Dictionary<string, PropertyInfo> _propertys = new Dictionary<string, PropertyInfo>();
        Dictionary<string, CFunction> _methods = new Dictionary<string, CFunction>();
        Dictionary<string, MethodInfo> _debug_methods = new Dictionary<string, MethodInfo>();

        Dictionary<string, FieldInfo> _static_fields = new Dictionary<string, FieldInfo>();
        Dictionary<string, PropertyInfo> _static_propertys = new Dictionary<string, PropertyInfo>();
        Dictionary<string, CFunction> _static_methods = new Dictionary<string, CFunction>();
        Dictionary<string, MethodInfo> _debug_static_methods = new Dictionary<string, MethodInfo>();

        CFunction _new_func = null;

        public static ImportTypeHandler Create(Type t)
        {
            var obj = new ImportTypeHandler();
            obj._type = t;
            if (t.IsInterface || t.ContainsGenericParameters)
            {
                return obj;
            }

            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                obj._fields[field.Name] = field;
            }
            foreach (var field in t.GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                obj._static_fields[field.Name] = field;
            }
            foreach (var property in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                obj._propertys[property.Name] = property;
            }
            foreach (var property in t.GetProperties(BindingFlags.Static | BindingFlags.Public))
            {
                obj._static_propertys[property.Name] = property;
            }
            foreach (var method in t.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                obj._methods[method.Name] = ReflectionHelper.GenerateCFunctionFromMethodInfo(method);
                obj._debug_methods[method.Name] = method;
            }
            foreach (var method in t.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                obj._static_methods[method.Name] = ReflectionHelper.GenerateCFunctionFromMethodInfo(method);
                obj._debug_static_methods[method.Name] = method;
            }
            // new
            obj._new_func = ReflectionHelper.GenerateCFunctionForNew(t);
            return obj;
        }

        public object GetValueFromCSToSS(object obj, object key)
        {
            if (key is string)
            {
                var name = (string)key;

                if (_fields.ContainsKey(name))
                {
                    var field = _fields[name];
                    return ConvertHelper.ConvertFromCSToSS(field.GetValue(obj));
                }
                else if (_propertys.ContainsKey(name))
                {
                    return ConvertHelper.ConvertFromCSToSS(_propertys[name].GetValue(obj, null));
                }
                else if (_methods.ContainsKey(name))
                {
                    return _methods[name];
                }
                throw new CFunctionException("attempt to get {0} from {1}", name, _type);
            }

            throw new CFunctionException("attempt to get from {0} with key type {1}", _type, key.GetType());
        }

        public void SetValueFromSSToCS(object obj, object key, object value)
        {
            if (key is string)
            {
                var name = (string)key;

                if (_fields.ContainsKey(name))
                {
                    var field = _fields[name];
                    field.SetValue(obj, ConvertHelper.CheckAndConvertFromSSToCS(value, field.FieldType));
                }
                else if (_propertys.ContainsKey(name))
                {
                    var property = _propertys[name];
                    property.SetValue(obj, ConvertHelper.CheckAndConvertFromSSToCS(value, property.PropertyType), null);
                }
                throw new CFunctionException("attempt to set {0} for {1}", name, _type);
            }
            throw new CFunctionException("attempt to get {0} with key type {1}", _type, obj.GetType());
        }

        public object Get(object name)
        {
            if (name is string)
            {
                string key = (string)name;
                if (key == "new")
                {
                    return _new_func;// can use it to check whether the type is support
                }
                else if (_static_fields.ContainsKey(key))
                {
                    var field = _static_fields[key];
                    return ConvertHelper.ConvertFromCSToSS(field.GetValue(null));
                }
                else if (_static_propertys.ContainsKey(key))
                {
                    return ConvertHelper.ConvertFromCSToSS(_static_propertys[key].GetValue(null, null));
                }
                else if (_static_methods.ContainsKey(key))
                {
                    return _static_methods[key];
                }
                throw new CFunctionException("attempt to get {0} from {1}", name, _type);
            }

            throw new CFunctionException("attempt to get from {0} with key type {1}", _type, name.GetType());
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.AppendFormat("=== {0} ===\n", _type);
            buffer.AppendLine("fields:");
            foreach (var item in _fields)
            {
                buffer.AppendFormat("    {0} \t: {1}\n", item.Key, item.Value.FieldType);
            }
            buffer.AppendLine("static fields:");
            foreach (var item in _static_fields)
            {
                buffer.AppendFormat("    {0} \t: {1}\n", item.Key, item.Value.FieldType);
            }
            buffer.AppendLine("propertys:");
            foreach (var item in _propertys)
            {
                buffer.AppendFormat("    {0} \t: {1}\n", item.Key, item.Value.PropertyType);
            }
            buffer.AppendLine("static propertys:");
            foreach (var item in _static_propertys)
            {
                buffer.AppendFormat("    {0} \t: {1}\n", item.Key, item.Value.PropertyType);
            }
            buffer.AppendLine("method:");
            foreach (var item in _methods)
            {
                buffer.AppendFormat("    {0} \t{1}(", _debug_methods[item.Key].ReturnType.Name, item.Key);
                foreach (var param in _debug_methods[item.Key].GetParameters())
                {
                    buffer.AppendFormat("{0} {1}, ", param.ParameterType.Name, param.Name);
                }
                buffer.AppendLine(")");
            }
            buffer.AppendLine("static method:");
            foreach (var item in _static_methods)
            {
                buffer.AppendFormat("    {0} \t{1}(", _debug_static_methods[item.Key].ReturnType.Name, item.Key);
                foreach (var param in _debug_static_methods[item.Key].GetParameters())
                {
                    buffer.AppendFormat("{0} {1}, ", param.ParameterType.Name, param.Name);
                }
                buffer.AppendLine(")");
            }
            buffer.AppendFormat("=== {0} === end\n", _type);
            return buffer.ToString();
        }

        public void Set(object name, object value)
        {
            throw new CFunctionException("attempt to set {0}", _type);
        }
    }

    class ReflectionHelper
    {
        public static CFunction GenerateCFunctionFromMethodInfo(MethodInfo method)
        {
            if (method.ContainsGenericParameters)
                return null;

            // 
            // out: param set null, get result after invoke
            // ref: get result after invoke
            // 
            // limit
            // 1. fixed param count, support default value
            // 2. fixed return count
            ParameterInfo[] param_arr = method.GetParameters();
            int param_total_count = param_arr.Count();
            int param_need_count = param_total_count;
            List<int> out_idx_arr = new List<int>();
            for (int i = 0; i < param_total_count; ++i)
            {
                var param = param_arr[i];
                if (param.IsOut || param.ParameterType.IsByRef)
                {
                    out_idx_arr.Add(i);
                }
                if (param.IsOptional)
                {
                    param_need_count--;
                }
            }
            int obj_extra = (method.IsStatic ? 0 : 1);

            CFunction cfunc = (Thread th) =>
            {
                int argc = th.GetCFunctionArgCount();
                // todo check param
                if (argc < param_need_count + obj_extra)
                {
                    throw new CFunctionException("args count less, {0} {1}", method.DeclaringType, method.Name);
                }

                object obj = null;
                if (obj_extra == 1)
                {
                    // class obj
                    obj = ConvertHelper.CheckAndConvertFromSSToCS(th.GetCFunctionArg(0), method.DeclaringType);
                }
                object[] args = new object[param_total_count];
                for (int i = 0; i < param_total_count; ++i)
                {
                    if (i + obj_extra < argc)
                    {
                        args[i] = ConvertHelper.CheckAndConvertFromSSToCS(th.GetCFunctionArg(i + obj_extra), param_arr[i].ParameterType);
                    }
                    else
                    {
                        if (param_arr[i].IsOptional == false)
                        {
                            throw new CFunctionException("{0} {1} miss {2} arg", method.DeclaringType, method.Name, i + 1);
                        }
                        args[i] = param_arr[i].DefaultValue;
                    }
                }

                object ret = method.Invoke(obj, args.ToArray());
                th.PushValue(ret);
                for (int i = 0; i < out_idx_arr.Count; ++i)
                {
                    th.PushValue(args[out_idx_arr[i]]);
                }
                return out_idx_arr.Count + 1;
            };
            return cfunc;
        }

        static bool CheckMatchParams(Thread th, MethodBase method, out object[] out_args, out int[] out_extra_out_idxes)
        {
            out_extra_out_idxes = null;// only used by constructor by now
            out_args = null;
            if (method.ContainsGenericParameters)
            {
                return false;
            }

            ParameterInfo[] param_arr = method.GetParameters();
            List<int> out_idx_arr = new List<int>();
            int obj_extra = ((method.IsStatic | method.IsConstructor) ? 0 : 1);

            int argc = th.GetCFunctionArgCount();

            object[] args = new object[param_arr.Length];
            for (int i = 0; i < param_arr.Length; ++i)
            {
                var param = param_arr[i];
                if (i + obj_extra < argc)
                {
                    try
                    {
                        args[i] = ConvertHelper.CheckAndConvertFromSSToCS(th.GetCFunctionArg(i + obj_extra), param.ParameterType);
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    if (param.IsOptional == false)
                    {
                        return false;
                    }
                    args[i] = param.DefaultValue;
                }

                if (param.IsOut || param.ParameterType.IsByRef)
                {
                    out_idx_arr.Add(i);
                }
            }
            out_args = args;
            out_extra_out_idxes = out_idx_arr.ToArray();
            return true;
        }

        public static CFunction GenerateCFunctionForNew(Type t)
        {
            CFunction cfunc = (Thread th) =>
            {
                if (th.GetCFunctionArgCount() == 0)
                {
                    object obj = Activator.CreateInstance(t);
                    th.PushValue(obj);
                    return 1;
                }
                // todo@om has bug, does not work
                foreach (var constructor in t.GetConstructors())
                {
                    object[] args = null;
                    int[] out_idxes = null;
                    bool is_ok = CheckMatchParams(th, constructor, out args, out out_idxes);
                    if (is_ok)
                    {
                        object obj = constructor.Invoke(args);
                        th.PushValue(obj);
                        return 1;
                    }
                }
                return 0;
            };
            return cfunc;
        }
    }

}
