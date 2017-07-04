using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

/*
 * 二进制序列化工具
 * [typename,typename,...]
 * [typeid,typeid,...]
 * complex[(*propety_name,typeid),...]
 * contain typeid
 * 
 * 使用限制：
 * 1. 自定义类一定要有默认构造函数，因为使用
 * 
 * 
 * 记录的知识点：
 * 1. Type.IsSerializable 类型是否具有`[Serializable]`特性。（这里不做检查）
 * 2. FieldInfo.IsNotSerialized 字段是否具有`[NotSerialized]`特性。【注意！！这种字段反序列号时不初始化的，没有调用构造函数】
 * 3. 引用类型判断, !Type.IsValueType【注意IsByRef是用来判断函数参数的】
 *      > https://stackoverflow.com/questions/16578609/why-does-type-isbyref-for-type-string-return-false-if-string-is-a-reference-type/16578825
 * 
 * > https://github.com/tomba/netserializer
 */
namespace SimpleScript.Serialize
{
    public class XSerializer
    {
        XTypeSerializer[] _typeSerializers = new XTypeSerializer[] {
            new XserializeObject(),

            new XSerializeBool(),
            new XSerializeEnum(),
            new XSerializeInt32(),
            new XSerializeDouble(),
            
            new XSerializeString(),
            new XSerializeByteArray(),

            new XSerializeArray(),

            new XSerializeDictionary(),
            new XSerializeList(),

            new XSerializeClass(),
		};

        Type[] _base_types = new Type[] { typeof(object), typeof(bool), typeof(double), typeof(string),};

        // For type save flag
        class TypeFlag
        {
            public static byte NUll = 255;
            public static byte REUSE = 254;
            public static byte ID32 = 253;// 后面一个int32是typeid
            //[0,252] short typeid
        }

        List<Type> _type_list = new List<Type>();
        Dictionary<Type, int> _type_id_map = new Dictionary<Type, int>();
        Dictionary<Type, XTypeSerializer> _type_handle_map = new Dictionary<Type, XTypeSerializer>();
        int _type_list_hash = 0;

        public XSerializer(params Type[] types)
        {
            ResetTypes(types);
        }

        public void ResetTypes(params Type[] types)
        {
            _type_handle_map.Clear();
            _type_id_map.Clear();
            _type_list.Clear();

            _AddTypesWithoutHash(_base_types);
            _AddTypesWithoutHash(types);

            OrderTypeAndCalculateHash();
        }

        public void AddTypes(params Type[] types)
        {
            _AddTypesWithoutHash(types);

            OrderTypeAndCalculateHash();
        }

        void _AddTypesWithoutHash(Type[] types)
        {
            var stack = new Stack<Type>(types);
            while(stack.Count > 0)
            {
                var type = stack.Pop();

                if (_type_handle_map.ContainsKey(type))
                    continue;

                //if (type.IsAbstract || type.IsInterface)
                //    throw new Exception(String.Format("Type {0} can not be serialized", type.FullName));

                if (type.ContainsGenericParameters)
                    throw new Exception(String.Format("Type {0} contains generic parameters", type.FullName));

                XTypeSerializer serializer = GetTypeSerializer(type);

                _type_handle_map[type] = serializer;
                _type_list.Add(type);

                foreach(var t in serializer.AddSubtypes(this, type))
                {
                    if (_type_handle_map.ContainsKey(t) == false)
                        stack.Push(t);
                }
            }
        }

        void OrderTypeAndCalculateHash()
        {
            var tmp = _type_list.OrderBy(t => t.ToString());
            _type_list = new List<Type>();
            _type_id_map.Clear();
            int type_id = 0;
            foreach (var type in tmp)
            {
                _type_list.Add(type);
                _type_id_map[type] = type_id++;
            }

            // todo calculate hash
            _type_list_hash = _type_list.Count;
        }

        public void Serialize(Stream stream, object obj)
        {
            _writed_obj_ids.Clear();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(_type_list_hash);
            _Write(writer, obj);
            _writed_obj_ids.Clear();
        }

        public T Deserialize<T>(Stream stream)
        {
            return (T)Deserialize(stream);
        }

        public object Deserialize(Stream stream)
        {
            _readed_objs.Clear();
            BinaryReader reader = new BinaryReader(stream);
            if(_type_list_hash != reader.ReadInt32())
            {
                throw new Exception("type list hash is error, check ResetTypes And AddTypes");
            }
            var obj = _Read(reader);
            _readed_objs.Clear();
            return obj;
        }

        // For class reuse, object born num
        Dictionary<object, int> _writed_obj_ids = new Dictionary<object, int>();// 【不需要传入比较器，这儿会使用Object.Equals来比较，相当于 == 】
        List<object> _readed_objs = new List<object>();

        internal void InternalWrite(BinaryWriter writer, object obj, Type type)
        {
            if(type.IsValueType)
            {
                _type_handle_map[type].Write(this, writer, obj);// value type optmize, don't need type flag
            }
            else
            {
                _Write(writer, obj);
            }
        }

        void _Write(BinaryWriter writer, object obj)
        {
            if (obj == null)
            {
                writer.Write(TypeFlag.NUll);// null
                return;
            }
            else if (_writed_obj_ids.ContainsKey(obj))
            {
                writer.Write(TypeFlag.REUSE);// reuse
                writer.Write(_writed_obj_ids[obj]);
            }
            else
            {
                // new one
                var type = obj.GetType();
                int type_id = 0;
                if (!_type_id_map.TryGetValue(type, out type_id))
                {
                    throw new Exception(String.Format("Unkown type {0}", type));
                }
                if (type_id >= TypeFlag.ID32)
                {
                    writer.Write(TypeFlag.ID32);
                    writer.Write(type_id);
                }
                else
                {
                    writer.Write((byte)type_id);
                }
             
                _type_handle_map[type].Write(this, writer, obj);
                _writed_obj_ids.Add(obj, _writed_obj_ids.Count);// 放在后面，和read对应
            }
        }

        internal object InternalRead(BinaryReader reader, Type type)
        {
            if (type.IsValueType)
            {
                return _type_handle_map[type].Read(this, reader, type);// value type optmize, don't need type flag
            }
            else
            {
                return _Read(reader);
            }
        }

        object _Read(BinaryReader reader)
        {
            byte flag = reader.ReadByte();
            if(flag == TypeFlag.NUll)
            {
                return null;
            }
            else if (flag == TypeFlag.REUSE)
            {
                int obj_id = reader.ReadInt32();
                return _readed_objs[obj_id];// 输入数据不对，会发生越界错误
            }
            else
            {
                int type_id = 0;
                if (flag == TypeFlag.ID32)
                {
                    type_id = reader.ReadInt32();
                }
                else
                {
                    type_id = flag;
                }
                // 取得实际类型
                var type = _type_list[type_id];// 输入数据不对，会发生越界错误
                var obj = _type_handle_map[type].Read(this, reader, type);// 输入数据不对，会发生读dic错误
                _readed_objs.Add(obj);
                return obj;
            }
        }

        XTypeSerializer GetTypeSerializer(Type type)
        {
            var serializer = _typeSerializers.FirstOrDefault(h => h.Handles(type));

            if (serializer == null)
                throw new NotSupportedException(String.Format("No serializer for {0}", type.FullName));

            return serializer;
        }
    }

    /***************************************************************/

    abstract class XTypeSerializer
    {
        /// <summary>
        /// Returns if this TypeSerializer handles the given type
        /// </summary>
        public abstract bool Handles(Type type);

        /// <summary>
        /// Return types that are needed to serialize the given type
        /// </summary>
        public virtual IEnumerable<Type> AddSubtypes(XSerializer serializer, Type type)
        {
            yield break;
            //return new Type[0];
        }

        public abstract object Read(XSerializer serializer, BinaryReader reader, Type type);
        public abstract void Write(XSerializer serializer, BinaryWriter writer, object obj);
    }

    /***************************************************************/
    public class XHelper
    {
        public static IEnumerable<FieldInfo> GetFieldInfos(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(fi => (fi.Attributes & FieldAttributes.NotSerialized) == 0)
                .OrderBy(f => f.Name, StringComparer.Ordinal);

            if (type.BaseType == null)
            {
                return fields;
            }
            else
            {
                var baseFields = GetFieldInfos(type.BaseType);
                return baseFields.Concat(fields);
            }
        }
    }
}
