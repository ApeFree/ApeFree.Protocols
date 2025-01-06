using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// 基元类型数组转换器
    /// </summary>
    public class JbinPrimitiveArrayConverter : JbinConverter<Array>
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
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
        protected override Array ConvertBytesToValue(byte[] bytes, Type objType)
        {
            var elementType = objType.GetElementType();

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
        protected override byte[] ConvertValueToBytes(Array value)
        {
            if (value is byte[] data)
            {
                return data;
            }
            else
            {
                var bytes = ConvertArrayToBytes(value);
                return bytes;
            }
        }

        /// <summary>
        /// 将基元类型的数组转为字节数组
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private byte[] ConvertArrayToBytes(Array array)
        {
            var elemType = array.GetType().GetElementType();
            int size = Marshal.SizeOf(elemType);
            int length = array.Length * size;
            var bytes = new byte[length];
            Buffer.BlockCopy(array, 0, bytes, 0, length);
            return bytes;
        }

        /// <summary>
        /// 将字节数组还原为基元类型的数组
        /// </summary>
        /// <param name="elemType"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private Array ConvertBytesToArray(Type elemType, byte[] bytes)
        {
            int size = Marshal.SizeOf(elemType);
            int length = bytes.Length / size;
            var array = Array.CreateInstance(elemType, length);
            Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
            return array;
        }
    }
}
