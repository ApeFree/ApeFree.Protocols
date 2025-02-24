using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// Jbin序列化上下文
    /// </summary>
    public class JbinSerializeContext
    {
        /// <summary>
        /// 数据块列表
        /// </summary>
        public List<byte[]> DataBlocks { get; }

        /// <summary>
        /// 数据类型列表
        /// </summary>
        public List<Type> DataTypes { get; }

        /// <summary>
        /// 当前转换器所属的Json序列化配置
        /// </summary>
        public JsonSerializerSettings Settings { get; }

        /// <summary>
        /// 序列化模式
        /// </summary>
        public SerializationMode SerializationMode { get; }

        public JbinSerializeContext(JsonSerializerSettings settings, SerializationMode serializationMode)
        {
            Settings = settings;
            SerializationMode = serializationMode;
            DataBlocks = new List<byte[]>();
            DataTypes = new List<Type>();
        }

        public JbinSerializeContext(List<byte[]> dataBlocks, List<Type> dataTypes, JsonSerializerSettings settings, SerializationMode serializationMode)
        {
            DataBlocks = dataBlocks;
            DataTypes = dataTypes;
            Settings = settings;
            SerializationMode = serializationMode;
        }
    }

    /// <summary>
    /// 序列化模式
    /// </summary>
    public enum SerializationMode
    {
        /// <summary>
        /// 序列化
        /// </summary>
        Serialize,

        /// <summary>
        /// 反序列化
        /// </summary>
        Deserialize
    }

}
