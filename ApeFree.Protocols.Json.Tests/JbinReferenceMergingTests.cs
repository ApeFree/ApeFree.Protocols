using ApeFree.Protocols.Json.Jbin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests
{
    /// <summary>
    /// Jbin 对象引用合并功能测试：验证同一对象实例被多处引用时的序列化去重和反序列化引用一致性。
    /// </summary>
    [TestClass]
    public class JbinReferenceMergingTests
    {
        #region 辅助

        private static T RoundTrip<T>(T original, JbinReferenceMergingStrategy merging = JbinReferenceMergingStrategy.SharedBlock)
        {
            var settings = JbinObject.JsonSerializerSettings;
            settings.ReferenceMergingStrategy = merging;
            var jbin = JbinObject.FromObject(original, settings);
            var bytes = jbin.ToBytes();
            var parsed = JbinObject.Parse(bytes);
            return parsed.ToObject<T>(settings);
        }

        public class SharedArrayModel
        {
            public byte[] DataA { get; set; }
            public byte[] DataB { get; set; }
        }

        public class SharedIntArrayModel
        {
            public int[] NumbersA { get; set; }
            public int[] NumbersB { get; set; }
        }

        public class SharedStringArrayModel
        {
            public string[] TextA { get; set; }
            public string[] TextB { get; set; }
        }

        public class SharedCrossTypeModel
        {
            public byte[] TypedData { get; set; }
            public byte[] AnotherRef { get; set; }
        }

        #endregion

        // ================================================================
        // 核心功能测试
        // ================================================================

        [TestMethod]
        public void Enabled_SharedByteArray_OnlyOneBlock()
        {
            var shared = new byte[] { 1, 2, 3, 4, 5 };
            var original = new SharedArrayModel
            {
                DataA = shared,
                DataB = shared,  // 同一个引用
            };

            var settings = JbinObject.JsonSerializerSettings;
            settings.ReferenceMergingStrategy = JbinReferenceMergingStrategy.SharedBlock;
            var jbin = JbinObject.FromObject(original, settings);

            // header + 只有 1 个 byte[] block（而非 2 个）
            Assert.AreEqual(2, jbin.DataBlocks.Count,
                "启用合并后，同一对象应只产生 1 个数据块（+ 1 个 header = 2）");
        }

        [TestMethod]
        public void Enabled_SharedByteArray_ValuesPreserved()
        {
            var shared = new byte[] { 10, 20, 30 };
            var original = new SharedArrayModel
            {
                DataA = shared,
                DataB = shared,
            };

            var result = RoundTrip(original, JbinReferenceMergingStrategy.SharedBlock);

            CollectionAssert.AreEqual(new byte[] { 10, 20, 30 }, result.DataA, "DataA");
            CollectionAssert.AreEqual(new byte[] { 10, 20, 30 }, result.DataB, "DataB");
        }

        [TestMethod]
        public void Enabled_SharedByteArray_ReferenceEqual()
        {
            var shared = new byte[] { 1, 2, 3 };
            var original = new SharedArrayModel
            {
                DataA = shared,
                DataB = shared,
            };

            var result = RoundTrip(original, JbinReferenceMergingStrategy.SharedBlock);

            Assert.IsTrue(ReferenceEquals(result.DataA, result.DataB),
                "启用合并后，反序列化的两个属性应指向同一个对象实例");
        }

        [TestMethod]
        public void Enabled_SharedIntArray_ReferenceEqual()
        {
            var shared = new int[] { 100, 200, 300 };
            var original = new SharedIntArrayModel
            {
                NumbersA = shared,
                NumbersB = shared,
            };

            var result = RoundTrip(original, JbinReferenceMergingStrategy.SharedBlock);

            CollectionAssert.AreEqual(shared, result.NumbersA, "NumbersA");
            CollectionAssert.AreEqual(shared, result.NumbersB, "NumbersB");
            Assert.IsTrue(ReferenceEquals(result.NumbersA, result.NumbersB),
                "int[] 也应支持引用合并");
        }

        [TestMethod]
        public void Enabled_SharedStringArray_ReferenceEqual()
        {
            var shared = new string[] { "hello", "world" };
            var original = new SharedStringArrayModel
            {
                TextA = shared,
                TextB = shared,
            };

            var result = RoundTrip(original, JbinReferenceMergingStrategy.SharedBlock);

            CollectionAssert.AreEqual(shared, result.TextA, "TextA");
            CollectionAssert.AreEqual(shared, result.TextB, "TextB");
            Assert.IsTrue(ReferenceEquals(result.TextA, result.TextB),
                "string[] 也应支持引用合并");
        }

        // ================================================================
        // 禁用合并测试
        // ================================================================

        [TestMethod]
        public void Independent_SharedByteArray_TwoBlocks()
        {
            var shared = new byte[] { 1, 2, 3 };
            var original = new SharedArrayModel
            {
                DataA = shared,
                DataB = shared,
            };

            var settings = JbinObject.JsonSerializerSettings;
            settings.ReferenceMergingStrategy = JbinReferenceMergingStrategy.Independent;
            var jbin = JbinObject.FromObject(original, settings);

            // header + 2 个独立的 byte[] block
            Assert.AreEqual(3, jbin.DataBlocks.Count,
                "独立策略下，同一对象应产生 2 个独立数据块（+ 1 个 header = 3）");
        }

        [TestMethod]
        public void Independent_SharedByteArray_NotReferenceEqual()
        {
            var shared = new byte[] { 1, 2, 3 };
            var original = new SharedArrayModel
            {
                DataA = shared,
                DataB = shared,
            };

            var result = RoundTrip(original, JbinReferenceMergingStrategy.Independent);

            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, result.DataA, "DataA");
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, result.DataB, "DataB");
            Assert.IsFalse(ReferenceEquals(result.DataA, result.DataB),
                "独立策略下，反序列化的两个属性应是独立对象");
        }

        // ================================================================
        // 非共享对象（不应被错误合并）
        // ================================================================

        [TestMethod]
        public void Enabled_DifferentObjects_StayIndependent()
        {
            var original = new SharedArrayModel
            {
                DataA = new byte[] { 1, 2, 3 },   // 独立对象 A
                DataB = new byte[] { 1, 2, 3 },   // 独立对象 B（值相同但引用不同）
            };

            var settings = JbinObject.JsonSerializerSettings;
            settings.ReferenceMergingStrategy = JbinReferenceMergingStrategy.SharedBlock;
            var jbin = JbinObject.FromObject(original, settings);

            // 值相同但引用不同，不应合并
            Assert.AreEqual(3, jbin.DataBlocks.Count,
                "值相等但引用不同的对象不应被合并，应产生 2 个数据块");
        }

        // ================================================================
        // 空间节省验证
        // ================================================================

        [TestMethod]
        public void Enabled_LargeSharedArray_SavesSpace()
        {
            var rng = new Random(42);
            var largeArray = new byte[1024 * 100]; // 100KB
            rng.NextBytes(largeArray);

            var original = new SharedArrayModel
            {
                DataA = largeArray,
                DataB = largeArray,   // 同一个引用
            };

            var settingsEnabled = JbinObject.JsonSerializerSettings;
            settingsEnabled.ReferenceMergingStrategy = JbinReferenceMergingStrategy.SharedBlock;
            var bytesEnabled = JbinObject.FromObject(original, settingsEnabled).ToBytes();

            var settingsDisabled = JbinObject.JsonSerializerSettings;
            settingsDisabled.ReferenceMergingStrategy = JbinReferenceMergingStrategy.Independent;
            var bytesDisabled = JbinObject.FromObject(original, settingsDisabled).ToBytes();

            Assert.IsTrue(bytesEnabled.Length < bytesDisabled.Length,
                $"启用合并 ({bytesEnabled.Length} bytes) 应比禁用 ({bytesDisabled.Length} bytes) 更小");

            // 启用合并后大约节省了 100KB
            var saved = bytesDisabled.Length - bytesEnabled.Length;
            Assert.IsTrue(saved > 90000,
                $"100KB 共享数组应节省约 100KB 空间，实际节省 {saved} bytes");
        }

        // ================================================================
        // 向后兼容性：现有测试回归
        // ================================================================

        public class LargeDataModel
        {
            public byte[] LargeBuffer { get; set; }
            public List<string> Metadata { get; set; }
            public Dictionary<string, byte[]> BinaryDict { get; set; }
        }

        [TestMethod]
        public void ReferenceMerging_MemoryLeaksCheck()
        {
            // 使用长生命周期的 settings，模拟最容易导致内存泄漏的场景
            // 如果 Context 没有 Reset()，每一次操作都会导致上一次的大对象被 settings 间接持有而无法释放
            var settings = JbinObject.JsonSerializerSettings;
            settings.ReferenceMergingStrategy = JbinReferenceMergingStrategy.SharedBlock;

            // 预热：让 GC 趋于稳定
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long initialMemory = GC.GetTotalMemory(true);

            // 迭代 10 次，每次产生并处理约 50MB 的数据
            for (int i = 0; i < 10; i++)
            {
                // 创建一个大的数据对象 (50MB) 
                var largeArray = new byte[1024 * 1024 * 50];
                new Random().NextBytes(largeArray);

                var model = new LargeDataModel
                {
                    LargeBuffer = largeArray,
                    Metadata = Enumerable.Range(0, 1000).Select(x => $"Item {x}").ToList(),
                    BinaryDict = new Dictionary<string, byte[]> { { "Key", largeArray } } // 引用同一个数组，触发引用合并
                };

                // 序列化
                var jbin = JbinObject.FromObject(model, settings);
                var jbinBytes = jbin.ToBytes();

                // 反序列化
                var parsedJbin = JbinObject.Parse(jbinBytes);
                var result = parsedJbin.ToObject<LargeDataModel>(settings);

                // 简单的断言确保数据完整
                Assert.AreEqual(model.LargeBuffer.Length, result.LargeBuffer.Length);
                Assert.IsTrue(ReferenceEquals(result.LargeBuffer, result.BinaryDict["Key"]));

                // 显式释放局部变量（帮助分析器，虽然不是必须的）
                model = null;
                jbin = null;
                jbinBytes = null;
                parsedJbin = null;
                result = null;

                // 每轮执行 GC。如果不释放 Context 缓存，GC 无法回收刚才处理的大对象。
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            long finalMemory = GC.GetTotalMemory(true);

            // 10 次 50MB 操作总共涉及 500MB 数据。
            // 如果存在内存泄漏，内存占用会持续增长。
            // 设定阈值为 100MB 增长极限（考虑堆碎片、内部字符串常量池等正常波动）。
            long diff = finalMemory - initialMemory;
            Assert.IsTrue(diff < 1024 * 1024 * 100,
                $"检测到疑似内存泄漏。内存增长: {diff / 1024 / 1024} MB。请检查 JbinSerializeContext 是否未调用 Reset()。");
        }
    }
}
