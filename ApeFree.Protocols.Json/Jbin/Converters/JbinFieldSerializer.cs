using System;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// Jbin 字段反序列化器接口
    /// <para>当你需要从 Jbin 二进制数据块中恢复对象时，需要实现此接口。</para>
    /// <para>它负责识别数据块类型并将其还原为 .NET 对象。</para>
    /// </summary>
    public interface IJbinFieldDeserializer
    {
        /// <summary>
        /// 判断当前反序列化器是否能处理指定的类型。
        /// </summary>
        /// <param name="defineType">字段在源代码中定义的静态类型（可能是接口或基类）。</param>
        /// <param name="realType">序列化时记录在 Jbin 头部中的实际运行时类型。</param>
        /// <returns>如果能处理则返回 true，否则返回 false。</returns>
        bool CanDeserialize(Type defineType, Type realType);

        /// <summary>
        /// 将原始字节还原为对象实例。
        /// </summary>
        /// <param name="bytes">从 Jbin 数据包中提取出来的原始二进制数据块。</param>
        /// <param name="defineType">申明类型。</param>
        /// <param name="realType">实际类型。</param>
        /// <returns>还原后的对象实例。</returns>
        object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType);
    }

    /// <summary>
    /// Jbin 字段序列化器接口
    /// <para>当你希望将某些高开销的数据（如大型数组、位图）从 JSON 字符串中“剥离”出来，以原始二进制形式存储时，请实现此接口。</para>
    /// </summary>
    public interface IJbinFieldSerializer
    {
        /// <summary>
        /// 判断当前序列化器是否能处理指定的对象类型。
        /// </summary>
        /// <param name="objectType">准备进行序列化的对象类型。</param>
        /// <returns>如果该类型应该被存入二进制块，则返回 true。</returns>
        bool CanSerialize(Type objectType);

        /// <summary>
        /// 将对象转换为字节数组（简易调用接口）。
        /// </summary>
        /// <param name="value">要序列化的对象实例。</param>
        /// <returns>转换后的二进制数据，将被存入 Jbin 独立数据块。</returns>
        byte[] ConvertValueToBytes(object value);

        /// <summary>
        /// 将指定类型的对象转换为字节数组（完整调用接口）。
        /// </summary>
        /// <param name="type">指定的辅助类型信息。</param>
        /// <param name="value">对象实例。</param>
        /// <returns>转换后的二进制数据。</returns>
        byte[] ConvertValueToBytes(Type type, object value);
    }

    /// <summary>
    /// Jbin 字段双向转换器接口
    /// <para>这是开发自定义转换器时最常用的接口，它集成了序列化和反序列化双向功能。</para>
    /// </summary>
    public interface IJbinFieldConverter : IJbinFieldSerializer, IJbinFieldDeserializer { }


}
