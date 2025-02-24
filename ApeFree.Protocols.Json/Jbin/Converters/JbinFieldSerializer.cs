using System;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// Jbin字段的反序列化器接口
    /// </summary>
    public interface IJbinFieldDeserializer
    {
        /// <summary>
        /// 可以被读取并反序列化成对象
        /// </summary>
        /// <param name="defineType"></param>
        /// <param name="realType"></param>
        /// <returns></returns>
        bool CanDeserialize(Type defineType, Type realType);

        /// <summary>
        /// 字节数组转指定类型的实现
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="defineType"></param>
        /// <param name="realType"></param>
        /// <returns></returns>
        object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType);
    }

    /// <summary>
    /// Jbin字段的序列化器接口
    /// </summary>
    public interface IJbinFieldSerializer
    {
        /// <summary>
        /// 可以被序列化成字节数组
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        bool CanSerialize(Type objectType);

        /// <summary>
        /// 对象转换为字节数组的实现
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        byte[] ConvertValueToBytes(object value);
    }

    /// <summary>
    /// Jbin字段的转换器接口
    /// </summary>
    public interface IJbinFieldConverter : IJbinFieldSerializer, IJbinFieldDeserializer { }

    /// <summary>
    /// Jbin字段的转换器
    /// </summary>
    public abstract class JbinFieldConverter : IJbinFieldDeserializer, IJbinFieldSerializer
    {
        public abstract bool CanDeserialize(Type defineType, Type realType);

        public abstract bool CanSerialize(Type objectType);

        public abstract object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType);

        public abstract byte[] ConvertValueToBytes(object value);
    }
}
