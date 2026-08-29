using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests.Core
{
    public class SharedReferenceModel
    {
        public string Title { get; set; }
        public byte[] DataA { get; set; }
        public byte[] DataB { get; set; }
        public int[] WaveformA { get; set; }
        public int[] WaveformB { get; set; }
        public string[] TagsA { get; set; }
        public string[] TagsB { get; set; }
        public Point LocationA { get; set; }
        public Point LocationB { get; set; }
    }

    [TestClass]
    public class JbinReferenceReuseTests
    {
        [TestMethod]
        public void TestSharedByteArray_DeduplicationAndReferenceEquality()
        {
            var sharedBytes = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE };

            var model = new SharedReferenceModel
            {
                Title = "SharedByteArrayTest",
                DataA = sharedBytes,
                DataB = sharedBytes // 传入完全相同的 byte[] 实例
            };

            var jbin = JbinObject.FromObject(model);

            // 验证 DataBlocks 数量：DataBlocks[0] 为 Header，DataBlocks[1] 为 sharedBytes，仅 1 个 Payload Block
            Assert.AreEqual(2, jbin.DataBlocks.Count, "相同 byte[] 引用应去重，DataBlocks 仅包含 1 个 Header + 1 个 Payload");

            var bytes = jbin.ToBytes();
            var parsedJbin = JbinObject.Parse(bytes);
            var restored = parsedJbin.ToObject<SharedReferenceModel>();

            Assert.IsNotNull(restored);
            Assert.IsNotNull(restored.DataA);
            Assert.IsNotNull(restored.DataB);

            // 验证内容一致
            TestAssertHelper.AssertSequenceEqual(sharedBytes, restored.DataA);
            TestAssertHelper.AssertSequenceEqual(sharedBytes, restored.DataB);

            // 核心验证：反序列化后的 DataA 与 DataB 必须指向同一内存实例！
            Assert.IsTrue(object.ReferenceEquals(restored.DataA, restored.DataB), "反序列化后 DataA 与 DataB 必须是同一对象引用");

            // 修改 DataA，DataB 必须同步变化
            restored.DataA[0] = 0x99;
            Assert.AreEqual((byte)0x99, restored.DataB[0], "修改共享引用 DataA，DataB 必须同步体现变化");
        }

        [TestMethod]
        public void TestSharedPrimitiveArray_DeduplicationAndReferenceEquality()
        {
            var sharedWaveform = Enumerable.Range(0, 5000).ToArray();

            var model = new SharedReferenceModel
            {
                Title = "SharedWaveformTest",
                WaveformA = sharedWaveform,
                WaveformB = sharedWaveform // 相同引用
            };

            var jbin = JbinObject.FromObject(model);

            // 验证仅分配 1 个负载数据块
            Assert.AreEqual(2, jbin.DataBlocks.Count, "相同 int[] 引用应去重，DataBlocks 仅包含 1 个 Header + 1 个 Payload");

            var bytes = jbin.ToBytes();
            var parsedJbin = JbinObject.Parse(bytes);
            var restored = parsedJbin.ToObject<SharedReferenceModel>();

            Assert.IsNotNull(restored);
            Assert.IsTrue(object.ReferenceEquals(restored.WaveformA, restored.WaveformB), "反序列化后 WaveformA 与 WaveformB 必须是同一对象引用");
            CollectionAssert.AreEqual(sharedWaveform, restored.WaveformA);
            CollectionAssert.AreEqual(sharedWaveform, restored.WaveformB);
        }

        [TestMethod]
        public void TestSharedStringArray_DeduplicationAndReferenceEquality()
        {
            var sharedTags = new string[] { "Alpha", "Beta", "Gamma" };

            var model = new SharedReferenceModel
            {
                Title = "SharedStringArrayTest",
                TagsA = sharedTags,
                TagsB = sharedTags // 相同引用
            };

            var jbin = JbinObject.FromObject(model);
            Assert.AreEqual(2, jbin.DataBlocks.Count, "相同 string[] 引用应去重");

            var bytes = jbin.ToBytes();
            var parsedJbin = JbinObject.Parse(bytes);
            var restored = parsedJbin.ToObject<SharedReferenceModel>();

            Assert.IsNotNull(restored);
            Assert.IsTrue(object.ReferenceEquals(restored.TagsA, restored.TagsB), "反序列化后 TagsA 与 TagsB 必须是同一对象引用");
            CollectionAssert.AreEqual(sharedTags, restored.TagsA);
        }

        [TestMethod]
        public void TestDistinctObjectsWithIdenticalContent_AreNotDeduplicated()
        {
            // 内容相同，但处于不同堆内存地址的两个独立实例
            var bytes1 = new byte[] { 1, 2, 3, 4 };
            var bytes2 = new byte[] { 1, 2, 3, 4 };

            Assert.IsFalse(object.ReferenceEquals(bytes1, bytes2), "前置验证：两个实例内存引用不同");

            var model = new SharedReferenceModel
            {
                Title = "DistinctObjectsTest",
                DataA = bytes1,
                DataB = bytes2
            };

            var jbin = JbinObject.FromObject(model);

            // 必须分别分配两个独立的 Payload Block
            Assert.AreEqual(3, jbin.DataBlocks.Count, "不同引用的实例不能误去重，应分配 2 个独立 Payload Block");

            var bytes = jbin.ToBytes();
            var parsedJbin = JbinObject.Parse(bytes);
            var restored = parsedJbin.ToObject<SharedReferenceModel>();

            Assert.IsNotNull(restored);
            Assert.IsFalse(object.ReferenceEquals(restored.DataA, restored.DataB), "反序列化后不同引用的对象依然保持独立");
            TestAssertHelper.AssertSequenceEqual(bytes1, restored.DataA);
            TestAssertHelper.AssertSequenceEqual(bytes2, restored.DataB);

            // 修改 DataA 不应影响 DataB
            restored.DataA[0] = 0xFF;
            Assert.AreEqual((byte)1, restored.DataB[0]);
        }

        [TestMethod]
        public void TestValueTypes_Structs_AreHandledCorrectly()
        {
            var p = new Point(100, 200);

            var model = new SharedReferenceModel
            {
                Title = "ValueTypeTest",
                LocationA = p,
                LocationB = p
            };

            var jbin = JbinObject.FromObject(model);
            var bytes = jbin.ToBytes();

            var parsedJbin = JbinObject.Parse(bytes);
            var restored = parsedJbin.ToObject<SharedReferenceModel>();

            Assert.IsNotNull(restored);
            Assert.AreEqual(p, restored.LocationA);
            Assert.AreEqual(p, restored.LocationB);
        }
    }
}
