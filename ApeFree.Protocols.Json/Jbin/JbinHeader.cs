using Newtonsoft.Json;
using System;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// Jbin数据头部
    /// </summary>
    public class JbinHeader
    {
        /// <summary>
        /// 结构描述文本
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 数据类型列表
        /// </summary>
        public Type[] Types { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this,Formatting.None);
        }
    }
}
