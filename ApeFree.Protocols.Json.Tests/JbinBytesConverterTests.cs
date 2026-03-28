using ApeFree.Protocols.Json.Jbin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests
{
    /// <summary>
    /// JbinBytesConverter 隔离测试：验证 byte[] / byte[][] / List&lt;byte[]&gt; 的序列化一致性。
    /// </summary>
    [TestClass]
    public class JbinBytesConverterTests
    {
        #region 辅助

        private static T RoundTrip<T>(T original)
        {
            var jbin = JbinObject.FromObject(original);
            var bytes = jbin.ToBytes();
            return JbinObject.Parse(bytes).ToObject<T>();
        }

        public class ByteArrayModel
        {
            public byte[] Data { get; set; }
        }

        public class ByteArrayArrayModel
        {
            public byte[][] Data { get; set; }
        }

        public class ByteArrayListModel
        {
            public List<byte[]> Data { get; set; }
        }

        #endregion

        [TestMethod]
        public void RoundTrip_SingleByteArray_Exact()
        {
            var original = new ByteArrayModel
            {
                Data = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray()
            };

            var result = RoundTrip(original);

            CollectionAssert.AreEqual(original.Data, result.Data);
        }

        [TestMethod]
        public void RoundTrip_ByteArrayArray_Exact()
        {
            var original = new ByteArrayArrayModel
            {
                Data = new[]
                {
                    new byte[] { 1, 2, 3 },
                    new byte[] { 0xFF, 0xFE },
                    new byte[] { 0 },
                }
            };

            var result = RoundTrip(original);

            Assert.AreEqual(original.Data.Length, result.Data.Length);
            for (int i = 0; i < original.Data.Length; i++)
            {
                CollectionAssert.AreEqual(original.Data[i], result.Data[i], $"Block[{i}]");
            }
        }

        [TestMethod]
        public void RoundTrip_ListOfByteArray_Exact()
        {
            var original = new ByteArrayListModel
            {
                Data = new List<byte[]>
                {
                    new byte[] { 10, 20 },
                    new byte[] { 30, 40, 50 },
                }
            };

            var result = RoundTrip(original);

            Assert.IsNotNull(result.Data);
            Assert.AreEqual(original.Data.Count, result.Data.Count);
            for (int i = 0; i < original.Data.Count; i++)
            {
                CollectionAssert.AreEqual(original.Data[i], result.Data[i], $"List[{i}]");
            }
        }

        [TestMethod]
        public void RoundTrip_EmptyByteArray()
        {
            var original = new ByteArrayModel { Data = new byte[0] };
            var result = RoundTrip(original);

            Assert.IsNotNull(result.Data);
            Assert.AreEqual(0, result.Data.Length);
        }

        [TestMethod]
        public void RoundTrip_LargeByteArray_1MB()
        {
            var rng = new Random(42);
            var data = new byte[1024 * 1024];
            rng.NextBytes(data);

            var original = new ByteArrayModel { Data = data };
            var result = RoundTrip(original);

            CollectionAssert.AreEqual(original.Data, result.Data, "1MB byte[] 往返应精确一致");
        }

        [TestMethod]
        public void CanSerialize_ByteArray_True()
        {
            var converter = new JbinBytesConverter();
            Assert.IsTrue(((IJbinFieldSerializer)converter).CanSerialize(typeof(byte[])));
        }

        [TestMethod]
        public void CanSerialize_IntArray_False()
        {
            var converter = new JbinBytesConverter();
            Assert.IsFalse(((IJbinFieldSerializer)converter).CanSerialize(typeof(int[])));
        }
    }
}
