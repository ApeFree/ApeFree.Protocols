#if NET8_0_OR_GREATER
using System.Collections.Generic;
#else
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#endif
using System;
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

        /// <summary>
        /// 已序列化对象引用与对应 CombinedId 字典（用于序列化阶段引用去重与 Block 复用）
        /// </summary>
        public Dictionary<object, long> SerializedObjectMap { get; }

        /// <summary>
        /// 已反序列化数据块索引与对象实例缓存字典（用于反序列化阶段实例复用）
        /// </summary>
        public Dictionary<int, object> DeserializedBlockCache { get; }

        /// <summary>
        /// 构造Jbin序列化上下文
        /// </summary>
        /// <param name="settings">Json序列化配置</param>
        /// <param name="serializationMode">序列化模式</param>
        public JbinSerializeContext(JsonSerializerSettings settings, SerializationMode serializationMode)
        {
            Settings = settings;
            SerializationMode = serializationMode;
            DataBlocks = new List<byte[]>();
            DataTypes = new List<Type>();
            SerializedObjectMap = new Dictionary<object, long>(ReferenceEqualityComparer.Instance);
            DeserializedBlockCache = new Dictionary<int, object>();
        }

        /// <summary>
        /// 构造Jbin序列化上下文
        /// </summary>
        /// <param name="dataBlocks">数据块列表</param>
        /// <param name="dataTypes">数据类型列表</param>
        /// <param name="settings">Json序列化配置</param>
        /// <param name="serializationMode">序列化模式</param>
        public JbinSerializeContext(List<byte[]> dataBlocks, List<Type> dataTypes, JsonSerializerSettings settings, SerializationMode serializationMode)
        {
            DataBlocks = dataBlocks;
            DataTypes = dataTypes;
            Settings = settings;
            SerializationMode = serializationMode;
            SerializedObjectMap = new Dictionary<object, long>(ReferenceEqualityComparer.Instance);
            DeserializedBlockCache = new Dictionary<int, object>();
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

#if !NET8_0_OR_GREATER
    /// <summary>
    /// 低版本 .NET 运行时（.NET Standard 2.0 / .NET Framework 4.5.2 等）的引用相等比较器
    /// </summary>
    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>, IEqualityComparer
    {
        public static ReferenceEqualityComparer Instance { get; } = new ReferenceEqualityComparer();

        private ReferenceEqualityComparer() { }

        public new bool Equals(object x, object y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
    }
#endif
}
