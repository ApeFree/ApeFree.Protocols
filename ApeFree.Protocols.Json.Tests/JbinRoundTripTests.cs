using ApeFree.Protocols.Json.Jbin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ApeFree.Protocols.Json.Tests
{
    /// <summary>
    /// 综合往返一致性测试：验证 FromObject → ToBytes → Parse → ToObject 的完整闭环。
    /// </summary>
    [TestClass]
    public class JbinRoundTripTests
    {
        #region 辅助方法

        /// <summary>
        /// 通用的 Jbin 往返测试辅助：序列化 → byte[] → 反序列化
        /// </summary>
        private static T JbinRoundTrip<T>(T original)
        {
            var jbin = JbinObject.FromObject(original);
            var bytes = jbin.ToBytes();
            var parsed = JbinObject.Parse(bytes);
            return parsed.ToObject<T>();
        }

        #endregion

        #region 测试模型

        public class FullModel
        {
            public int IntValue { get; set; }
            public long LongValue { get; set; }
            public float FloatValue { get; set; }
            public double DoubleValue { get; set; }
            public bool BoolValue { get; set; }
            public string StringValue { get; set; }
            public TaskStatus EnumValue { get; set; }
            public byte[] ByteArray { get; set; }
            public int[] IntArray { get; set; }
            public short[] ShortArray { get; set; }
            public Point PointValue { get; set; }
            public SizeF[] SizeFArray { get; set; }
            public string[] StringArray { get; set; }
            public List<FullModel> Children { get; set; }
        }

        #endregion

        [TestMethod]
        public void RoundTrip_ComplexObject_AllFieldsPreserved()
        {
            // Arrange
            var original = new FullModel
            {
                IntValue = 42,
                LongValue = 123456789012345L,
                FloatValue = 3.14f,
                DoubleValue = 2.718281828,
                BoolValue = true,
                StringValue = "Hello Jbin",
                EnumValue = TaskStatus.RanToCompletion,
                ByteArray = new byte[] { 0x00, 0xFF, 0xAB, 0xCD },
                IntArray = new[] { 1, -2, int.MaxValue, int.MinValue, 0 },
                ShortArray = new short[] { -1, 0, 32767 },
                PointValue = new Point(100, -200),
                SizeFArray = new[] { new SizeF(1.5f, 2.5f), new SizeF(0f, 0f) },
                StringArray = new[] { "apple", "banana", "apple", "cherry" },
            };

            // Act
            var result = JbinRoundTrip(original);

            // Assert — 逐字段比对
            Assert.AreEqual(original.IntValue, result.IntValue, "IntValue");
            Assert.AreEqual(original.LongValue, result.LongValue, "LongValue");
            Assert.AreEqual(original.FloatValue, result.FloatValue, "FloatValue");
            Assert.AreEqual(original.DoubleValue, result.DoubleValue, "DoubleValue");
            Assert.AreEqual(original.BoolValue, result.BoolValue, "BoolValue");
            Assert.AreEqual(original.StringValue, result.StringValue, "StringValue");
            Assert.AreEqual(original.EnumValue, result.EnumValue, "EnumValue");

            CollectionAssert.AreEqual(original.ByteArray, result.ByteArray, "ByteArray");
            CollectionAssert.AreEqual(original.IntArray, result.IntArray, "IntArray");
            CollectionAssert.AreEqual(original.ShortArray, result.ShortArray, "ShortArray");

            Assert.AreEqual(original.PointValue, result.PointValue, "PointValue");

            Assert.AreEqual(original.SizeFArray.Length, result.SizeFArray.Length, "SizeFArray.Length");
            for (int i = 0; i < original.SizeFArray.Length; i++)
            {
                Assert.AreEqual(original.SizeFArray[i], result.SizeFArray[i], $"SizeFArray[{i}]");
            }

            CollectionAssert.AreEqual(original.StringArray, result.StringArray, "StringArray");
        }

        [TestMethod]
        public void RoundTrip_WithNullProperties_PreservesNulls()
        {
            var original = new FullModel
            {
                IntValue = 7,
                StringValue = null,
                ByteArray = null,
                IntArray = null,
                Children = null,
            };

            var result = JbinRoundTrip(original);

            Assert.AreEqual(7, result.IntValue);
            Assert.IsNull(result.StringValue);
            Assert.IsNull(result.ByteArray);
            Assert.IsNull(result.IntArray);
            Assert.IsNull(result.Children);
        }

        [TestMethod]
        public void RoundTrip_EmptyCollections_PreservesEmpty()
        {
            var original = new FullModel
            {
                ByteArray = new byte[0],
                IntArray = new int[0],
                ShortArray = new short[0],
                StringArray = new string[0],
                Children = new List<FullModel>(),
            };

            var result = JbinRoundTrip(original);

            Assert.IsNotNull(result.ByteArray);
            Assert.AreEqual(0, result.ByteArray.Length);
            Assert.IsNotNull(result.IntArray);
            Assert.AreEqual(0, result.IntArray.Length);
            Assert.IsNotNull(result.ShortArray);
            Assert.AreEqual(0, result.ShortArray.Length);
        }

        [TestMethod]
        public void RoundTrip_NestedChildren_PreservesHierarchy()
        {
            var original = new FullModel
            {
                IntValue = 1,
                StringValue = "parent",
                Children = new List<FullModel>
                {
                    new FullModel
                    {
                        IntValue = 2,
                        StringValue = "child-1",
                        ByteArray = new byte[] { 1, 2, 3 },
                        PointValue = new Point(10, 20),
                    },
                    new FullModel
                    {
                        IntValue = 3,
                        StringValue = "child-2",
                        IntArray = new[] { 100, 200 },
                    }
                }
            };

            var result = JbinRoundTrip(original);

            Assert.AreEqual("parent", result.StringValue);
            Assert.IsNotNull(result.Children);
            Assert.AreEqual(2, result.Children.Count);
            Assert.AreEqual("child-1", result.Children[0].StringValue);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, result.Children[0].ByteArray);
            Assert.AreEqual(new Point(10, 20), result.Children[0].PointValue);
            Assert.AreEqual("child-2", result.Children[1].StringValue);
            CollectionAssert.AreEqual(new[] { 100, 200 }, result.Children[1].IntArray);
        }

        [TestMethod]
        public void RoundTrip_LongValueNotCombinedId_PreservesExactValue()
        {
            // Combined ID 特征：第 63 位和第 31 位都为 1
            // 故意构造一个不具备该特征的普通 long 值
            var original = new FullModel { LongValue = 999999999999L };

            var result = JbinRoundTrip(original);

            Assert.AreEqual(999999999999L, result.LongValue);
        }

        [TestMethod]
        public void RoundTrip_EnumProperty_PreservesEnumValue()
        {
            var original = new FullModel { EnumValue = TaskStatus.Faulted };
            var result = JbinRoundTrip(original);
            Assert.AreEqual(TaskStatus.Faulted, result.EnumValue);
        }

        [TestMethod]
        public void RoundTrip_ViaStream_ConsistentWithBytes()
        {
            var original = new FullModel
            {
                IntValue = 42,
                ByteArray = new byte[] { 1, 2, 3 },
                PointValue = new Point(10, 20),
            };

            var jbin = JbinObject.FromObject(original);
            var directBytes = jbin.ToBytes();

            byte[] streamBytes;
            using (var ms = new MemoryStream())
            {
                jbin.WriteTo(ms);
                streamBytes = ms.ToArray();
            }

            CollectionAssert.AreEqual(directBytes, streamBytes,
                "ToBytes() 和 WriteTo(Stream) 产生的结果应完全一致");
        }
    }
}
