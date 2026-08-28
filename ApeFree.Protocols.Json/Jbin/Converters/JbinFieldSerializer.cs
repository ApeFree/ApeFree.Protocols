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
        /// <param name="defineType">字段声明类型</param>
        /// <param name="realType">真实数据类型</param>
        /// <returns></returns>
        bool CanDeserialize(Type defineType, Type realType);

        /// <summary>
        /// 字节数组转指定类型的实现（默认模式）
        /// </summary>
        /// <param name="bytes">源字节数组</param>
        /// <param name="defineType">字段声明类型</param>
        /// <param name="realType">真实数据类型</param>
        /// <returns></returns>
        object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType);

        /// <summary>
        /// 字节数组根据指定模式ID转指定类型的实现
        /// </summary>
        /// <param name="bytes">源字节数组</param>
        /// <param name="defineType">字段声明类型</param>
        /// <param name="realType">真实数据类型</param>
        /// <param name="modeId">模式ID (从CombineId解析)</param>
        /// <returns></returns>
        object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType, int modeId);
    }

    /// <summary>
    /// Jbin字段的序列化器接口
    /// </summary>
    public interface IJbinFieldSerializer
    {
        /// <summary>
        /// 可以被序列化成字节数组
        /// </summary>
        /// <param name="objectType">对象类型</param>
        /// <returns></returns>
        bool CanSerialize(Type objectType);

        /// <summary>
        /// 获取指定类型的序列化模式ID
        /// </summary>
        /// <param name="objectType">对象类型</param>
        /// <returns>模式ID (0 为默认)</returns>
        int GetSerializationMode(Type objectType);

        /// <summary>
        /// 对象转换为字节数组的实现
        /// </summary>
        /// <param name="value">待序列化对象</param>
        /// <returns></returns>
        byte[] ConvertValueToBytes(object value);

        /// <summary>
        /// 对象转换为字节数组的实现
        /// </summary>
        /// <param name="type">指定待序列化对象的类型</param>
        /// <param name="value">待序列化对象</param>
        /// <returns></returns>
        byte[] ConvertValueToBytes(Type type, object value);

        /// <summary>
        /// 对象根据指定模式ID转换为字节数组的实现
        /// </summary>
        /// <param name="type">指定待序列化对象的类型</param>
        /// <param name="value">待序列化对象</param>
        /// <param name="modeId">模式ID</param>
        /// <returns></returns>
        byte[] ConvertValueToBytes(Type type, object value, int modeId);
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

        public virtual int GetSerializationMode(Type objectType) => 0;

        public virtual object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType)
        {
            return ConvertBytesToValue(bytes, defineType, realType, 0);
        }

        public abstract object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType, int modeId);

        public virtual byte[] ConvertValueToBytes(object value)
        {
            return ConvertValueToBytes(value?.GetType(), value, 0);
        }

        public virtual byte[] ConvertValueToBytes(Type type, object value)
        {
            return ConvertValueToBytes(type, value, 0);
        }

        public abstract byte[] ConvertValueToBytes(Type type, object value, int modeId);
    }
}
