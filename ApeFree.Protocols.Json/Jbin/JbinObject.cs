using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography.X509Certificates;

namespace ApeFree.Protocols.Json.Jbin
{
    public class JbinObject : IDisposable
    {
        private bool disposedValue;

        public static JsonSerializerSettings JsonSerializerSettings => new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new CachedContractResolver(),
            TraceWriter = null,

            Converters = new List<JsonConverter>
            {
                new JbinDeserializer(),
                new JbinGenericArrayConverter(),
                new JbinBytesConverter(),
                new JbinGenericStructConverter(),
                new JbinStringDictArrayConverter(),
                new JbinPrimitiveArrayConverter(),
                //new JbinStringConverter(),
                //new JbinBitmapConverter(),
                //new JbinConcurrentQueueShortsConverter(),
                //new JbinObjectConverter(),
            },
        };

        /// <summary>
        /// 数据块列表
        /// </summary>
        public List<byte[]> DataBlocks { get; private set; }

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
            var headerSize = (uint)(DataBlocks.Count + 1) * sizeof(uint);
            ulong blockSize = 0;

            foreach (var block in DataBlocks)
            {
                blockSize += (uint)block.Length;
            }

            byte[] bytes = new byte[headerSize + blockSize];

            int offset = 0;

            // 将数据块的个数拷贝到bytes中
            BitConverter.GetBytes((uint)DataBlocks.Count).CopyTo(bytes, offset);
            offset += sizeof(uint);

            // 将所有数据块的长度拷贝到bytes中
            foreach (var block in DataBlocks)
            {
                BitConverter.GetBytes((uint)block.Length).CopyTo(bytes, offset);
                offset += sizeof(uint);
            }

            // 拷贝数据块内容
            foreach (var block in DataBlocks)
            {
                block.CopyTo(bytes, offset);
                offset += block.Length;
            }

            return bytes;
        }

        /// <summary>
        /// 序列化对象到流
        /// </summary>
        /// <param name="stream"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void WriteTo(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream), "Stream cannot be null.");
            }

            // 将数据块的个数写入到 stream
            stream.Write(BitConverter.GetBytes((uint)DataBlocks.Count), 0, 4);

            // 将所有数据块的长度写入到 stream
            foreach (var block in DataBlocks)
            {
                stream.Write(BitConverter.GetBytes((uint)block.Length), 0, 4);
            }

            // 拷贝数据块内容
            foreach (var block in DataBlocks)
            {
                stream.Write(block, 0, block.Length);
            }
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ToObject<T>(JsonSerializerSettings settings = null)
        {
            // 反序列化到对象
            var obj = (T)ToObject(typeof(T), settings);
            return obj;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public object ToObject(Type type, JsonSerializerSettings settings = null)
        {
            // 在序列化配置中添加转换器
            if (settings == null)
            {
                settings = JsonSerializerSettings;
            }

            var types = Header.Types.ToList();

            var context = new JbinSerializeContext(DataBlocks, types, settings, SerializationMode.Deserialize);

            settings.Converters.ForEach(x =>
            {
                if (x is JbinConverter c)
                {
                    c.Initialize(context);
                }
            });

            // 反序列化到对象
            var obj = JsonConvert.DeserializeObject(Json, type, settings);

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
            var context = new JbinSerializeContext(settings, SerializationMode.Serialize);

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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DataBlocks?.Clear();
                }

                DataBlocks = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    internal class CachedContractResolver : DefaultContractResolver
    {
        private readonly ConcurrentDictionary<Type, JsonContract> _cache = new();

        protected override JsonContract CreateContract(Type type)
        {
            // 先查缓存，没有再生成
            return _cache.GetOrAdd(type, base.CreateContract);
        }
    }
}
