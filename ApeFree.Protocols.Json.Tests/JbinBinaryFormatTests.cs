using ApeFree.Protocols.Json.Jbin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ApeFree.Protocols.Json.Tests
{
    /// <summary>
    /// Jbin 二进制协议格式验证测试。
    /// 直接检查 JbinObject 内部的 DataBlocks 结构、Header 内容和 Combined ID 编解码规则。
    /// </summary>
    [TestClass]
    public class JbinBinaryFormatTests
    {
        #region 辅助

        public class SimpleModel
        {
            public byte[] Data { get; set; }
            public int[] Numbers { get; set; }
        }

        #endregion

        [TestMethod]
        public void BinaryFormat_BlockCount_MatchesExpected()
        {
            // 一个含有 byte[] 和 int[] 的对象，应产生至少 3 个 DataBlocks：
            // [0] = JbinHeader, [1] = byte[] 数据块, [2] = int[] 数据块
            var original = new SimpleModel
            {
                Data = new byte[] { 1, 2, 3 },
                Numbers = new[] { 10, 20, 30 },
            };

            var jbin = JbinObject.FromObject(original);

            // header + 2 个数据块 = 3
            Assert.AreEqual(3, jbin.DataBlocks.Count,
                "应有 3 个数据块: header + byte[] + int[]");
        }

        [TestMethod]
        public void BinaryFormat_FirstBlock_IsJsonHeader()
        {
            var original = new SimpleModel { Data = new byte[] { 1 } };
            var jbin = JbinObject.FromObject(original);

            // 第一个块应是可解析为 JbinHeader 的 JSON
            var headerJson = Encoding.UTF8.GetString(jbin.DataBlocks[0]);
            Assert.IsTrue(headerJson.Contains("Content"), "Header 应包含 Content 字段");

            // 验证能正确解析
            var header = JsonConvert.DeserializeObject<JbinHeader>(headerJson);
            Assert.IsNotNull(header);
            Assert.IsNotNull(header.Content, "Header.Content 不应为 null");
        }

        [TestMethod]
        public void BinaryFormat_HeaderContainsTypes()
        {
            var original = new SimpleModel
            {
                Data = new byte[] { 1 },
                Numbers = new[] { 10 },
            };

            var jbin = JbinObject.FromObject(original);

            Assert.IsNotNull(jbin.Header.Types, "Header.Types 不应为 null");
            Assert.IsTrue(jbin.Header.Types.Length > 0, "Header.Types 应至少包含一个类型");

            // byte[] 和 int[] 的类型都应被记录
            Assert.IsTrue(jbin.Header.Types.Any(t => t == typeof(byte[])),
                "Header.Types 应包含 byte[] 类型");
            Assert.IsTrue(jbin.Header.Types.Any(t => t == typeof(int[])),
                "Header.Types 应包含 int[] 类型");
        }

        [TestMethod]
        public void BinaryFormat_ToBytesAndParse_Symmetric()
        {
            var original = new SimpleModel
            {
                Data = new byte[] { 0xAA, 0xBB },
                Numbers = new[] { 42, 99 },
            };

            var jbin1 = JbinObject.FromObject(original);
            var bytes = jbin1.ToBytes();

            // 解析回来
            var jbin2 = JbinObject.Parse(bytes);

            // DataBlocks 数量应一致
            Assert.AreEqual(jbin1.DataBlocks.Count, jbin2.DataBlocks.Count, "DataBlocks 数量");

            // 每个块的内容应逐字节一致
            for (int i = 0; i < jbin1.DataBlocks.Count; i++)
            {
                CollectionAssert.AreEqual(jbin1.DataBlocks[i], jbin2.DataBlocks[i],
                    $"DataBlocks[{i}] 内容");
            }
        }

        [TestMethod]
        public void BinaryFormat_CombinedId_EncodeDecode()
        {
            // 验证 Combined ID 的编码规则：
            // combinedId = (typeId << 32) | (uint)blockId
            // combinedId |= (long)1 << 31   (blockId 最高位置 1)
            // combinedId |= (long)1 << 63   (typeId 最高位置 1)
            //
            // 反向提取：
            // typeId = (int)((id >> 32) & 0x7FFFFFFF)
            // blockId = (int)(id & 0x7FFFFFFF)

            int originalTypeId = 5;
            int originalBlockId = 42;

            // 编码
            long combinedId = ((long)originalTypeId << 32) | (uint)originalBlockId;
            combinedId |= (long)1 << 31;
            combinedId |= (long)1 << 63;

            // 验证特征位
            Assert.IsTrue(((combinedId >> 63) & 1) != 0, "第 63 位应为 1");
            Assert.IsTrue(((combinedId >> 31) & 1) != 0, "第 31 位应为 1");

            // 解码
            int decodedTypeId = (int)((combinedId >> 32) & 0x7FFFFFFF);
            int decodedBlockId = (int)(combinedId & 0x7FFFFFFF);

            Assert.AreEqual(originalTypeId, decodedTypeId, "TypeId 解码");
            Assert.AreEqual(originalBlockId, decodedBlockId, "BlockId 解码");
        }

        [TestMethod]
        public void BinaryFormat_StreamParse_EquivalentToBytesParse()
        {
            var original = new SimpleModel
            {
                Data = new byte[] { 1, 2, 3, 4, 5 },
                Numbers = new[] { -1, 0, 1 },
            };

            var jbin = JbinObject.FromObject(original);
            var bytes = jbin.ToBytes();

            // 通过 byte[] 解析
            var fromBytes = JbinObject.Parse(bytes);

            // 通过 Stream 解析
            JbinObject fromStream;
            using (var ms = new MemoryStream(bytes))
            {
                fromStream = JbinObject.Parse(ms);
            }

            // 两种方式产生的 DataBlocks 应完全一致
            Assert.AreEqual(fromBytes.DataBlocks.Count, fromStream.DataBlocks.Count);
            for (int i = 0; i < fromBytes.DataBlocks.Count; i++)
            {
                CollectionAssert.AreEqual(fromBytes.DataBlocks[i], fromStream.DataBlocks[i],
                    $"DataBlocks[{i}]");
            }
        }
    }
}
