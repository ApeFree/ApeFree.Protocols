using Newtonsoft.Json;
using System.Collections.Generic;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// Jbin 引用合并策略
    /// <para>定义了当序列化时遇到同一个对象实例（引用相等）多次被引用时的处理方式。</para>
    /// </summary>
    public enum JbinReferenceMergingStrategy
    {
        /// <summary>
        /// 独立存储策略：每次引用该对象都会独立进行序列化并生成新的数据块（DataBlock）。
        /// <para>反序列化后，每个属性将获得一个独立的对象实例，彼此引用不相等。</para>
        /// </summary>
        Independent,

        /// <summary>
        /// 数据块共享策略：相同引用的对象只序列化一次，所有引用该对象的位置共享同一个 DataBlock (BlockId)。
        /// <para>反序列化后，所有引用该数据块的属性都将指向同一个还原后的对象实例，保持内存引用一致性。</para>
        /// </summary>
        SharedBlock,
    }

    /// <summary>
    /// Jbin 序列化配置
    /// <para>继承自 <see cref="JsonSerializerSettings"/>，提供 Jbin 协议特有的序列化行为控制。</para>
    /// </summary>
    public class JbinSerializerSettings : JsonSerializerSettings
    {
        /// <summary>
        /// 对象引用合并策略。默认为 <see cref="JbinReferenceMergingStrategy.SharedBlock"/>。
        /// <para>如果设置为 <see cref="JbinReferenceMergingStrategy.SharedBlock"/>，则 <see cref="object.ReferenceEquals"/> 相等多个属性将共享同一个二进制数据块。</para>
        /// </summary>
        public JbinReferenceMergingStrategy ReferenceMergingStrategy { get; set; } = JbinReferenceMergingStrategy.SharedBlock;
    }
}
