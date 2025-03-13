using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ApeFree.Protocols.Json.Jbin
{
    /*
    public class JbinStringConverter : JbinSerializer<string>
    {
        protected override string ConvertBytesToValue(byte[] bytes, Type defineType, Type realType)
        {
            return bytes.EncodeToString();
        }

        protected override byte[] ConvertValueToBytes(string value)
        {
            return value.GetBytes();
        }
    }

    public class JbinStringArrayConverter : JbinSerializer<string[]>
    {
        protected override string[] ConvertBytesToValue(byte[] bytes, Type defineType, Type realType)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                using (BinaryReader br = new BinaryReader(stream))
                {
                    var arrayLen = br.ReadInt32();
                    var array = new string[arrayLen];

                    for (int i = 0; i < arrayLen; i++)
                    {
                        var itemLen = br.ReadInt32();

                        if (itemLen == -1)
                        {
                            array[i] = null;
                        }
                        else if (itemLen == 0)
                        {
                            array[i] = string.Empty;
                        }
                        else
                        {
                            var itemBytes = br.ReadBytes(itemLen);
                            var itemString = itemBytes.EncodeToString();
                            array[i] = itemString;
                        }
                    }

                    return array;
                }
            }
        }

        protected override byte[] ConvertValueToBytes(string[] value)
        {
            var len = value.Where(x => x != null).Sum(Encoding.UTF8.GetByteCount) + (value.Length + 1) * sizeof(int);
            var buffer = new byte[len];

            using (MemoryStream stream = new MemoryStream(buffer))
            {
                using (BinaryWriter bw = new BinaryWriter(stream))
                {
                    // 数组长度
                    bw.Write(value.Length);

                    // 写入每一个字符串
                    foreach (string item in value)
                    {
                        if (item == null)
                        {
                            bw.Write(-1);
                        }
                        else if (item == string.Empty)
                        {
                            bw.Write(0);
                        }
                        else
                        {
                            bw.Write(item.Length);
                            bw.Write(item.GetBytes());
                        }
                    }
                }
            }
            return buffer;
        }
    }*/

    public class JbinStringDictArrayConverter : JbinSerializer<string[]>, IJbinFieldDeserializer
    {
        public bool CanDeserialize(Type defineType, Type realType)
        {
            return realType == typeof(string[]);
        }

        public object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType)
        {
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

        public override byte[] ConvertValueToBytes(object array)
        {
            var value = (string[])array;
            var dict = value.Distinct().ToArray();

            var len = dict.Sum(Encoding.UTF8.GetByteCount) + (2 + dict.Length + value.Length) * sizeof(int);
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
                        else if (item == string.Empty)
                        {
                            bw.Write(0);
                        }
                        else
                        {
                            bw.Write(item.Length);
                            bw.Write(item.GetBytes());
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

        public override byte[] ConvertValueToBytes(Type type, object value)
        {
            return ConvertValueToBytes(value.GetType(), value);
        }
    }
}
