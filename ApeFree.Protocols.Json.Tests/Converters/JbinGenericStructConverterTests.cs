using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;

namespace ApeFree.Protocols.Json.Tests.Converters
{
    [TestClass]
    public class JbinGenericStructConverterTests
    {
        private T Roundtrip<T>(T obj)
        {
            var jbin = JbinObject.FromObject(obj);
            var bytes = jbin.ToBytes();
            var parsed = JbinObject.Parse(bytes);
            return parsed.ToObject<T>();
        }

        [TestMethod]
        public void TestAllGenericStructs()
        {
            var model = new StructContainerModel
            {
                Point = new Point(-120, 450),
                PointF = new PointF(12.345f, -98.765f),
                Size = new Size(1920, 1080),
                SizeF = new SizeF(3840.5f, 2160.75f),
                Color = Color.FromArgb(200, 50, 100, 150)
            };

            var result = Roundtrip(model);

            Assert.IsNotNull(result);
            Assert.AreEqual(model.Point.X, result.Point.X);
            Assert.AreEqual(model.Point.Y, result.Point.Y);

            Assert.AreEqual(model.PointF.X, result.PointF.X, 0.0001f);
            Assert.AreEqual(model.PointF.Y, result.PointF.Y, 0.0001f);

            Assert.AreEqual(model.Size.Width, result.Size.Width);
            Assert.AreEqual(model.Size.Height, result.Size.Height);

            Assert.AreEqual(model.SizeF.Width, result.SizeF.Width, 0.0001f);
            Assert.AreEqual(model.SizeF.Height, result.SizeF.Height, 0.0001f);

            Assert.AreEqual(model.Color.ToArgb(), result.Color.ToArgb());
            Assert.AreEqual(model.Color.A, result.Color.A);
            Assert.AreEqual(model.Color.R, result.Color.R);
            Assert.AreEqual(model.Color.G, result.Color.G);
            Assert.AreEqual(model.Color.B, result.Color.B);
        }

        [TestMethod]
        public void TestColor_NamedAndTransparent()
        {
            var colors = new[] { Color.Red, Color.Transparent, Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 255, 255, 255) };
            foreach (var c in colors)
            {
                var model = new StructContainerModel { Color = c };
                var result = Roundtrip(model);
                Assert.AreEqual(c.ToArgb(), result.Color.ToArgb());
            }
        }
    }
}
