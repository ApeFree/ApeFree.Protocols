using ApeFree.Protocols.Json.Jbin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests
{
    /// <summary>
    /// Jbin 对象引用合并功能测试：验证同一对象实例被多处引用时的序列化去重和反序列化引用一致性�?    /// </summary>
    [TestClass]
    public class JbinReferenceMergingStrategyTests
    {
        #region 辅助

        private static T RoundTrip<T>(T original, JbinReferenceMergingStrategy merging = JbinReferenceMergingStrategyStrategy.SharedBlock)
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
                DataB = shared,  // 同一个引�?            };

            var settings = JbinObject.JsonSerializerSettings;
            settings.ReferenceMergingStrategy = JbinReferenceMergingStrategyStrategy.SharedBlock;
            var jbin = JbinObject.FromObject(original, settings);

            // header + 只有 1 �?byte[] block（而非 2 个）
            Assert.AreEqual(2, jbin.DataBlocks.Count,
                "启用合并后，同一对象应只产生 1 个数据块�? 1 �?header = 2�?);
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

            var result = RoundTrip(original, JbinReferenceMergingStrategyStrategy.SharedBlock);

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

            var result = RoundTrip(original, JbinReferenceMergingStrategyStrategy.SharedBlock);

            Assert.IsTrue(ReferenceEquals(result.DataA, result.DataB),
                "启用合并后，反序列化的两个属性应指向同一个对象实�?);
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

            var result = RoundTrip(original, JbinReferenceMergingStrategyStrategy.SharedBlock);

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

            var result = RoundTrip(original, JbinReferenceMergingStrategyStrategy.SharedBlock);

            CollectionAssert.AreEqual(shared, result.TextA, "TextA");
            CollectionAssert.AreEqual(shared, result.TextB, "TextB");
            Assert.IsTrue(ReferenceEquals(result.TextA, result.TextB),
                "string[] 也应支持引用合并");
        }

        // ================================================================
        // 禁用合并测试
        // ================================================================

        [TestMethod]
        public void Disabled_SharedByteArray_TwoBlocks()
        {
            var shared = new byte[] { 1, 2, 3 };
            var original = new SharedArrayModel
            {
                DataA = shared,
                DataB = shared,
            };

            var settings = JbinObject.JsonSerializerSettings;
            settings.ReferenceMergingStrategy = JbinReferenceMergingStrategyStrategy.Independent;
            var jbin = JbinObject.FromObject(original, settings);

            // header + 2 个独立的 byte[] block
            Assert.AreEqual(3, jbin.DataBlocks.Count,
                "禁用合并后，同一对象应产�?2 个独立数据块�? 1 �?header = 3�?);
        }

        [TestMethod]
        public void Disabled_SharedByteArray_NotReferenceEqual()
        {
            var shared = new byte[] { 1, 2, 3 };
            var original = new SharedArrayModel
            {
                DataA = shared,
                DataB = shared,
            };

            var result = RoundTrip(original, JbinReferenceMergingStrategyStrategy.Independent);

            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, result.DataA, "DataA");
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, result.DataB, "DataB");
            Assert.IsFalse(ReferenceEquals(result.DataA, result.DataB),
                "禁用合并后，反序列化的两个属性应是独立对�?);
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
                DataB = new byte[] { 1, 2, 3 },   // 独立对象 B（值相同但引用不同�?            };

            var settings = JbinObject.JsonSerializerSettings;
            settings.ReferenceMergingStrategy = JbinReferenceMergingStrategyStrategy.SharedBlock;
            var jbin = JbinObject.FromObject(original, settings);

            // 值相同但引用不同，不应合�?            Assert.AreEqual(3, jbin.DataBlocks.Count,
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
                DataB = largeArray,   // 同一个引�?            };

            var settingsEnabled = JbinObject.JsonSerializerSettings;
            settingsEnabled.ReferenceMergingStrategy = JbinReferenceMergingStrategyStrategy.SharedBlock;
            var bytesEnabled = JbinObject.FromObject(original, settingsEnabled).ToBytes();

            var settingsDisabled = JbinObject.JsonSerializerSettings;
            settingsDisabled.ReferenceMergingStrategy = JbinReferenceMergingStrategyStrategy.Independent;
            var bytesDisabled = JbinObject.FromObject(original, settingsDisabled).ToBytes();

            Assert.IsTrue(bytesEnabled.Length < bytesDisabled.Length,
                $"启用合并 ({bytesEnabled.Length} bytes) 应比禁用 ({bytesDisabled.Length} bytes) 更小");

            // 启用合并后大约节省了 100KB
            var saved = bytesDisabled.Length - bytesEnabled.Length;
            Assert.IsTrue(saved > 90000,
                $"100KB 共享数组应节省约 100KB 空间，实际节�?{saved} bytes");
        }

        // ================================================================
        // 向后兼容性：现有测试回归
        // ================================================================

        [TestMethod]
        public void Enabled_NonSharedObject_WorksNormally()
        {
            // 确保启用合并不会影响普通（非共享）对象的序列化行为
            var original = new SharedArrayModel
            {
                DataA = new byte[] { 10, 20 },
                DataB = new byte[] { 30, 40, 50 },
            };

            var result = RoundTrip(original, JbinReferenceMergingStrategyStrategy.SharedBlock);

            CollectionAssert.AreEqual(new byte[] { 10, 20 }, result.DataA);
            CollectionAssert.AreEqual(new byte[] { 30, 40, 50 }, result.DataB);
        }
    }
}
