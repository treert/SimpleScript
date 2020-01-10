using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleScript
{
    public static class ConvertHelper
    {
        // this type convert to double, has no accuracy miss
        // add enum
        // !!! convert from double to them, may have accuracy overflow 
        // double support up to 2^53;
        private static HashSet<Type> _number_types = new HashSet<Type>()
        {
            typeof(sbyte),typeof(byte),
            typeof(Int16),typeof(UInt16),
            typeof(Int32),typeof(UInt32),
            typeof(float),
            typeof(char),
        };

        public static object CheckAndConvertFromSSToCS(object obj, Type target_type)
        {
            if (target_type.IsEnum && obj is double)
            {
                return Enum.ToObject(target_type, Convert.ToInt64(obj));
            }

            if (target_type.IsPrimitive && obj is double && _number_types.Contains(target_type))
            {
                throw new Exception();
                // return Convert.ChangeType(obj, target_type);// todo error
            }

            // todo convert closure to delegate
            // has not find a good way to do this, just do not deal it now
            // in this situation, use ss function in callback, need a special C# function who's param type is Closure
            // maybe can use a DelegateFactory later
            // keep old comment
            if (obj is Closure && typeof(System.Delegate).IsAssignableFrom(target_type))
            {
                var closure = obj as Closure;
                var callback = closure.ConvertToDelegate(target_type);
                if (callback != null)
                {
                    return callback;
                }
                else
                {
                    throw new CFunctionException("can not convert Clourse to {0}", target_type);
                }
            }

            if (obj == null)
            {
                if (target_type.IsValueType)
                {
                    throw new CFunctionException("{0} is ValueType, can not assign from null", target_type);
                }
            }
            else if (target_type.IsAssignableFrom(obj.GetType()) == false)
            {
                throw new CFunctionException("{0} can not assign from {1}", target_type, obj.GetType());
            }
            return obj;
        }

        public static object ConvertFromCSToSS(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            Type t = obj.GetType();

            if (t.IsEnum)
            {
                return Convert.ToDouble(obj);
            }

            if (t.IsPrimitive && _number_types.Contains(t))
            {
                return Convert.ToDouble(obj);
                // https://stackoverflow.com/questions/12647068/invalid-cast-exception-on-int-to-double
                // return (double)obj; // WTF error
            }

            // other just keep same
            return obj;
        }
    }
}
