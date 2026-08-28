using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests.Converters
{
    [TestClass]
    public class JbinBytesConverterTests
    {
        private T Roundtrip<T>(T obj)
        {
            var jbin = JbinObject.FromObject(obj);
            var bytes = jbin.ToBytes();
            var parsed = JbinObject.Parse(bytes);
            return parsed.ToObject<T>();
        }

        [TestMethod]
        public void TestSingleByteArray_EmptyAndValues()
        {
            var model = new BytesContainerModel
            {
                SingleBytes = new byte[] { 0x00, 0x12, 0x34, 0xAB, 0xCD, 0xEF, 0xFF }
            };

            var result = Roundtrip(model);

            Assert.IsNotNull(result);
            TestAssertHelper.AssertSequenceEqual(model.SingleBytes, result.SingleBytes);
        }

        [TestMethod]
        public void TestSingleByteArray_LargePayload()
        {
            var largeBytes = new byte[1024 * 1024 * 2]; // 2MB
            for (int i = 0; i < largeBytes.Length; i++)
            {
                largeBytes[i] = (byte)(i % 256);
            }

            var model = new BytesContainerModel { SingleBytes = largeBytes };
            var result = Roundtrip(model);

            Assert.IsNotNull(result.SingleBytes);
            Assert.AreEqual(largeBytes.Length, result.SingleBytes.Length);
            TestAssertHelper.AssertSequenceEqual(largeBytes, result.SingleBytes);
        }

        [TestMethod]
        public void TestJaggedByteArray_MultipleBlocks()
        {
            var model = new BytesContainerModel
            {
                JaggedBytes = new byte[][]
                {
                    new byte[] { 1, 2, 3 },
                    new byte[0],
                    new byte[] { 10, 20, 30, 40, 50 },
                    Enumerable.Range(0, 1000).Select(x => (byte)(x % 256)).ToArray()
                }
            };

            var result = Roundtrip(model);

            Assert.IsNotNull(result.JaggedBytes);
            Assert.AreEqual(model.JaggedBytes.Length, result.JaggedBytes.Length);
            for (int i = 0; i < model.JaggedBytes.Length; i++)
            {
                TestAssertHelper.AssertSequenceEqual(model.JaggedBytes[i], result.JaggedBytes[i], $"Index: {i}");
            }
        }

        [TestMethod]
        public void TestByteArrayList_MultipleBlocks()
        {
            var model = new BytesContainerModel
            {
                ByteList = new List<byte[]>
                {
                    new byte[] { 0xAA, 0xBB },
                    new byte[] { 0xCC, 0xDD, 0xEE },
                    new byte[0]
                }
            };

            var result = Roundtrip(model);

            Assert.IsNotNull(result.ByteList);
            Assert.AreEqual(model.ByteList.Count, result.ByteList.Count);
            for (int i = 0; i < model.ByteList.Count; i++)
            {
                TestAssertHelper.AssertSequenceEqual(model.ByteList[i], result.ByteList[i], $"Index: {i}");
            }
        }

        [TestMethod]
        public void TestNullBytesProperties()
        {
            var model = new BytesContainerModel
            {
                SingleBytes = null,
                JaggedBytes = null,
                ByteList = null
            };

            var result = Roundtrip(model);

            Assert.IsNotNull(result);
            Assert.IsNull(result.SingleBytes);
            Assert.IsNull(result.JaggedBytes);
            Assert.IsNull(result.ByteList);
        }
    }
}
