using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// 基元类型数组转换器
    /// </summary>
    public class JbinPrimitiveArrayConverter : JbinSerializer<Array>, IJbinFieldDeserializer
    {
        /// <inheritdoc/>
        public override bool CanSerialize(Type objectType)
        {
            if (!objectType.IsArray)
            {
                return false;
            }

            var elementType = objectType.GetElementType();

            if (!elementType.IsPrimitive)
            {
                return false;
            }

            if (elementType == typeof(byte))
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public bool CanDeserialize(Type defineType, Type realType)
        {
            if (!realType.IsArray)
            {
                return false;
            }

            var elementType = realType.GetElementType();

            if (!elementType.IsPrimitive)
            {
                return false;
            }

            if (elementType == typeof(byte))
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType)
        {
            var elementType = realType.GetElementType();

            if (elementType == typeof(byte))
            {
                return bytes;
            }
            else
            {
                var array = ConvertBytesToArray(elementType, bytes);
                return array;
            }
        }

        /// <inheritdoc/>
        public override byte[] ConvertValueToBytes(object value)
        {
            var type = value.GetType();
            return ConvertValueToBytes(type, value);
        }

        /// <inheritdoc/>
        public override byte[] ConvertValueToBytes(Type type, object value)
        {
            if (value is byte[] data)
            {
                return data;
            }
            else
            {
                var bytes = ConvertArrayToBytes((Array)value);
                return bytes;
            }
        }

        /// <summary>
        /// 将基元类型的数组转为字节数组
        /// </summary>
        /// <param name="array">基元类型的数组</param>
        private byte[] ConvertArrayToBytes(Array array)
        {
            var elemType = array.GetType().GetElementType();
            var size = GetValueTypeSize(elemType);
            int length = array.Length * size;
            var bytes = new byte[length];
            try
            {
                Buffer.BlockCopy(array, 0, bytes, 0, length);
            }
            catch (Exception ex)
            {
                var e = new InvalidOperationException($"无法序列化`{elemType.FullName}[]`. (TypeSize={size})", ex);
                throw e;
            }
            return bytes;
        }

        /// <summary>
        /// 将字节数组还原为基元类型的数组
        /// </summary>
        /// <param name="elemType">基元类型</param>
        /// <param name="bytes">字节数组</param>
        /// <returns></returns>
        private Array ConvertBytesToArray(Type elemType, byte[] bytes)
        {
            var size = GetValueTypeSize(elemType);
            int length = bytes.Length / size;
            var array = Array.CreateInstance(elemType, length);
            try
            {
                Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                var e = new InvalidOperationException($"无法反序列化`{elemType.FullName}[]`. (TypeSize={size})", ex);
                throw e;
            }
            return array;
        }

        /// <summary>
        /// 获取值类型的字节长度
        /// </summary>
        /// <param name="elemType">值类型</param>
        /// <returns></returns>
        public static int GetValueTypeSize(Type elemType)
        {
            if (elemType.IsEnum)
            {
                elemType = Enum.GetUnderlyingType(elemType);
            }

            if (!valueTypeSizeDict.TryGetValue(elemType, out byte size))
            {
                size = (byte)Marshal.SizeOf(elemType);
            }
            return size;
        }

        /// <summary>
        /// 值类型尺寸
        /// </summary>
        private static readonly Dictionary<Type, byte> valueTypeSizeDict = new Dictionary<Type, byte>
        {
            {typeof(bool),1},
            {typeof(byte),1},
            {typeof(sbyte),1},
            {typeof(char),2},
            {typeof(short),2},
            {typeof(ushort),2},
            {typeof(int),4},
            {typeof(uint),4},
            {typeof(long),8},
            {typeof(ulong),8},
            {typeof(float),4},
            {typeof(double),8},
            {typeof(decimal),16},
        };
    }
}
