using ApeFree.Protocols.Json.Jbin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests
{
    /// <summary>
    /// JbinPrimitiveArrayConverter 隔离测试：验证各类基元类型数组通过 Buffer.BlockCopy 的往返一致性。
    /// </summary>
    [TestClass]
    public class JbinPrimitiveArrayTests
    {
        #region 辅助

        private static T RoundTrip<T>(T original)
        {
            var jbin = JbinObject.FromObject(original);
            var bytes = jbin.ToBytes();
            return JbinObject.Parse(bytes).ToObject<T>();
        }

        public class IntArrayModel { public int[] Data { get; set; } }
        public class ShortArrayModel { public short[] Data { get; set; } }
        public class FloatArrayModel { public float[] Data { get; set; } }
        public class DoubleArrayModel { public double[] Data { get; set; } }
        public class LongArrayModel { public long[] Data { get; set; } }
        public class BoolArrayModel { public bool[] Data { get; set; } }

        #endregion

        [TestMethod]
        public void RoundTrip_IntArray_ValuesPreserved()
        {
            var original = new IntArrayModel
            {
                Data = new[] { 0, 1, -1, int.MaxValue, int.MinValue, 42 }
            };

            var result = RoundTrip(original);
            CollectionAssert.AreEqual(original.Data, result.Data);
        }

        [TestMethod]
        public void RoundTrip_ShortArray_ValuesPreserved()
        {
            var original = new ShortArrayModel
            {
                Data = new short[] { 0, -1, short.MaxValue, short.MinValue, 100 }
            };

            var result = RoundTrip(original);
            CollectionAssert.AreEqual(original.Data, result.Data);
        }

        [TestMethod]
        public void RoundTrip_FloatArray_ValuesPreserved()
        {
            var original = new FloatArrayModel
            {
                Data = new[] { 0f, 3.14f, -1.5f, float.MaxValue, float.MinValue, float.Epsilon }
            };

            var result = RoundTrip(original);
            CollectionAssert.AreEqual(original.Data, result.Data);
        }

        [TestMethod]
        public void RoundTrip_DoubleArray_ValuesPreserved()
        {
            var original = new DoubleArrayModel
            {
                Data = new[] { 0.0, Math.PI, -2.718, double.MaxValue, double.MinValue }
            };

            var result = RoundTrip(original);
            CollectionAssert.AreEqual(original.Data, result.Data);
        }

        [TestMethod]
        public void RoundTrip_LongArray_ValuesPreserved()
        {
            var original = new LongArrayModel
            {
                Data = new[] { 0L, long.MaxValue, long.MinValue, 123456789012345L, -1L }
            };

            var result = RoundTrip(original);
            CollectionAssert.AreEqual(original.Data, result.Data);
        }

        [TestMethod]
        public void RoundTrip_BoolArray_ValuesPreserved()
        {
            var original = new BoolArrayModel
            {
                Data = new[] { true, false, true, true, false }
            };

            var result = RoundTrip(original);
            CollectionAssert.AreEqual(original.Data, result.Data);
        }

        [TestMethod]
        public void RoundTrip_EmptyIntArray()
        {
            var original = new IntArrayModel { Data = new int[0] };
            var result = RoundTrip(original);

            Assert.IsNotNull(result.Data);
            Assert.AreEqual(0, result.Data.Length);
        }

        [TestMethod]
        public void RoundTrip_SingleElementArray()
        {
            var original = new IntArrayModel { Data = new[] { 42 } };
            var result = RoundTrip(original);

            Assert.AreEqual(1, result.Data.Length);
            Assert.AreEqual(42, result.Data[0]);
        }

        [TestMethod]
        public void CanSerialize_ByteArray_ReturnsFalse()
        {
            // byte[] 应交由 JbinBytesConverter 处理，PrimitiveArrayConverter 应排除它
            var converter = new JbinPrimitiveArrayConverter();
            Assert.IsFalse(converter.CanSerialize(typeof(byte[])));
        }

        [TestMethod]
        public void CanSerialize_IntArray_ReturnsTrue()
        {
            var converter = new JbinPrimitiveArrayConverter();
            Assert.IsTrue(converter.CanSerialize(typeof(int[])));
        }

        [TestMethod]
        public void CanSerialize_StringArray_ReturnsFalse()
        {
            // string 不是基元类型
            var converter = new JbinPrimitiveArrayConverter();
            Assert.IsFalse(converter.CanSerialize(typeof(string[])));
        }
    }
}
