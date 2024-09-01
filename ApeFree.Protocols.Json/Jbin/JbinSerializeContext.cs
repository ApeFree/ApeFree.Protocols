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

        public JbinSerializeContext(JsonSerializerSettings settings)
        {
            Settings = settings;
            DataBlocks = new List<byte[]>();
            DataTypes = new List<Type>();
        }

        public JbinSerializeContext(List<byte[]> dataBlocks, List<Type> dataTypes, JsonSerializerSettings settings)
        {
            DataBlocks = dataBlocks;
            DataTypes = dataTypes;
            Settings = settings;
        }
    }

}
