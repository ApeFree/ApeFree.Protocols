using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// 字符串字典压缩数组转换器
    /// <para>1. 本类展示了 Jbin 转换器不仅可以做“平流拷贝”，还可以做“存储优化”。</para>
    /// <para>2. 逻辑的核心在于“字典化”：它会先统计字符串数组中出现了哪些不重复的字符串，建立一个映射字典，并将这些唯一字符串存入二进制块的前部。</para>
    /// <para>3. 数组的内容本身则被压缩成一组指向字典下标的整数索引。极大地减小了当数组包含大量重复长字符串时的总体积。</para>
    /// <para>4. 这是一个非常好的示例，演示了如何通过创建专用的转换器来根据特定业务数据特征进行极限压缩。</para>
    /// </summary>
    public class JbinStringDictArrayConverter : JbinSerializer<string[]>, IJbinFieldConverter
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
                            var itemString = Encoding.UTF8.GetString(itemBytes);
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

        public override byte[] ConvertValueToBytes(Type type, object array)
        {
            var value = (string[])array;
            var dict = value.Distinct().ToArray();

            var len = dict.Sum(s => string.IsNullOrEmpty(s) ? 0 : Encoding.UTF8.GetByteCount(s)) + (2 + dict.Length + value.Length) * sizeof(int);
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
                            var bytes = Encoding.UTF8.GetBytes(item);
                            bw.Write(bytes.Length);
                            bw.Write(bytes);
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
