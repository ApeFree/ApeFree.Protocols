using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests.Converters
{
    [TestClass]
    public class JbinPrimitiveArrayConverterTests
    {
        private T Roundtrip<T>(T obj)
        {
            var jbin = JbinObject.FromObject(obj);
            var bytes = jbin.ToBytes();
            var parsed = JbinObject.Parse(bytes);
            return parsed.ToObject<T>();
        }

        [TestMethod]
        public void TestAllPrimitiveArraysCombined()
        {
            var original = new PrimitiveArraysModel
            {
                BoolArray = new bool[] { true, false, true, true, false },
                SbyteArray = new sbyte[] { sbyte.MinValue, -120, -1, 0, 1, 120, sbyte.MaxValue },
                ShortArray = new short[] { short.MinValue, -1000, 0, 1000, short.MaxValue },
                UshortArray = new ushort[] { ushort.MinValue, 0, 100, 10000, ushort.MaxValue },
                IntArray = new int[] { int.MinValue, -999999, 0, 12345, int.MaxValue },
                UintArray = new uint[] { uint.MinValue, 0, 12345, 999999, uint.MaxValue },
                LongArray = new long[] { long.MinValue, -1L, 0L, 1L, 123456789012345L, long.MaxValue },
                UlongArray = new ulong[] { ulong.MinValue, 0UL, 1UL, 123456789012345UL, ulong.MaxValue },
                FloatArray = new float[] { float.MinValue, -3.14159f, 0f, 2.71828f, float.MaxValue },
                DoubleArray = new double[] { double.MinValue, -3.141592653589793, 0.0, 2.718281828459045, double.MaxValue },
                DecimalArray = new decimal[] { decimal.MinValue, -123.456789m, 0m, 9876.54321m, decimal.MaxValue },
                CharArray = new char[] { 'A', 'z', '0', '中', '文', ' ', '\t', '\0' },
                EnumArray = new SampleStatus[] { SampleStatus.None, SampleStatus.Running, SampleStatus.Completed, SampleStatus.Failed },
                DayOfWeekArray = new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Friday, DayOfWeek.Saturday }
            };

            var result = Roundtrip(original);

            Assert.IsNotNull(result);
            TestAssertHelper.AssertSequenceEqual(original.BoolArray, result.BoolArray);
            TestAssertHelper.AssertSequenceEqual(original.SbyteArray, result.SbyteArray);
            TestAssertHelper.AssertSequenceEqual(original.ShortArray, result.ShortArray);
            TestAssertHelper.AssertSequenceEqual(original.UshortArray, result.UshortArray);
            TestAssertHelper.AssertSequenceEqual(original.IntArray, result.IntArray);
            TestAssertHelper.AssertSequenceEqual(original.UintArray, result.UintArray);
            TestAssertHelper.AssertSequenceEqual(original.LongArray, result.LongArray);
            TestAssertHelper.AssertSequenceEqual(original.UlongArray, result.UlongArray);
            TestAssertHelper.AssertFloatSequenceEqual(original.FloatArray, result.FloatArray);
            TestAssertHelper.AssertDoubleSequenceEqual(original.DoubleArray, result.DoubleArray);
            TestAssertHelper.AssertSequenceEqual(original.DecimalArray, result.DecimalArray);
            TestAssertHelper.AssertSequenceEqual(original.CharArray, result.CharArray);
            TestAssertHelper.AssertSequenceEqual(original.EnumArray, result.EnumArray);
            TestAssertHelper.AssertSequenceEqual(original.DayOfWeekArray, result.DayOfWeekArray);
        }

        [TestMethod]
        public void TestBoolArray_EmptyAndLarge()
        {
            var emptyModel = new SinglePrimitiveArrayModel<bool> { Title = "Empty", Data = new bool[0] };
            var emptyResult = Roundtrip(emptyModel);
            Assert.AreEqual(0, emptyResult.Data.Length);

            var largeData = Enumerable.Range(0, 10000).Select(i => i % 2 == 0).ToArray();
            var largeModel = new SinglePrimitiveArrayModel<bool> { Title = "Large", Data = largeData };
            var largeResult = Roundtrip(largeModel);
            TestAssertHelper.AssertSequenceEqual(largeData, largeResult.Data);
        }

        [TestMethod]
        public void TestIntArray_LargeAndSequential()
        {
            var data = Enumerable.Range(-5000, 10000).ToArray();
            var model = new SinglePrimitiveArrayModel<int> { Title = "10k Ints", Data = data };
            var result = Roundtrip(model);
            TestAssertHelper.AssertSequenceEqual(data, result.Data);
        }

        [TestMethod]
        public void TestDoubleArray_SpecialValues()
        {
            var data = new double[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.Epsilon, -0.0, 0.0, 1e-15, 1e15 };
            var model = new SinglePrimitiveArrayModel<double> { Title = "Special Doubles", Data = data };
            var result = Roundtrip(model);

            Assert.AreEqual(data.Length, result.Data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                if (double.IsNaN(data[i]))
                {
                    Assert.IsTrue(double.IsNaN(result.Data[i]));
                }
                else
                {
                    Assert.AreEqual(data[i], result.Data[i]);
                }
            }
        }

        [TestMethod]
        public void TestFloatArray_SpecialValues()
        {
            var data = new float[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.Epsilon, 0.0f, -123.456f };
            var model = new SinglePrimitiveArrayModel<float> { Title = "Special Floats", Data = data };
            var result = Roundtrip(model);

            Assert.AreEqual(data.Length, result.Data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                if (float.IsNaN(data[i]))
                {
                    Assert.IsTrue(float.IsNaN(result.Data[i]));
                }
                else
                {
                    Assert.AreEqual(data[i], result.Data[i], 0.0001f);
                }
            }
        }

        [TestMethod]
        public void TestLongArray_BitPatternValues()
        {
            // 测试包含 CombinedId 特征位模式的特殊数值（最高位和第31位为1），确保反序列化不会误判崩溃
            long trickyValue1 = (long)1 << 63 | (long)1 << 31 | 12345L;
            long trickyValue2 = (long)1 << 63 | (long)1 << 31;
            var data = new long[] { 0L, -1L, trickyValue1, trickyValue2, long.MinValue, long.MaxValue };
            var model = new SinglePrimitiveArrayModel<long> { Title = "Tricky Longs", Data = data };
            var result = Roundtrip(model);

            TestAssertHelper.AssertSequenceEqual(data, result.Data);
        }

        [TestMethod]
        public void TestPrimitiveArray_NullArrayProperty()
        {
            var model = new SinglePrimitiveArrayModel<int> { Title = "Null Data", Data = null };
            var result = Roundtrip(model);

            Assert.AreEqual("Null Data", result.Title);
            Assert.IsNull(result.Data);
        }

        [TestMethod]
        public void TestGetValueTypeSize_AllSupportedTypes()
        {
            Assert.AreEqual(1, JbinPrimitiveArrayConverter.GetValueTypeSize(typeof(bool)));
            Assert.AreEqual(1, JbinPrimitiveArrayConverter.GetValueTypeSize(typeof(byte)));
            Assert.AreEqual(1, JbinPrimitiveArrayConverter.GetValueTypeSize(typeof(sbyte)));
            Assert.AreEqual(2, JbinPrimitiveArrayConverter.GetValueTypeSize(typeof(char)));
            Assert.AreEqual(2, JbinPrimitiveArrayConverter.GetValueTypeSize(typeof(short)));
            Assert.AreEqual(2, JbinPrimitiveArrayConverter.GetValueTypeSize(typeof(ushort)));
            Assert.AreEqual(4, JbinPrimitiveArrayConverter.GetValueTypeSize(typeof(int)));
            Assert.AreEqual(4, JbinPrimitiveArrayConverter.GetValueTypeSize(typeof(uint)));
            Assert.AreEqual(8, JbinPrimitiveArrayConverter.GetValueTypeSize(typeof(long)));
            Assert.AreEqual(8, JbinPrimitiveArrayConverter.GetValueTypeSize(typeof(ulong)));
            Assert.AreEqual(4, JbinPrimitiveArrayConverter.GetValueTypeSize(typeof(float)));
            Assert.AreEqual(8, JbinPrimitiveArrayConverter.GetValueTypeSize(typeof(double)));
            Assert.AreEqual(16, JbinPrimitiveArrayConverter.GetValueTypeSize(typeof(decimal)));
            Assert.AreEqual(4, JbinPrimitiveArrayConverter.GetValueTypeSize(typeof(SampleStatus)));
        }
    }
}
