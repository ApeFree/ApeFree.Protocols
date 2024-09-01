using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        /// <summary>
        /// 当初始化完成后（重置数据块列表）
        /// </summary>
        protected virtual void OnInitialized() { }

        /// <summary>
        /// 将数据转换为字节数组
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal abstract byte[] ConvertObjectToBytes(object value);

        /// <summary>
        /// 将字节数组转换为数据
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        internal abstract object ConvertBytesToObject(byte[] bytes, Type objectType);
    }

    public abstract class JbinConverter<T> : JbinConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(T);
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // 判断long类型的数值是否符合拼接数的特征
            if (reader.Value is long id && ((id >> 63) & 1) != 0 && ((id >> 31) & 1) != 0)
            {
                // 尝试拆出TypeId和BlockId
                int typeId = (int)((id >> 32) & 0x7FFFFFFF);    // 提取高 32 位并清除最高位标志
                int blockId = (int)(id & 0x7FFFFFFF);           // 提取低 32 位并清除最高位标志

                // 检查ID是否有效
                if (typeId < DataTypes.Count && blockId < DataBlocks.Count)
                {
                    objectType = DataTypes[typeId];
                    byte[] bytes = DataBlocks[blockId];

                    // 将数据块还原
                    var value = ConvertBytesToValue(bytes, objectType);

                    return value;
                }
            }

            // 避免Long值冲突，冲突的Long值需要使用string转义
            //if (reader.Value is string str && str.StartsWith("$L="))
            //{
            //    if (long.TryParse(str.Substring(3), out long value))
            //    {
            //        return value;
            //    }
            //}

            if (reader.TokenType == JsonToken.StartObject)
            {
                // 读取到的JSON对象
                JObject jsonObject = JObject.Load(reader);

                // 获取类型信息
                string typeInfo = jsonObject["$type"]?.ToString();
                if (typeInfo == null)
                {
                    throw new JsonSerializationException("缺少类型信息。");
                }

                // 加载类型
                Type targetType = Type.GetType(typeInfo);
                if (targetType == null)
                {
                    throw new JsonSerializationException($"类型 {typeInfo} 未找到。");
                }

                // 移除$type令牌，避免反序列化时再次处理
                jsonObject.Remove("$type");

                // 使用正确的类型反序列化对象
                var obj = jsonObject.ToObject(targetType, serializer);
                return obj;
            }
            else
            {
                return reader.Value;
            }
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //// 判断long类型的数值是否符合拼接数的特征
            //if (value is long id && ((id >> 63) & 1) != 0 && ((id >> 31) & 1) != 0)
            //{
            //    writer.WriteValue($"$L={id}");
            //    return;
            //}

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
        /// 将数据转换为字节数组
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected abstract byte[] ConvertValueToBytes(T value);

        /// <summary>
        /// 将字节数组转换为数据
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        protected abstract T ConvertBytesToValue(byte[] bytes, Type objectType);

        /// <inheritdoc/>
        internal override object ConvertBytesToObject(byte[] bytes, Type objectType)
        {
            return ConvertBytesToValue(bytes, objectType);
        }

        /// <inheritdoc/>
        internal override byte[] ConvertObjectToBytes(object value)
        {
            return ConvertValueToBytes((T)value);
        }
    }

}
