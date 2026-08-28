using STTech.CodePlus.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// 字符串数组转换器（支持字典索引去重与 Deflate 紧凑流压缩）
    /// </summary>
    public class JbinStringDictArrayConverter : JbinSerializer<string[]>, IJbinFieldDeserializer
    {
        /// <summary>
        /// 字符串数组序列化模式
        /// </summary>
        public StringArrayCompressMode StringArrayMode { get; set; } = StringArrayCompressMode.Dictionary;

        /// <inheritdoc/>
        public bool CanDeserialize(Type defineType, Type realType)
        {
            return realType == typeof(string[]);
        }

        /// <inheritdoc/>
        public override int GetSerializationMode(Type objectType)
        {
            if (objectType == typeof(string[]))
            {
                return (int)StringArrayMode;
            }
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
            if (modeId == (int)StringArrayCompressMode.Deflate)
            {
                return bytes.DeserializeToStringArray();
            }

            using (MemoryStream stream = new MemoryStream(bytes))
            {
                using (BinaryReader br = new BinaryReader(stream))
                {
                    var dictLen = br.ReadInt32();
                    var arrayLen = br.ReadInt32();

                    var dict = new string[dictLen];
                    var array = new string[arrayLen];

                    for (int i = 0; i < dictLen; i++)
                    {
                        var itemLen = br.ReadInt32();

                        if (itemLen == -1)
                        {
                            dict[i] = null;
                        }
                        else if (itemLen == 0)
                        {
                            dict[i] = string.Empty;
                        }
                        else
                        {
                            var itemBytes = br.ReadBytes(itemLen);
                            var itemString = itemBytes.EncodeToString();
                            dict[i] = itemString;
                        }
                    }

                    for (int i = 0; i < arrayLen; i++)
                    {
                        var itemIndex = br.ReadInt32();
                        array[i] = dict[itemIndex];
                    }

                    return array;
                }
            }
        }

        /// <inheritdoc/>
        public override byte[] ConvertValueToBytes(object array)
        {
            return ConvertValueToBytes(array?.GetType(), array, 0);
        }

        /// <inheritdoc/>
        public override byte[] ConvertValueToBytes(Type type, object value)
        {
            int modeId = GetSerializationMode(type);
            return ConvertValueToBytes(type, value, modeId);
        }

        /// <inheritdoc/>
        public override byte[] ConvertValueToBytes(Type type, object array, int modeId)
        {
            var value = (string[])array;

            if (modeId == (int)StringArrayCompressMode.Deflate)
            {
                return value.SerializeToBytes();
            }

            var dict = value.Distinct().ToArray();

            var totalStringBytes = dict.Where(x => !string.IsNullOrEmpty(x)).Sum(x => Encoding.UTF8.GetByteCount(x));
            var len = totalStringBytes + (2 + dict.Length + value.Length) * sizeof(int);
            var buffer = new byte[len];

            using (MemoryStream stream = new MemoryStream(buffer))
            {
                using (BinaryWriter bw = new BinaryWriter(stream))
                {
                    // 数组长度
                    bw.Write(dict.Length);
                    bw.Write(value.Length);

                    // 写入每一个字符串
                    foreach (string item in dict)
                    {
                        if (item == null)
                        {
                            bw.Write(-1);
                        }
                        else if (item.Length == 0)
                        {
                            bw.Write(0);
                        }
                        else
                        {
                            var itemBytes = item.GetBytes();
                            bw.Write(itemBytes.Length);
                            bw.Write(itemBytes);
                        }
                    }

                    foreach (var item in value)
                    {
                        var index = Array.IndexOf(dict, item);
                        bw.Write(index);
                    }
                }
            }
            return buffer;
        }
    }
}
