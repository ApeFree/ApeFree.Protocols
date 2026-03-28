using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        /// 对象引用合并策略
        /// </summary>
        public JbinReferenceMergingStrategy ReferenceMergingStrategy { get; }

        /// <summary>
        /// 序列化时：对象引用 → BlockId 的映射表（按引用相等性比较）
        /// </summary>
        private readonly Dictionary<object, int> _referenceToBlockId;

        /// <summary>
        /// 反序列化时：BlockId → 已还原对象的缓存
        /// </summary>
        private readonly Dictionary<int, object> _blockIdToObject;

        public JbinSerializeContext(JsonSerializerSettings settings, SerializationMode serializationMode)
            : this(settings, serializationMode, JbinReferenceMergingStrategy.SharedBlock)
        {
        }

        public JbinSerializeContext(JsonSerializerSettings settings, SerializationMode serializationMode, JbinReferenceMergingStrategy referenceMergingStrategy)
        {
            Settings = settings;
            SerializationMode = serializationMode;
            DataBlocks = new List<byte[]>();
            DataTypes = new List<Type>();
            ReferenceMergingStrategy = referenceMergingStrategy;
            _referenceToBlockId = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
            _blockIdToObject = new Dictionary<int, object>();
        }

        public JbinSerializeContext(List<byte[]> dataBlocks, List<Type> dataTypes, JsonSerializerSettings settings, SerializationMode serializationMode)
            : this(dataBlocks, dataTypes, settings, serializationMode, JbinReferenceMergingStrategy.SharedBlock)
        {
        }

        public JbinSerializeContext(List<byte[]> dataBlocks, List<Type> dataTypes, JsonSerializerSettings settings, SerializationMode serializationMode, JbinReferenceMergingStrategy referenceMergingStrategy)
        {
            DataBlocks = dataBlocks;
            DataTypes = dataTypes;
            Settings = settings;
            SerializationMode = serializationMode;
            ReferenceMergingStrategy = referenceMergingStrategy;
            _referenceToBlockId = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
            _blockIdToObject = new Dictionary<int, object>();
        }

        #region 引用合并 API

        /// <summary>
        /// 序列化时：查找对象是否已有 BlockId（仅对引用类型生效）
        /// </summary>
        public bool TryGetBlockId(object obj, out int blockId)
        {
            if (ReferenceMergingStrategy == JbinReferenceMergingStrategy.SharedBlock
                && obj != null
                && !obj.GetType().IsValueType)
            {
                return _referenceToBlockId.TryGetValue(obj, out blockId);
            }
            blockId = -1;
            return false;
        }

        /// <summary>
        /// 序列化时：注册对象与 BlockId 的映射
        /// </summary>
        public void RegisterReference(object obj, int blockId)
        {
            if (ReferenceMergingStrategy == JbinReferenceMergingStrategy.SharedBlock
                && obj != null
                && !obj.GetType().IsValueType)
            {
                _referenceToBlockId[obj] = blockId;
            }
        }

        /// <summary>
        /// 反序列化时：查找 BlockId 是否已还原过
        /// </summary>
        public bool TryGetCachedObject(int blockId, out object obj)
        {
            if (ReferenceMergingStrategy == JbinReferenceMergingStrategy.SharedBlock)
            {
                return _blockIdToObject.TryGetValue(blockId, out obj);
            }
            obj = null;
            return false;
        }

        /// <summary>
        /// 反序列化时：缓存已还原的对象
        /// </summary>
        public void CacheObject(int blockId, object obj)
        {
            if (ReferenceMergingStrategy == JbinReferenceMergingStrategy.SharedBlock)
            {
                _blockIdToObject[blockId] = obj;
            }
        }

        #endregion
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

    /// <summary>
    /// 按引用相等性比较对象的比较器。
    /// <para>用于确保引用合并只对同一个对象实例生效，而非值相等的不同实例。</para>
    /// </summary>
    internal class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

        public new bool Equals(object x, object y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
