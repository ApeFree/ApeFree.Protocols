using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// 基元类型数组转换器（支持多模式与时序/基元压缩算法）
    /// </summary>
    public class JbinPrimitiveArrayConverter : JbinSerializer<Array>, IJbinFieldDeserializer
    {
        /// <summary>
        /// 32位整数数组序列化模式
        /// </summary>
        public Int32ArrayCompressMode Int32Mode { get; set; } = Int32ArrayCompressMode.Raw;

        /// <summary>
        /// 单精度浮点数组序列化模式
        /// </summary>
        public SingleArrayCompressMode SingleMode { get; set; } = SingleArrayCompressMode.Raw;

        /// <summary>
        /// 双精度浮点数组序列化模式
        /// </summary>
        public DoubleArrayCompressMode DoubleMode { get; set; } = DoubleArrayCompressMode.Raw;

        /// <summary>
        /// 64位整数数组序列化模式
        /// </summary>
        public Int64ArrayCompressMode Int64Mode { get; set; } = Int64ArrayCompressMode.Raw;

        /// <summary>
        /// 16位整数数组序列化模式
        /// </summary>
        public Int16ArrayCompressMode Int16Mode { get; set; } = Int16ArrayCompressMode.Raw;

        /// <inheritdoc/>
        public override bool CanSerialize(Type objectType)
        {
            if (!objectType.IsArray)
            {
                return false;
            }

            var elementType = objectType.GetElementType();
            return IsSupportedElementType(elementType);
        }

        /// <inheritdoc/>
        public bool CanDeserialize(Type defineType, Type realType)
        {
            if (!realType.IsArray)
            {
                return false;
            }

            var elementType = realType.GetElementType();
            return IsSupportedElementType(elementType);
        }

        private static bool IsSupportedElementType(Type elementType)
        {
            if (elementType == null || elementType == typeof(byte))
            {
                return false;
            }

            return elementType.IsPrimitive || elementType == typeof(decimal) || elementType.IsEnum;
        }

        /// <inheritdoc/>
        public override int GetSerializationMode(Type objectType)
        {
            if (!objectType.IsArray) return 0;
            var elemType = objectType.GetElementType();

            if (elemType == typeof(int)) return (int)Int32Mode;
            if (elemType == typeof(float)) return (int)SingleMode;
            if (elemType == typeof(double)) return (int)DoubleMode;
            if (elemType == typeof(long)) return (int)Int64Mode;
            if (elemType == typeof(short)) return (int)Int16Mode;

            return 0;
        }

        /// <inheritdoc/>
        public object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType)
        {
            return ConvertBytesToValue(bytes, defineType, realType, 0);
        }

        /// <inheritdoc/>
        public object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType, int modeId)
        {
            var elementType = realType.GetElementType();

            if (elementType == typeof(byte))
            {
                return bytes;
            }

            // 根据 ModeId 自适应解压缩还原
            if (modeId != 0)
            {
                if (elementType == typeof(int) && modeId == (int)Int32ArrayCompressMode.FrameOfReference)
                {
                    return bytes.DecompressForInt32();
                }

                if (elementType == typeof(float) && modeId == (int)SingleArrayCompressMode.Gorilla)
                {
                    return bytes.DecompressGorillaSingle();
                }

                if (elementType == typeof(double) && modeId == (int)DoubleArrayCompressMode.Gorilla)
                {
                    return bytes.DecompressGorillaDouble();
                }

                if (elementType == typeof(long) && modeId == (int)Int64ArrayCompressMode.Simple8b)
                {
                    return bytes.DecompressSimple8bInt64();
                }

                if (elementType == typeof(short) && modeId == (int)Int16ArrayCompressMode.DeltaBitPacking)
                {
                    return bytes.DecompressDeltaBitPackingInt16();
                }
            }

            // 默认 Raw 模式
            return ConvertBytesToArray(elementType, bytes);
        }

        /// <inheritdoc/>
        public override byte[] ConvertValueToBytes(object value)
        {
            return ConvertValueToBytes(value?.GetType(), value, 0);
        }

        /// <inheritdoc/>
        public override byte[] ConvertValueToBytes(Type type, object value)
        {
            int modeId = GetSerializationMode(type);
            return ConvertValueToBytes(type, value, modeId);
        }

        /// <inheritdoc/>
        public override byte[] ConvertValueToBytes(Type type, object value, int modeId)
        {
            if (value is byte[] data)
            {
                return data;
            }

            var elemType = type.IsArray ? type.GetElementType() : value.GetType().GetElementType();

            // 根据指定的 ModeId 进行压缩序列化
            if (modeId != 0)
            {
                if (elemType == typeof(int) && modeId == (int)Int32ArrayCompressMode.FrameOfReference && value is int[] intArr)
                {
                    return intArr.CompressFor();
                }

                if (elemType == typeof(float) && modeId == (int)SingleArrayCompressMode.Gorilla && value is float[] floatArr)
                {
                    return floatArr.CompressGorilla();
                }

                if (elemType == typeof(double) && modeId == (int)DoubleArrayCompressMode.Gorilla && value is double[] doubleArr)
                {
                    return doubleArr.CompressGorilla();
                }

                if (elemType == typeof(long) && modeId == (int)Int64ArrayCompressMode.Simple8b && value is long[] longArr)
                {
                    return longArr.CompressSimple8b();
                }

                if (elemType == typeof(short) && modeId == (int)Int16ArrayCompressMode.DeltaBitPacking && value is short[] shortArr)
                {
                    return shortArr.CompressDeltaBitPacking();
                }
            }

            // 默认 Raw 模式
            return ConvertArrayToBytes((Array)value);
        }

        /// <summary>
        /// 将基元类型的数组转为字节数组 (Raw 内存拷贝)
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
                if (elemType.IsPrimitive)
                {
                    Buffer.BlockCopy(array, 0, bytes, 0, length);
                }
                else
                {
                    var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                    try
                    {
                        Marshal.Copy(handle.AddrOfPinnedObject(), bytes, 0, length);
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
            }
            catch (Exception ex)
            {
                var e = new InvalidOperationException($"无法序列化`{elemType.FullName}[]`. (TypeSize={size})", ex);
                throw e;
            }
            return bytes;
        }

        /// <summary>
        /// 将字节数组还原为基元类型的数组 (Raw 内存拷贝)
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
                if (elemType.IsPrimitive)
                {
                    Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
                }
                else
                {
                    var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                    try
                    {
                        Marshal.Copy(bytes, 0, handle.AddrOfPinnedObject(), bytes.Length);
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
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
