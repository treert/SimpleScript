using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Serialization;

/*
 * 二进制序列化工具
 * 具体类型序列化组件
 * 
 * 零散的知识：
 * 1. SetValue 和 SetValueDirect的区别， SetValue不能给值类型赋值，因为参数类型是object，会有一次装箱，然后就与原来的值无关了。
 *     【这里用的FormatterServices.GetUninitializedObject创建的就是object，并不会遇到这种问题】
 *      http://bbs.csdn.net/topics/390345584 使用FiledInfo修改struct，使用了TypedReference。
 *      http://bbs.csdn.net/topics/391011879 这个解决方案更简单。
 *      - 还有中方法`TypedReference tf = __makeref(struct_obj); field.SetValueDirect(tf,"xx")`。
 *          > https://stackoverflow.com/questions/1711393/practical-uses-of-typedreference
 * 2. MethodInfo.MakeGenericMethod 用来设置模版函数的模版类型参数，只能用于泛型函数
 *      - 注意！！ 这个函数会返回一个新的MethodInfo，不改变原来的
 * 3. Type.IsArray 针对的是`int[]`这种类型的，`List<int>`返回false
 * 
 * > https://github.com/tomba/netserializer
 */
namespace SimpleScript.Serialize
{


    /***************************** object very special ******************************/
    class XserializeObject :XTypeSerializer
    {
        public override bool Handles(Type type)
        {
            return typeof(object) == type;
        }

        public override object Read(XSerializer serializer, BinaryReader reader, Type type)
        {
            return new object();
        }

        public override void Write(XSerializer serializer, BinaryWriter writer, object obj)
        {
            Debug.Assert(typeof(object) == obj.GetType());
            return;
        }
    }
    /***************************** Class & Struct ***********************************/
    /**
     * 通用类序列化，这个放到最后面
     */
    class XSerializeClass: XTypeSerializer
    {
        // 这个类的实例，每个serializer会有一个。
        Dictionary<Type, FieldInfo[]> _class_fields = new Dictionary<Type, FieldInfo[]>();

        public override bool Handles(Type type)
        {
            // todo@om 可以考虑加上
            //if(type.IsSerializable)
            //{
            //    throw new Exception(String.Format("Class {0} has not add [Serializable]"));
            //}
            
            if(type.IsClass)
            {
                return true;
            }
            else if(type.IsValueType)
            {
                return !type.IsPrimitive && !type.IsEnum;
            }
            return false;
        }

        public override IEnumerable<Type> AddSubtypes(XSerializer serializer, Type type)
        {
            if(type.IsAbstract || type.IsInterface)
            {
                yield break;// can not use new Type[0]
            }
            else
            {
                var fields = XHelper.GetFieldInfos(type);
                var fields_array = fields.ToArray();
                _class_fields.Add(type, fields_array);// can optimize

                foreach (var field in fields_array)
                {
                    yield return field.FieldType;
                }
            }
        }

        public override object Read(XSerializer serializer, BinaryReader reader, Type type)
        {
            var fields = _class_fields[type];
            object obj = FormatterServices.GetUninitializedObject(type);
            foreach(var field in fields)
            {
                var val = serializer.InternalRead(reader, field.FieldType);
                field.SetValue(obj, val);
            }
            return obj;
        }

        public override void Write(XSerializer serializer, BinaryWriter writer, object obj)
        {
            var fields = _class_fields[obj.GetType()];
            foreach(var field in fields)
            {
                var val = field.GetValue(obj);
                serializer.InternalWrite(writer, val, field.FieldType);
            }
        }
    }
    /***************************** generic Contains ***********************************/
    // 序列化泛型的基类，方便写代码
    abstract class XSerializeGenericTypeBase : XTypeSerializer
    {
        public override bool Handles(Type type)
        {
            if (!type.IsGenericType)
                return false;

            return type.GetGenericTypeDefinition() == GetWorkGenericType();
        }

        public abstract Type GetWorkGenericType();

        public override IEnumerable<Type> AddSubtypes(XSerializer serializer, Type type)
        {
            return type.GetGenericArguments();
        }

        public override object Read(XSerializer serializer, BinaryReader reader, Type type)
        {
            var method_write = this.GetType()
                .GetMethod("ReadGeneric", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(type.GetGenericArguments());
            return method_write.Invoke(null, new object[] { serializer, reader });
        }

        public override void Write(XSerializer serializer, BinaryWriter writer, object obj)
        {
            var method_write = this.GetType()
                .GetMethod("WriteGeneric", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(obj.GetType().GetGenericArguments());
            method_write.Invoke(null, new object[] { serializer, writer, obj });
        }
    }

    class XSerializeList : XSerializeGenericTypeBase
    {
        public override Type GetWorkGenericType()
        {
            return typeof(List<>);
        }

        static object ReadGeneric<T>(XSerializer serializer, BinaryReader reader)
        {
            int count = reader.ReadInt32();
            List<T> obj = new List<T>(count);
            for (int i = 0; i < count; ++i)
            {
                obj.Add((T)serializer.InternalRead(reader, typeof(T)));
            }
            return obj;
        }

        static void WriteGeneric<T>(XSerializer serializer, BinaryWriter writer, List<T> obj)
        {
            writer.Write(obj.Count);
            for (int i = 0; i < obj.Count; ++i)
            {
                serializer.InternalWrite(writer, obj[i], typeof(T));
            }
        }
    }

    class XSerializeDictionary : XSerializeGenericTypeBase
    {
        public override Type GetWorkGenericType()
        {
            return typeof(Dictionary<,>);
        }

        static object ReadGeneric<TKey, TValue>(XSerializer serializer, BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Dictionary<TKey, TValue> obj = new Dictionary<TKey, TValue>(count);
            for (int i = 0; i < count; ++i)
            {
                TKey key = (TKey)serializer.InternalRead(reader, typeof(TKey));
                TValue value = (TValue)serializer.InternalRead(reader, typeof(TValue));
                obj.Add(key, value);
            }
            return obj;
        }

        static void WriteGeneric<TKey, TValue>(XSerializer serializer, BinaryWriter writer, Dictionary<TKey, TValue> obj)
        {
            writer.Write(obj.Count);
            foreach(var item in obj)
            {
                serializer.InternalWrite(writer, item.Key, typeof(TKey));
                serializer.InternalWrite(writer, item.Value, typeof(TValue));
            }
        }
    }

    /***************************** array ****************************************/
    class XSerializeArray : XTypeSerializer
    {
        public override bool Handles(Type type)
        {
            return type.IsArray && type.GetArrayRank() <= 1;// 蛋疼
        }

        public override IEnumerable<Type> AddSubtypes(XSerializer serializer, Type type)
        {
            return new[] { type.GetElementType() };
        }

        public override object Read(XSerializer serializer, BinaryReader reader, Type type)
        {
            var method = GetReader(type.GetArrayRank())
                .MakeGenericMethod(type.GetElementType());
            return method.Invoke(null, new object[] { serializer, reader, type });
        }

        public override void Write(XSerializer serializer, BinaryWriter writer, object obj)
        {
            var type = obj.GetType();
            var method = GetWriter(type.GetArrayRank())
                .MakeGenericMethod(type.GetElementType());
            method.Invoke(null, new object[] { serializer, writer, obj });
        }

        MethodInfo GetReader(int rank)
        {
            return this.GetType().GetMethod("Read_" + rank, BindingFlags.Static | BindingFlags.NonPublic);
        }

        MethodInfo GetWriter(int rank)
        {
            return this.GetType().GetMethod("Write_" + rank, BindingFlags.Static | BindingFlags.NonPublic);
        }

        static object Read_1<T>(XSerializer serializer, BinaryReader reader, Type type)
        {
            var element_type = type.GetElementType();
            int len_1 = reader.ReadInt32();
            T[] obj = new T[len_1];
            for (int i = 0; i < len_1; ++i)
            {
                obj[i] = (T)serializer.InternalRead(reader, element_type);
            }
            return obj;
        }

        static void Write_1<T>(XSerializer serializer, BinaryWriter writer, T[] obj)
        {
            int len_1 = obj.GetLength(0);
            writer.Write(len_1);
            for (int i = 0; i < len_1; ++i)
            {
                serializer.InternalWrite(writer, obj[i], typeof(T));
            }
        }
    }

    /***************************** string & byte[] ******************************/
    class XSerializeString : XTypeSerializer
    {
        public override bool Handles(Type type)
        {
            return typeof(string) == type;
        }

        public override object Read(XSerializer serializer, BinaryReader reader, Type type)
        {
            return reader.ReadString();
        }

        public override void Write(XSerializer serializer, BinaryWriter writer, object obj)
        {
            writer.Write((string)obj);
        }
    }

    class XSerializeByteArray:XTypeSerializer
    {

        public override bool Handles(Type type)
        {
            return typeof(byte[]) == type;
        }

        public override object Read(XSerializer serializer, BinaryReader reader, Type type)
        {
            int count = reader.ReadInt32();
            return reader.ReadBytes(count);
        }

        public override void Write(XSerializer serializer, BinaryWriter writer, object obj)
        {
            byte[] data = (byte[]) obj;
            writer.Write(data.Length);
            writer.Write(data);
        }
    }

    /***************************** Primitives ***********************************/
    class XSerializeEnum : XTypeSerializer
    {

        public override bool Handles(Type type)
        {
            return type.IsEnum;
        }

        public override object Read(XSerializer serializer, BinaryReader reader, Type type)
        {
            object obj = serializer.InternalRead(reader, Enum.GetUnderlyingType(type));
            return obj;// Enum里没有好的方法转换类型，发现可以直接=
        }

        public override void Write(XSerializer serializer, BinaryWriter writer, object obj)
        {
            serializer.InternalWrite(writer, obj, Enum.GetUnderlyingType(obj.GetType()));
        }

        public override IEnumerable<Type> AddSubtypes(XSerializer serializer, Type type)
        {
            return new[] { Enum.GetUnderlyingType(type) };
        }
    }

    class XSerializeBool:XTypeSerializer
    {
        public override bool Handles(Type type)
        {
            return typeof(bool) == type;
        }

        public override object Read(XSerializer serializer, BinaryReader reader, Type type)
        {
            return reader.ReadBoolean();
        }

        public override void Write(XSerializer serializer, BinaryWriter writer, object obj)
        {
            writer.Write((bool)obj);
        }
    }

    class XSerializeInt32 : XTypeSerializer
    {
        public override bool Handles(Type type)
        {
            return typeof(int) == type;
        }

        public override object Read(XSerializer serializer, BinaryReader reader, Type type)
        {
            return reader.ReadInt32();
        }

        public override void Write(XSerializer serializer, BinaryWriter writer, object obj)
        {
            writer.Write((int)obj);
        }
    }

    class XSerializeDouble : XTypeSerializer
    {
        public override bool Handles(Type type)
        {
            return typeof(double) == type;
        }

        public override object Read(XSerializer serializer, BinaryReader reader, Type type)
        {
            return reader.ReadDouble();
        }

        public override void Write(XSerializer serializer, BinaryWriter writer, object obj)
        {
            writer.Write((double)obj);
        }
    }

}
