using ApeFree.Protocols.Json.Jbin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace ApeFree.Protocols.Json.Tests
{
    /// <summary>
    /// JbinDeserializer 边界条件测试：验证各种基本类型在反序列化时的正确处理。
    /// 特别关注 Combined ID 识别、数值类型精度和枚举还原。
    /// </summary>
    [TestClass]
    public class JbinDeserializerEdgeCaseTests
    {
        #region 辅助

        private static T RoundTrip<T>(T original)
        {
            var jbin = JbinObject.FromObject(original);
            var bytes = jbin.ToBytes();
            return JbinObject.Parse(bytes).ToObject<T>();
        }

        /// <summary>
        /// 混合类型模型：在一个对象中包含所有基本类型，用于测试反序列化器的全面覆盖。
        /// </summary>
        public class MixedModel
        {
            public int IntProp { get; set; }
            public long LongProp { get; set; }
            public float FloatProp { get; set; }
            public double DoubleProp { get; set; }
            public bool BoolProp { get; set; }
            public string StringProp { get; set; }
            public TaskStatus EnumProp { get; set; }
            public byte ByteProp { get; set; }
            public short ShortProp { get; set; }
            public uint UIntProp { get; set; }
        }

        #endregion

        [TestMethod]
        public void Deserialize_IntProperty_NotMistakenForCombinedId()
        {
            // 普通 int 属性应原样保留，不会被 JbinDeserializer 误认为 Combined ID
            var original = new MixedModel { IntProp = 12345 };
            var result = RoundTrip(original);
            Assert.AreEqual(12345, result.IntProp);
        }

        [TestMethod]
        public void Deserialize_LongProperty_RegularValue_PreservedExactly()
        {
            // 不具备 Combined ID 特征的普通 long 值
            var original = new MixedModel { LongProp = 9876543210L };
            var result = RoundTrip(original);
            Assert.AreEqual(9876543210L, result.LongProp);
        }

        [TestMethod]
        public void Deserialize_LongProperty_HighBitsPattern()
        {
            // 构造一个第 63 位和第 31 位都为 1 的 long 值
            // 这个值恰好与 Combined ID 特征吻合，但它是一个合法的 long 属性
            // Jbin 的设计应保证在声明为 long 的字段时 CanConvert 返回 false
            long tricky = (1L << 63) | (1L << 31) | 42;
            var original = new MixedModel { LongProp = tricky };
            var result = RoundTrip(original);
            Assert.AreEqual(tricky, result.LongProp);
        }

        [TestMethod]
        public void Deserialize_EnumProperty_CorrectValue()
        {
            var original = new MixedModel { EnumProp = TaskStatus.Canceled };
            var result = RoundTrip(original);
            Assert.AreEqual(TaskStatus.Canceled, result.EnumProp);
        }

        [TestMethod]
        public void Deserialize_FloatProperty_Precision()
        {
            var original = new MixedModel { FloatProp = 3.14159f };
            var result = RoundTrip(original);
            Assert.AreEqual(3.14159f, result.FloatProp);
        }

        [TestMethod]
        public void Deserialize_DoubleProperty_Precision()
        {
            var original = new MixedModel { DoubleProp = 2.718281828459045 };
            var result = RoundTrip(original);
            Assert.AreEqual(2.718281828459045, result.DoubleProp);
        }

        [TestMethod]
        public void Deserialize_BoolProperty_True()
        {
            var original = new MixedModel { BoolProp = true };
            var result = RoundTrip(original);
            Assert.IsTrue(result.BoolProp);
        }

        [TestMethod]
        public void Deserialize_BoolProperty_False()
        {
            // DefaultValueHandling.Ignore 会跳过默认值 (false)
            // 所以 false 返回后也是 false（默认值）
            var original = new MixedModel { BoolProp = false };
            var result = RoundTrip(original);
            Assert.IsFalse(result.BoolProp);
        }

        [TestMethod]
        public void Deserialize_StringProperty_ExactMatch()
        {
            var original = new MixedModel { StringProp = "Hello, 世界! 🌍" };
            var result = RoundTrip(original);
            Assert.AreEqual("Hello, 世界! 🌍", result.StringProp);
        }

        [TestMethod]
        public void Deserialize_ByteProperty_Preserved()
        {
            var original = new MixedModel { ByteProp = 255 };
            var result = RoundTrip(original);
            Assert.AreEqual((byte)255, result.ByteProp);
        }

        [TestMethod]
        public void Deserialize_ShortProperty_Preserved()
        {
            var original = new MixedModel { ShortProp = -32000 };
            var result = RoundTrip(original);
            Assert.AreEqual((short)-32000, result.ShortProp);
        }

        [TestMethod]
        public void Deserialize_UIntProperty_Preserved()
        {
            var original = new MixedModel { UIntProp = uint.MaxValue };
            var result = RoundTrip(original);
            Assert.AreEqual(uint.MaxValue, result.UIntProp);
        }

        [TestMethod]
        public void Deserialize_MixedObject_AllTypesCorrect()
        {
            var original = new MixedModel
            {
                IntProp = -42,
                LongProp = long.MaxValue,
                FloatProp = -0.5f,
                DoubleProp = double.Epsilon,
                BoolProp = true,
                StringProp = "test",
                EnumProp = TaskStatus.Running,
                ByteProp = 128,
                ShortProp = 1000,
                UIntProp = 999999,
            };

            var result = RoundTrip(original);

            Assert.AreEqual(original.IntProp, result.IntProp, "IntProp");
            Assert.AreEqual(original.LongProp, result.LongProp, "LongProp");
            Assert.AreEqual(original.FloatProp, result.FloatProp, "FloatProp");
            Assert.AreEqual(original.DoubleProp, result.DoubleProp, "DoubleProp");
            Assert.AreEqual(original.BoolProp, result.BoolProp, "BoolProp");
            Assert.AreEqual(original.StringProp, result.StringProp, "StringProp");
            Assert.AreEqual(original.EnumProp, result.EnumProp, "EnumProp");
            Assert.AreEqual(original.ByteProp, result.ByteProp, "ByteProp");
            Assert.AreEqual(original.ShortProp, result.ShortProp, "ShortProp");
            Assert.AreEqual(original.UIntProp, result.UIntProp, "UIntProp");
        }
    }
}
