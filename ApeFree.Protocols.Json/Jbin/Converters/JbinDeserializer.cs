using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApeFree.Protocols.Json.Jbin
{
    public class JbinDeserializer : JbinConverter
    {
        private static readonly Type LongType = typeof(long);

        public List<IJbinFieldDeserializer> Serializers { get; set; }

        private bool tag = false;

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            if (Context.SerializationMode == SerializationMode.Serialize)
            {
                return false;
            }

            if (Context.DataTypes.Contains(objectType))
            {
                return true;
            }

            if (tag)
            {
                tag = false;
                return false;
            }

            // 只要字段类型的申明不是长整型的数值在反序列化的时候都要拦截
            return objectType != LongType;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (Context.SerializationMode == SerializationMode.Serialize)
            {
                return;
            }

            Serializers = Context.Settings.Converters.Where(x => x != this && x is IJbinFieldDeserializer).Cast<IJbinFieldDeserializer>().ToList();
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type defineType, object existingValue, JsonSerializer serializer)
        {
            // 如果字段的定义是长整型，则直接返回数值
            if (defineType == typeof(long) && reader.Value is long num)
            {
                return num;
            }

            // 判断long类型的数值是否符合拼接数的特征
            if (reader.Value is long id && ((id >> 63) & 1) != 0 && ((id >> 31) & 1) != 0)
            {
                // 尝试拆出TypeId和BlockId
                int typeId = (int)((id >> 32) & 0x7FFFFFFF);    // 提取高 32 位并清除最高位标志
                int blockId = (int)(id & 0x7FFFFFFF);           // 提取低 32 位并清除最高位标志

                // 检查ID是否有效
                if (typeId < DataTypes.Count && blockId < DataBlocks.Count)
                {
                    var realType = DataTypes[typeId];
                    byte[] bytes = DataBlocks[blockId];

                    // 寻找匹配的序列化器
                    // 这里可以使用缓存优化查找速度
                    var js = Serializers.FirstOrDefault(x => x.CanDeserialize(defineType, realType));

                    if (js != null)
                    {
                        // 将数据块还原
                        var value = js.ConvertBytesToValue(bytes, defineType, realType);
                        return value;
                    }
                    else
                    {
                        // 无对应的序列化器还原
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                tag = true;
            }

            if (reader.TokenType == JsonToken.StartObject)
            {
                // 读取到的JSON对象
                JObject jsonObject = JObject.Load(reader);

                // 加载类型
                Type targetType;

                // 获取类型信息
                string typeInfo = jsonObject["$type"]?.ToString();
                if (typeInfo == null)
                {
                    targetType = defineType;
                }
                else
                {
                    targetType = TypeExtensions.GetType(typeInfo);

                    if (targetType == null)
                    {
                        throw new JsonSerializationException($"类型 {typeInfo} 未找到。");
                    }
                }

                // 移除$type令牌，避免反序列化时再次处理
                _ = jsonObject.Remove("$type");

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
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { }
    }
}
