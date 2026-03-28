using ApeFree.Protocols.Json.Jbin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests
{
    /// <summary>
    /// JbinGenericArrayConverter 隔离测试：验证容器类型 (T[] / List&lt;T&gt;) 的递归序列化能力。
    /// </summary>
    [TestClass]
    public class JbinGenericArrayTests
    {
        #region 辅助

        private static T RoundTrip<T>(T original)
        {
            var jbin = JbinObject.FromObject(original);
            var bytes = jbin.ToBytes();
            return JbinObject.Parse(bytes).ToObject<T>();
        }

        public class PointArrayModel { public Point[] Data { get; set; } }
        public class PointListModel { public List<Point> Data { get; set; } }
        public class ByteArrayArrayModel { public byte[][] Data { get; set; } }
        public class SizeFListModel { public List<SizeF> Data { get; set; } }

        #endregion

        [TestMethod]
        public void RoundTrip_ArrayOfPoints_AllPreserved()
        {
            var original = new PointArrayModel
            {
                Data = new[] { new Point(1, 2), new Point(-3, 4), new Point(0, 0) }
            };

            var result = RoundTrip(original);

            Assert.AreEqual(original.Data.Length, result.Data.Length);
            for (int i = 0; i < original.Data.Length; i++)
            {
                Assert.AreEqual(original.Data[i], result.Data[i], $"Point[{i}]");
            }
        }

        [TestMethod]
        public void RoundTrip_ListOfPoints_AllPreserved()
        {
            var original = new PointListModel
            {
                Data = new List<Point> { new Point(10, 20), new Point(30, 40) }
            };

            var result = RoundTrip(original);

            Assert.IsNotNull(result.Data);
            Assert.AreEqual(2, result.Data.Count);
            Assert.AreEqual(new Point(10, 20), result.Data[0]);
            Assert.AreEqual(new Point(30, 40), result.Data[1]);
        }

        [TestMethod]
        public void RoundTrip_ArrayOfByteArrays_ViaGenericConverter()
        {
            var original = new ByteArrayArrayModel
            {
                Data = new[]
                {
                    new byte[] { 1, 2 },
                    new byte[] { 3, 4, 5 },
                }
            };

            var result = RoundTrip(original);

            Assert.AreEqual(2, result.Data.Length);
            CollectionAssert.AreEqual(new byte[] { 1, 2 }, result.Data[0]);
            CollectionAssert.AreEqual(new byte[] { 3, 4, 5 }, result.Data[1]);
        }

        [TestMethod]
        public void RoundTrip_EmptyPointList()
        {
            var original = new PointListModel { Data = new List<Point>() };
            var result = RoundTrip(original);

            // 空列表可能被 Jbin 的 DefaultValueHandling.Ignore 跳过
            // 如果还原成 null 也属正常行为，这里验证不抛异常
            if (result.Data != null)
            {
                Assert.AreEqual(0, result.Data.Count);
            }
        }

        [TestMethod]
        public void RoundTrip_SingleElementList()
        {
            var original = new PointListModel
            {
                Data = new List<Point> { new Point(42, 99) }
            };

            var result = RoundTrip(original);

            Assert.IsNotNull(result.Data);
            Assert.AreEqual(1, result.Data.Count);
            Assert.AreEqual(new Point(42, 99), result.Data[0]);
        }

        [TestMethod]
        public void RoundTrip_LargeList_1000Elements()
        {
            var original = new SizeFListModel
            {
                Data = Enumerable.Range(0, 1000)
                    .Select(i => new SizeF(i * 1.1f, i * 2.2f))
                    .ToList()
            };

            var result = RoundTrip(original);

            Assert.IsNotNull(result.Data);
            Assert.AreEqual(1000, result.Data.Count);
            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(original.Data[i].Width, result.Data[i].Width, $"Width[{i}]");
                Assert.AreEqual(original.Data[i].Height, result.Data[i].Height, $"Height[{i}]");
            }
        }
    }
}
