using ApeFree.Protocols.Json.Jbin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;

namespace ApeFree.Protocols.Json.Tests
{
    /// <summary>
    /// JbinGenericStructConverter 隔离测试：验证 Point / PointF / Size / SizeF / Color 的二进制压缩往返一致性。
    /// </summary>
    [TestClass]
    public class JbinGenericStructTests
    {
        #region 辅助

        private static T RoundTrip<T>(T original)
        {
            var jbin = JbinObject.FromObject(original);
            var bytes = jbin.ToBytes();
            return JbinObject.Parse(bytes).ToObject<T>();
        }

        public class PointModel { public Point Value { get; set; } }
        public class PointFModel { public PointF Value { get; set; } }
        public class SizeModel { public Size Value { get; set; } }
        public class SizeFModel { public SizeF Value { get; set; } }
        public class ColorModel { public Color Value { get; set; } }

        #endregion

        [TestMethod]
        public void RoundTrip_Point_ExactXY()
        {
            var original = new PointModel { Value = new Point(100, -200) };
            var result = RoundTrip(original);
            Assert.AreEqual(100, result.Value.X);
            Assert.AreEqual(-200, result.Value.Y);
        }

        [TestMethod]
        public void RoundTrip_PointF_FloatPrecision()
        {
            var original = new PointFModel { Value = new PointF(3.14f, -2.71f) };
            var result = RoundTrip(original);
            Assert.AreEqual(3.14f, result.Value.X, "X");
            Assert.AreEqual(-2.71f, result.Value.Y, "Y");
        }

        [TestMethod]
        public void RoundTrip_Size_ExactWidthHeight()
        {
            var original = new SizeModel { Value = new Size(1920, 1080) };
            var result = RoundTrip(original);
            Assert.AreEqual(1920, result.Value.Width);
            Assert.AreEqual(1080, result.Value.Height);
        }

        [TestMethod]
        public void RoundTrip_SizeF_FloatPrecision()
        {
            var original = new SizeFModel { Value = new SizeF(99.5f, 0.001f) };
            var result = RoundTrip(original);
            Assert.AreEqual(99.5f, result.Value.Width);
            Assert.AreEqual(0.001f, result.Value.Height);
        }

        [TestMethod]
        public void RoundTrip_Color_ExactArgb()
        {
            var original = new ColorModel { Value = Color.FromArgb(128, 64, 32, 16) };
            var result = RoundTrip(original);
            Assert.AreEqual(128, result.Value.A, "Alpha");
            Assert.AreEqual(64, result.Value.R, "Red");
            Assert.AreEqual(32, result.Value.G, "Green");
            Assert.AreEqual(16, result.Value.B, "Blue");
        }

        [TestMethod]
        public void RoundTrip_PointZero()
        {
            var original = new PointModel { Value = new Point(0, 0) };
            var result = RoundTrip(original);
            Assert.AreEqual(Point.Empty, result.Value);
        }

        [TestMethod]
        public void RoundTrip_PointMaxMinValues()
        {
            var original = new PointModel { Value = new Point(int.MaxValue, int.MinValue) };
            var result = RoundTrip(original);
            Assert.AreEqual(int.MaxValue, result.Value.X);
            Assert.AreEqual(int.MinValue, result.Value.Y);
        }

        [TestMethod]
        public void BinarySize_Point_Is8Bytes()
        {
            // Point 由两个 int (4+4=8) 组成，二进制块应恰好是 8 字节
            var original = new PointModel { Value = new Point(1, 2) };
            var jbin = JbinObject.FromObject(original);

            // DataBlocks: [0] = header, [1] = Point 的二进制块
            Assert.IsTrue(jbin.DataBlocks.Count >= 2, "至少应包含 header 和一个数据块");

            // 找到 Point 对应的数据块（非 header，长度应为 8）
            bool found8ByteBlock = false;
            for (int i = 1; i < jbin.DataBlocks.Count; i++)
            {
                if (jbin.DataBlocks[i].Length == 8)
                {
                    found8ByteBlock = true;
                    break;
                }
            }
            Assert.IsTrue(found8ByteBlock, "应存在一个 8 字节的 Point 数据块");
        }
    }
}
