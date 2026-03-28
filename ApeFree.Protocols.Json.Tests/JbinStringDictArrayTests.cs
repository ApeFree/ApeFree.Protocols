using ApeFree.Protocols.Json.Jbin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests
{
    /// <summary>
    /// JbinStringDictArrayConverter 隔离测试：验证字符串数组的字典压缩方案的正确性。
    /// </summary>
    [TestClass]
    public class JbinStringDictArrayTests
    {
        #region 辅助

        private static T RoundTrip<T>(T original)
        {
            var jbin = JbinObject.FromObject(original);
            var bytes = jbin.ToBytes();
            return JbinObject.Parse(bytes).ToObject<T>();
        }

        public class StringArrayModel { public string[] Data { get; set; } }

        #endregion

        [TestMethod]
        public void RoundTrip_UniqueStrings_AllPreserved()
        {
            var original = new StringArrayModel
            {
                Data = new[] { "alpha", "beta", "gamma", "delta" }
            };

            var result = RoundTrip(original);
            CollectionAssert.AreEqual(original.Data, result.Data);
        }

        [TestMethod]
        public void RoundTrip_DuplicateStrings_AllPreserved()
        {
            var original = new StringArrayModel
            {
                Data = new[] { "apple", "banana", "apple", "cherry", "banana", "apple" }
            };

            var result = RoundTrip(original);
            CollectionAssert.AreEqual(original.Data, result.Data);
        }

        [TestMethod]
        public void RoundTrip_EmptyStrings_Preserved()
        {
            var original = new StringArrayModel
            {
                Data = new[] { "", "hello", "", "world", "" }
            };

            var result = RoundTrip(original);
            CollectionAssert.AreEqual(original.Data, result.Data);
        }

        [TestMethod]
        public void RoundTrip_SingleElement()
        {
            var original = new StringArrayModel { Data = new[] { "solo" } };
            var result = RoundTrip(original);

            Assert.AreEqual(1, result.Data.Length);
            Assert.AreEqual("solo", result.Data[0]);
        }

        [TestMethod]
        public void RoundTrip_ChineseStrings()
        {
            var original = new StringArrayModel
            {
                Data = new[] { "你好", "世界", "测试", "你好" }
            };

            var result = RoundTrip(original);
            CollectionAssert.AreEqual(original.Data, result.Data);
        }

        [TestMethod]
        public void RoundTrip_LongRepeatedStrings()
        {
            // 构造含有大量重复长字符串的数组
            var longStr = new string('x', 1000);
            var original = new StringArrayModel
            {
                Data = Enumerable.Repeat(longStr, 100).ToArray()
            };

            var result = RoundTrip(original);
            CollectionAssert.AreEqual(original.Data, result.Data);
        }

        [TestMethod]
        public void Compression_DuplicatesSmaller()
        {
            // 大量重复字符串时，Jbin 应比纯 JSON 更紧凑
            var longStr = new string('A', 500);
            var original = new StringArrayModel
            {
                Data = Enumerable.Repeat(longStr, 50).ToArray()
            };

            var jbin = JbinObject.FromObject(original);
            var jbinBytes = jbin.ToBytes();

            // 纯 JSON 中每个元素都会完整存储字符串
            var jsonSize = Newtonsoft.Json.JsonConvert.SerializeObject(original).Length;

            Assert.IsTrue(jbinBytes.Length < jsonSize,
                $"Jbin ({jbinBytes.Length} bytes) 应小于纯 JSON ({jsonSize} chars) —— 字典压缩应发挥作用");
        }
    }
}
