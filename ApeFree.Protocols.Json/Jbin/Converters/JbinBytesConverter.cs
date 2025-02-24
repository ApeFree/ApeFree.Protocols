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
            if (value.GetType() == SupportedTypes[0])
            {
                return (byte[])value;
            }
            else
            {
                var group = new List<byte[]>();
                if (value is byte[][] array)
                {
                    group = array.ToList();
                }
                else if (value is List<byte[]> list)
                {
                    group = list;
                }

                using (var ms = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        bw.Write(group.Count);
                        foreach (var block in group)
                        {
                            bw.Write(block.Length);
                            bw.Write(block);
                        }
                    }
                    return ms.ToArray();
                }
            }
        }
    }
}
