using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApeFree.Protocols.Json.Jbin
{
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

    public abstract class JbinSerializer<T> : JbinConverter, IJbinFieldSerializer
    {
        public virtual bool CanSerialize(Type objectType)
        {
            return objectType == typeof(T);
        }

        public virtual int GetSerializationMode(Type objectType) => 0;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var valueType = value.GetType();

            // 针对引用类型检查是否已有相同的对象实例已被序列化
            if (!valueType.IsValueType)
            {
                lock (Context.SerializedObjectMap)
                {
                    if (Context.SerializedObjectMap.TryGetValue(value, out long existingCombinedId))
                    {
                        writer.WriteValue(existingCombinedId);
                        return;
                    }
                }
            }

            long blockId = 0;
            int modeId = GetSerializationMode(valueType);

            var bytes = ConvertValueToBytes(valueType, value, modeId);
            lock (DataBlocks)
            {
                DataBlocks.Add(bytes);
                blockId = DataBlocks.Count;
            }

            long typeId = 0;
            lock (DataTypes)
            {
                typeId = DataTypes.IndexOf(valueType);
                if (typeId == -1)
                {
                    DataTypes.Add(valueType);
                    typeId = DataTypes.Count - 1;
                }
            }

            // 合并ModeId、TypeId和BlockId为一个long：
            // Bit 63: 1 (Magic)
            // Bits 55..62 (8 bits): modeId
            // Bits 32..54 (23 bits): typeId
            // Bit 31: 1 (Magic)
            // Bits 0..30 (31 bits): blockId
            long combinedId = ((long)(modeId & 0xFF) << 55) | (((long)typeId & 0x007FFFFF) << 32) | ((uint)blockId & 0x7FFFFFFF);
            combinedId |= (1L << 63);
            combinedId |= (1L << 31);

            // 针对引用类型记录到已序列化字典中
            if (!valueType.IsValueType)
            {
                lock (Context.SerializedObjectMap)
                {
                    Context.SerializedObjectMap[value] = combinedId;
                }
            }

            writer.WriteValue(combinedId);
        }

        public virtual byte[] ConvertValueToBytes(object value)
        {
            return ConvertValueToBytes(value?.GetType(), value, 0);
        }

        public virtual byte[] ConvertValueToBytes(Type type, object value)
        {
            return ConvertValueToBytes(type, value, 0);
        }

        public abstract byte[] ConvertValueToBytes(Type type, object value, int modeId);
    }
}
