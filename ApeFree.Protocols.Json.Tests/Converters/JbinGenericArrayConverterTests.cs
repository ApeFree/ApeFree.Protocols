using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests.Converters
{
    [TestClass]
    public class JbinGenericArrayConverterTests
    {
        private T Roundtrip<T>(T obj)
        {
            var jbin = JbinObject.FromObject(obj);
            var bytes = jbin.ToBytes();
            var parsed = JbinObject.Parse(bytes);
            return parsed.ToObject<T>();
        }

        [TestMethod]
        public void TestStructArrays()
        {
            var model = new StructCollectionsModel
            {
                PointArray = new Point[] { new Point(0, 0), new Point(100, 200), new Point(-50, -30) },
                PointFArray = new PointF[] { new PointF(1.1f, 2.2f), new PointF(-3.3f, 4.4f) },
                SizeArray = new Size[] { new Size(10, 20), new Size(800, 600) },
                SizeFArray = new SizeF[] { new SizeF(12.5f, 34.8f), new SizeF(100.1f, 200.2f) },
                ColorArray = new Color[] { Color.Red, Color.Green, Color.Blue, Color.FromArgb(128, 255, 0, 128) }
            };

            var result = Roundtrip(model);

            Assert.IsNotNull(result);

            Assert.AreEqual(model.PointArray.Length, result.PointArray.Length);
            for (int i = 0; i < model.PointArray.Length; i++)
            {
                Assert.AreEqual(model.PointArray[i], result.PointArray[i]);
            }

            Assert.AreEqual(model.PointFArray.Length, result.PointFArray.Length);
            for (int i = 0; i < model.PointFArray.Length; i++)
            {
                Assert.AreEqual(model.PointFArray[i].X, result.PointFArray[i].X, 0.0001f);
                Assert.AreEqual(model.PointFArray[i].Y, result.PointFArray[i].Y, 0.0001f);
            }

            Assert.AreEqual(model.SizeArray.Length, result.SizeArray.Length);
            for (int i = 0; i < model.SizeArray.Length; i++)
            {
                Assert.AreEqual(model.SizeArray[i], result.SizeArray[i]);
            }

            Assert.AreEqual(model.SizeFArray.Length, result.SizeFArray.Length);
            for (int i = 0; i < model.SizeFArray.Length; i++)
            {
                Assert.AreEqual(model.SizeFArray[i].Width, result.SizeFArray[i].Width, 0.0001f);
                Assert.AreEqual(model.SizeFArray[i].Height, result.SizeFArray[i].Height, 0.0001f);
            }

            Assert.AreEqual(model.ColorArray.Length, result.ColorArray.Length);
            for (int i = 0; i < model.ColorArray.Length; i++)
            {
                Assert.AreEqual(model.ColorArray[i].ToArgb(), result.ColorArray[i].ToArgb());
            }
        }

        [TestMethod]
        public void TestStructLists()
        {
            var model = new StructCollectionsModel
            {
                PointList = new List<Point> { new Point(1, 2), new Point(3, 4) },
                PointFList = new List<PointF> { new PointF(1.5f, 2.5f) },
                SizeList = new List<Size> { new Size(100, 200) },
                SizeFList = new List<SizeF> { new SizeF(100.5f, 200.5f) },
                ColorList = new List<Color> { Color.Yellow, Color.Cyan },
                ByteArrayList = new List<byte[]> { new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 } }
            };

            var result = Roundtrip(model);

            Assert.IsNotNull(result);
            TestAssertHelper.AssertListEqual(model.PointList, result.PointList);
            TestAssertHelper.AssertListEqual(model.SizeList, result.SizeList);
            TestAssertHelper.AssertListEqual(model.ColorList.Select(c => c.ToArgb()).ToList(), result.ColorList.Select(c => c.ToArgb()).ToList());

            Assert.AreEqual(model.ByteArrayList.Count, result.ByteArrayList.Count);
            for (int i = 0; i < model.ByteArrayList.Count; i++)
            {
                TestAssertHelper.AssertSequenceEqual(model.ByteArrayList[i], result.ByteArrayList[i]);
            }
        }

        [TestMethod]
        public void TestListWithNullElements()
        {
            var model = new StructCollectionsModel
            {
                ByteArrayList = new List<byte[]>
                {
                    new byte[] { 1, 2, 3 },
                    null,
                    new byte[] { 4, 5 }
                }
            };

            var result = Roundtrip(model);

            Assert.IsNotNull(result.ByteArrayList);
            Assert.AreEqual(3, result.ByteArrayList.Count);
            TestAssertHelper.AssertSequenceEqual(model.ByteArrayList[0], result.ByteArrayList[0]);
            Assert.IsNull(result.ByteArrayList[1]);
            TestAssertHelper.AssertSequenceEqual(model.ByteArrayList[2], result.ByteArrayList[2]);
        }
    }
}
