using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// Jbin数据头部
    /// </summary>
    public class JbinHeader
    {
        // 默认的Json序列化配置
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.None,
        };

        /// <summary>
        /// 版本
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 数据结构描述
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 数据类型列表
        /// </summary>
        public Type[] Types { get; set; }

        /// <summary>
        /// 标签字典
        /// </summary>
        public Dictionary<string, object> Tags { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, serializerSettings);
        }
    }
}
