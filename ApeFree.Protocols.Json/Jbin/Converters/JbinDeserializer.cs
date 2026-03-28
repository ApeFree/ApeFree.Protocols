using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STTech.CodePlus.Utils;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// Jbin 中央反序列化器 (反序列化路由器)
    /// <para>核心逻辑：</para>
    /// <para>1. 本类不直接处理具体的二进制字节还原，而是作为“路由器”拦截 Newtonsoft.Json 的反序列化过程。</para>
    /// <para>2. 它在 ReadJson 中检查读到的 long 值是否符合拼接 ID 特征。</para>
    /// <para>3. 如果是拼接 ID，它会根据存储在其中的 TypeId 和 BlockId 找到真实数据和类型，并委派给 <see cref="FieldDeserializers"/> 中的具体执行者进行还原。</para>
    /// <para>4. 如果不是拼接 ID，则利用 <see cref="_bypassNextCanConvert"/> 让 Newtonsoft.Json 回到标准的反序列化流程。</para>
    /// </summary>
    public class JbinDeserializer : JbinConverter
    {
        private static readonly Type LongType = typeof(long);
        private static readonly Type FloatType = typeof(float);

        // 所有在Json会被存成Long的整数值类型
        private static readonly Type[] numericTypes = new[]
        {
            typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong)
        };

        /// <summary>
        /// 字段反序列化器列表
        /// </summary>
        public List<IJbinFieldDeserializer> FieldDeserializers { get; set; }

        /// <summary>
        /// 标记是否跳过下一次 CanConvert 检查。
        /// 用于在反序列化普通对象时剥离 JbinDeserializer，以避免引发无限递归。
        /// </summary>
        private bool _bypassNextCanConvert = false;

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

            // 如果标记了跳过，则放行下一次反序列化委托给 Newtonsoft.Json 默认处理
            if (_bypassNextCanConvert)
            {
                _bypassNextCanConvert = false;
                return false;
            }

            // 只要字段类型的申明不是长整型的数值在反序列化的时候都要拦截
            return objectType != LongType;
        }

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            // 如果当前处于序列化模式，此路由器不需要工作。
            if (Context.SerializationMode == SerializationMode.Serialize)
            {
                return;
            }

            // 在初始化时，从全局配置中提取出所有实现了 IJbinFieldDeserializer 接口的具体转换器。
            // 当 ReadJson 识别到拼接 ID 时，会从这个列表中寻找匹配的处理者。
            FieldDeserializers = Context.Settings.Converters.Where(x => x != this && x is IJbinFieldDeserializer).Cast<IJbinFieldDeserializer>().ToList();
        }

        /// <summary>
        /// 核心读取逻辑：Jbin 的“反序列化调度中心”。
        /// </summary>
        public override object ReadJson(JsonReader reader, Type defineType, object existingValue, JsonSerializer serializer)
        {
            if (defineType.IsValueType)
            {
                // 如果字段的定义是枚举类型，则直接转换并返回枚举值
                if (defineType.IsEnum)
                {
                    var value = Enum.ToObject(defineType, reader.Value);
                    return value;
                }
                // 如果字段的定义是整数的数值类型，并且改节点的值类型是Long，则直接强转并返回数值
                else if (reader.Value is long numLong)
                {
                    if (numericTypes.Contains(defineType))
                    {
                        return Convert.ChangeType(numLong, defineType);
                    }
                    else
                    {
                        // 此时的numLong应该是一个拼接数，指向一个数据块
                        // 这里不做处理
                    }
                }
                // 单精度浮点数和双精度浮点数在解析时会被转换成double类型，所以需要进行判断
                else if (reader.Value is double numDouble)
                {
                    if (defineType == FloatType)
                    {
                        return Convert.ChangeType(numDouble, FloatType);
                    }
                    else
                    {
                        return numDouble;
                    }
                }
                // 其他的值类型（boolean、char）
                else if (reader.Value.GetType() == defineType)
                {
                    return reader.Value;
                }

                else
                {
                    // 其他情况
                }
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
                    // 引用合并：检查 BlockId 是否已还原过
                    if (Context.TryGetCachedObject(blockId, out object cached))
                    {
                        return cached;
                    }

                    var realType = DataTypes[typeId];
                    var bytes = DataBlocks[blockId];

                    // 寻找匹配的序列化器
                    var js = FieldDeserializers.FirstOrDefault(x => x.CanDeserialize(defineType, realType));

                    if (js != null)
                    {
                        // 将数据块还原
                        var value = js.ConvertBytesToValue(bytes, defineType, realType);

                        // 引用合并：缓存已还原的对象
                        Context.CacheObject(blockId, value);

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
                // 如果发现当前节点并非拼接 ID（例如是一个由 StartObject 引导的普通对象）
                // 标记在此之后 Newtonsoft.Json 的递归调用跳过 JbinDeserializer 检查
                // 确保本层对象的反序列化能回归标准路径
                _bypassNextCanConvert = true;
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
                    targetType = TypeParser.GetType(typeInfo);

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
