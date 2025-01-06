using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;

namespace ApeFree.Protocols.Json.Jbin
{
    public class JbinObject
    {
        public static JsonSerializerSettings JsonSerializerSettings => new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,

            Converters = new List<JsonConverter>
            {
                new JbinBytesConverter(),
                new JbinStructConverter(),
                new JbinPrimitiveArrayConverter(),
                new JbinStringDictArrayConverter(),
                //new JbinStringConverter(),
                new JbinBitmapConverter(),
                new JbinConcurrentQueueShortsConverter(),
                new JbinObjectConverter(),
            },
        };

        /// <summary>
        /// 数据块列表
        /// </summary>
        public List<byte[]> DataBlocks { get; }

        public JbinHeader Header { get; }

        /// <summary>
        /// Json信息
        /// </summary>
        public string Json => Header.Content;

        /// <summary>
        /// 构造Jbin对象
        /// </summary>
        /// <param name="dataBlocks"></param>
        internal JbinObject(List<byte[]> dataBlocks)
        {
            DataBlocks = dataBlocks;
            var jsonHeader = DataBlocks.First().EncodeToString();
            Header = JsonConvert.DeserializeObject<JbinHeader>(jsonHeader);
        }

        /// <summary>
        /// 序列化对象
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var lst = new List<byte[]>();

            // 数据块个数
            var blockCount = BitConverter.GetBytes(DataBlocks.Count);
            lst.Add(blockCount);

            // 每个数据块的大小
            var blockSizeArray = DataBlocks.Select(b => BitConverter.GetBytes((uint)b.Length)).ToArray();
            lst.AddRange(blockSizeArray);

            // 每个数据块的内容
            lst.AddRange(DataBlocks);

            // 最终的Jbin数据
            var bytes = ConcatByteArrays(lst);

            return bytes;
        }

        /// <summary>
        /// 将多个字节数组合并到一起
        /// </summary>
        /// <param name="byteArrays"></param>
        /// <returns></returns>
        public static byte[] ConcatByteArrays(IEnumerable<byte[]> byteArrays)
        {
            int totalLength = byteArrays.Sum(arr => arr.Length);
            byte[] result = new byte[totalLength];
            int offset = 0;
            foreach (var byteArray in byteArrays)
            {
                Array.Copy(byteArray, 0, result, offset, byteArray.Length);
                offset += byteArray.Length;
            }
            return result;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ToObject<T>(JsonSerializerSettings settings = null)
        {
            // 在序列化配置中添加转换器
            if (settings == null)
            {
                settings = JsonSerializerSettings;
            }

            var types = Header.Types.ToList();

            var context = new JbinSerializeContext(DataBlocks, types, settings);

            settings.Converters.ForEach(x =>
            {
                if (x is JbinConverter c)
                {
                    c.Initialize(context);
                }
            });

            // 反序列化到对象
            var obj = JsonConvert.DeserializeObject<T>(Json, settings);

            return obj;
        }

        /// <summary>
        /// 通过对象生成Jbin对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static JbinObject FromObject(object obj, JsonSerializerSettings settings = null)
        {
            // 在序列化配置中添加转换器
            if (settings == null)
            {
                settings = JsonSerializerSettings;
            }

            // 构造Jbin序列化上下文
            var context = new JbinSerializeContext(settings);

            // 为所有转换器初始化
            settings.Converters.ForEach(x =>
            {
                if (x is JbinConverter c)
                {
                    c.Initialize(context);
                }
            });

            // 序列化
            var jsonStructure = JsonConvert.SerializeObject(obj, Formatting.None, settings);

            // Jbin数据头
            var header = new JbinHeader()
            {
                Content = jsonStructure,
                Types = context.DataTypes.ToArray()
            };

            // 将Json字符串插入数据块头部
            context.DataBlocks.Insert(0, header.ToString().GetBytes());

            // 使用数据块构造Jbin对象
            var jbin = new JbinObject(context.DataBlocks);

            return jbin;
        }

        /// <summary>
        /// 将字节数组解析为Jbin对象
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static JbinObject Parse(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                return Parse(stream);
            }
        }

        /// <summary>
        /// 读取流数据并解析为Jbin对象
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static JbinObject Parse(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // 数据块个数
                var blockCount = reader.ReadUInt32();

                // 每个数据块的大小
                var blockSizeArray = Enumerable.Range(0, (int)blockCount).Select(x => reader.ReadUInt32()).ToArray();

                // 每个数据块的内容
                var blocks = blockSizeArray.Select(size => reader.ReadBytes((int)size)).ToList();   // TODO: 这里可以使用并发取数据提升性能

                // 构造Jbin对象
                var jbin = new JbinObject(blocks);

                return jbin;
            }
        }
    }
}
