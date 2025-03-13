using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ApeFree.Protocols.Json.Jbin
{

    public class JbinBytesConverter : JbinSerializer<byte[]>, IJbinFieldConverter
    {
        private static readonly Type[] SupportedTypes = new Type[] { typeof(byte[]), typeof(byte[][]), typeof(List<byte[]>) };

        public bool CanDeserialize(Type defineType, Type realType)
        {
            return SupportedTypes.Contains(realType);
        }

        public object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType)
        {
            if (realType == SupportedTypes[0])
            {
                return bytes;
            }
            else
            {
                List<byte[]> group = new List<byte[]>();

                using (var ms = new MemoryStream(bytes))
                {
                    using (var br = new BinaryReader(ms))
                    {
                        var size = br.ReadInt32();
                        for (int i = 0; i < size; i++)
                        {
                            var blockLen = br.ReadInt32();
                            var block = br.ReadBytes(blockLen);
                            group.Add(block);
                        }
                    }
                }


                if (realType == SupportedTypes[1])
                {
                    return group.ToArray();
                }
                else if (realType == SupportedTypes[2])
                {
                    return group;
                }
            }

            return null;
        }

        public override byte[] ConvertValueToBytes(object value)
        {
            var type = value.GetType();

            return ConvertValueToBytes(type, value);

            //if (value.GetType() == SupportedTypes[0])
            //{
            //    return (byte[])value;
            //}
            //else
            //{
            //    var group = new List<byte[]>();
            //    if (value is byte[][] array)
            //    {
            //        group = array.ToList();
            //    }
            //    else if (value is List<byte[]> list)
            //    {
            //        group = list;
            //    }

            //    using (var ms = new MemoryStream())
            //    {
            //        using (BinaryWriter bw = new BinaryWriter(ms))
            //        {
            //            bw.Write(group.Count);
            //            foreach (var block in group)
            //            {
            //                bw.Write(block.Length);
            //                bw.Write(block);
            //            }
            //        }
            //        return ms.ToArray();
            //    }
            //}
        }

        public override byte[] ConvertValueToBytes(Type type, object value)
        {
            // 基础类型直接处理
            if (type == typeof(byte[]))
            {
                return (byte[])value; // 原始数据直接返回
            }

            // 集合类型序列化
            if (_collectionSerializers.TryGetValue(type, out var serializer))
            {
                return serializer(value);
            }

            throw new NotSupportedException($"未实现类型[{type.FullName}]的序列化实现。");
        }

        private static readonly Dictionary<Type, Func<object, byte[]>> _collectionSerializers = new()
        {
            [typeof(byte[][])] = obj => SerializeBlockArray((byte[][])obj),
            [typeof(List<byte[]>)] = obj => SerializeBlockArray(((List<byte[]>)obj).ToArray())
        };

        private static byte[] SerializeBlockArray(byte[][] blocks)
        {
            // 预计算总长度：4字节(块数) + 每个块的4字节长度+实际数据
            int totalLength = sizeof(int);
            foreach (var block in blocks)
            {
                totalLength += sizeof(int) + block.Length;
            }

            byte[] result = new byte[totalLength];
            int offset = 0;

            // 写入块数
            Buffer.BlockCopy(BitConverter.GetBytes(blocks.Length), 0, result, offset, sizeof(int));
            offset += sizeof(int);

            // 写入每个块
            foreach (var block in blocks)
            {
                // 写入长度
                Buffer.BlockCopy(BitConverter.GetBytes(block.Length), 0, result, offset, sizeof(int));
                offset += sizeof(int);

                // 写入数据
                Buffer.BlockCopy(block, 0, result, offset, block.Length);
                offset += block.Length;
            }

            return result;
        }
    }
}
