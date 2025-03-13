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

        ///// <summary>
        ///// 将数据转换为字节数组
        ///// </summary>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //internal abstract byte[] ConvertObjectToBytes(object value);

        ///// <summary>
        ///// 将字节数组转换为数据
        ///// </summary>
        ///// <param name="bytes"></param>
        ///// <returns></returns>
        //internal abstract object ConvertBytesToObject(byte[] bytes, Type defineType, Type realType);
    }

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

        public abstract byte[] ConvertValueToBytes(object value);
        public abstract byte[] ConvertValueToBytes(Type type, object value);
    }
}
