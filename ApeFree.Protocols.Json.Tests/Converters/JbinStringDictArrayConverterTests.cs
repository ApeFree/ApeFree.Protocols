using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests.Converters
{
    [TestClass]
    public class JbinStringDictArrayConverterTests
    {
        private T Roundtrip<T>(T obj)
        {
            var jbin = JbinObject.FromObject(obj);
            var bytes = jbin.ToBytes();
            var parsed = JbinObject.Parse(bytes);
            return parsed.ToObject<T>();
        }

        [TestMethod]
        public void TestStringArray_WithHighDuplicates()
        {
            var repeated = new[] { "Critical", "Warning", "Info", "Critical", "Info", "Warning", "Critical", "Critical" };
            var model = new StringDictModel
            {
                Name = "LogTags",
                Tags = repeated,
                Categories = new[] { "UI", "Network", "UI", "Database", "Network" }
            };

            var result = Roundtrip(model);

            Assert.IsNotNull(result);
            Assert.AreEqual("LogTags", result.Name);
            TestAssertHelper.AssertSequenceEqual(model.Tags, result.Tags);
            TestAssertHelper.AssertSequenceEqual(model.Categories, result.Categories);
        }

        [TestMethod]
        public void TestStringArray_WithNullAndEmptyStrings()
        {
            var model = new StringDictModel
            {
                Name = "NullAndEmpty",
                Tags = new string[] { "First", null, string.Empty, "Second", null, string.Empty, "Third" }
            };

            var result = Roundtrip(model);

            Assert.IsNotNull(result);
            TestAssertHelper.AssertSequenceEqual(model.Tags, result.Tags);
        }

        [TestMethod]
        public void TestStringArray_UnicodeAndSpecialCharacters()
        {
            var model = new StringDictModel
            {
                Name = "UnicodeTest",
                Tags = new string[] { "你好，世界", "こんにちは", "Hello World! 🚀", "\t\r\nSpecial\0", "你好，世界" }
            };

            var result = Roundtrip(model);

            Assert.IsNotNull(result);
            TestAssertHelper.AssertSequenceEqual(model.Tags, result.Tags);
        }

        [TestMethod]
        public void TestStringArray_EmptyAndLarge()
        {
            var emptyModel = new StringDictModel { Name = "Empty", Tags = new string[0] };
            var emptyResult = Roundtrip(emptyModel);
            Assert.AreEqual(0, emptyResult.Tags.Length);

            var pool = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon" };
            var largeTags = Enumerable.Range(0, 10000).Select(i => pool[i % pool.Length]).ToArray();
            var largeModel = new StringDictModel { Name = "Large", Tags = largeTags };
            var largeResult = Roundtrip(largeModel);

            TestAssertHelper.AssertSequenceEqual(largeTags, largeResult.Tags);
        }
    }
}
