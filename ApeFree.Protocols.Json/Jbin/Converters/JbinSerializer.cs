using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// Jbin 转换器基类
    /// <para>这是所有 Jbin 转换器的共同父类，继承自 Newtonsoft.Json 的 JsonConverter。</para>
    /// <para>它通过 <see cref="JbinSerializeContext"/> 为具体的子类提供了对二进制数据块 (DataBlocks) 和类型记录 (DataTypes) 的访问权限。</para>
    /// </summary>
    public abstract class JbinConverter : JsonConverter
    {
        /// <summary>
        /// 转换上下文
        /// </summary>
        protected internal JbinSerializeContext Context { get; private set; }

        /// <summary>
        /// 数据块列表
        /// </summary>
        public List<byte[]> DataBlocks => Context.DataBlocks;

        /// <summary>
        /// 数据类型列表
        /// </summary>
        public List<Type> DataTypes => Context.DataTypes;

        /// <summary>
        /// 当前转换器所属的Json序列化配置
        /// </summary>
        public JsonSerializerSettings Settings => Context.Settings;

        internal void Initialize(JbinSerializeContext context)
        {
            Context = context;
            OnInitialized();
        }

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            if (Context.SerializationMode == SerializationMode.Serialize)
            {
                return this is IJbinFieldSerializer x && x.CanSerialize(objectType);
            }

            return false;
        }

        /// <summary>
        /// 当初始化完成后（重置数据块列表）
        /// </summary>
        protected virtual void OnInitialized() { }


    }

    /// <summary>
    /// Jbin 序列化器基类 (泛型)
    /// <para>1. 本类通过重写 WriteJson 拦截了 Newtonsoft.Json 的常规对象序列化。</para>
    /// <para>2. 具体子类实现 <see cref="ConvertValueToBytes(Type, object)"/> 将对象转换为二进制数据。</para>
    /// <para>3. 本基类负责将转换后的字节存入 Context 的 DataBlocks，生成一个拼接后的 long ID (Combined ID) 写入 JSON 字符串中。</para>
    /// </summary>
    /// <typeparam name="T">要处理的目标数据类型</typeparam>
    public abstract class JbinSerializer<T> : JbinConverter, IJbinFieldSerializer
    {
        public virtual bool CanSerialize(Type objectType)
        {
            return objectType == typeof(T);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            long blockId = 0;

            var bytes = ConvertValueToBytes((T)value);
            lock (DataBlocks)
            {
                DataBlocks.Add(bytes);
                blockId = DataBlocks.Count;
            }

            long typeId = 0;
            var valueType = value.GetType();

            lock (DataTypes)
            {
                typeId = DataTypes.IndexOf(valueType);
                if (typeId == -1)
                {
                    DataTypes.Add(valueType);
                    typeId = DataTypes.Count - 1;
                }
            }

            // 合并数据类型Id和数据块Id为一个long（合并时将这两个数值的最高位设置为1）
            long combinedId = (typeId << 32) | (uint)blockId;
            combinedId |= (long)1 << 31;
            combinedId |= (long)1 << 63;

            writer.WriteValue(combinedId);
        }

        /// <summary>
        /// 简易序列化入口：将对象转为字节数组。
        /// <para>子类可根据需要重写，默认会转发给带 Type 参数的版本。</para>
        /// </summary>
        public virtual byte[] ConvertValueToBytes(object value)
        {
            return ConvertValueToBytes(value.GetType(), value);
        }

        /// <summary>
        /// 核心序列化实现：子类在此定义对象转二进制块的逻辑。
        /// </summary>
        /// <param name="type">对象的真实运行时类型。</param>
        /// <param name="value">对象实例。</param>
        /// <returns>序列化生成的字节数组。</returns>
        public abstract byte[] ConvertValueToBytes(Type type, object value);
    }
}
