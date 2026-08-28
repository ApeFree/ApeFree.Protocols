using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests.Converters
{
    public class MultiModeSensorData
    {
        public string SensorId { get; set; }
        public int[] Timestamps { get; set; }
        public float[] Voltages { get; set; }
        public double[] Temperatures { get; set; }
        public short[] RawCounts { get; set; }
        public long[] MonotonicCounters { get; set; }
        public string[] ErrorLogs { get; set; }
    }

    [TestClass]
    public class JbinMultiModeSerializationTests
    {
        [TestMethod]
        public void TestInt32Array_Mode0_Raw_vs_Mode1_FrameOfReference()
        {
            // 构造局部平滑波动的整数数组（适合 FoR 算法）
            int count = 5000;
            var data = new int[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = 10000 + (i % 20); // 范围集中在 10000~10019
            }

            // 1. Mode 0: Raw 序列化
            var settingsRaw = JbinObject.JsonSerializerSettings;
            var primitiveConverterRaw = settingsRaw.Converters.OfType<JbinPrimitiveArrayConverter>().First();
            primitiveConverterRaw.Int32Mode = Int32ArrayCompressMode.Raw;

            var jbinRaw = JbinObject.FromObject(data, settingsRaw);
            var bytesRaw = jbinRaw.ToBytes();

            // 2. Mode 1: FrameOfReference 序列化
            var settingsFoR = JbinObject.JsonSerializerSettings;
            var primitiveConverterFoR = settingsFoR.Converters.OfType<JbinPrimitiveArrayConverter>().First();
            primitiveConverterFoR.Int32Mode = Int32ArrayCompressMode.FrameOfReference;

            var jbinFoR = JbinObject.FromObject(data, settingsFoR);
            var bytesFoR = jbinFoR.ToBytes();

            // 验证 FoR 模式大幅压缩了体积
            Assert.IsTrue(bytesFoR.Length < bytesRaw.Length / 2, $"FoR 字节大小 ({bytesFoR.Length}) 应远小于 Raw 字节大小 ({bytesRaw.Length})");

            // 3. 自适应反序列化测试（使用没有任何配置的默认 settings 进行反序列化）
            var parsedJbin = JbinObject.Parse(bytesFoR);
            var restored = parsedJbin.ToObject<int[]>();

            Assert.IsNotNull(restored);
            Assert.AreEqual(count, restored.Length);
            CollectionAssert.AreEqual(data, restored);
        }

        [TestMethod]
        public void TestSingleArray_Mode0_Raw_vs_Mode1_Gorilla()
        {
            // 构造浮点时序传感器数据（典型阶梯波动，适合 Gorilla 算法）
            int count = 5000;
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = (float)Math.Round(25.0 + (i / 50) * 0.1, 2);
            }

            // Mode 0: Raw
            var settingsRaw = JbinObject.JsonSerializerSettings;
            var jbinRaw = JbinObject.FromObject(data, settingsRaw);
            var bytesRaw = jbinRaw.ToBytes();

            // Mode 1: Gorilla
            var settingsGorilla = JbinObject.JsonSerializerSettings;
            var converter = settingsGorilla.Converters.OfType<JbinPrimitiveArrayConverter>().First();
            converter.SingleMode = SingleArrayCompressMode.Gorilla;

            var jbinGorilla = JbinObject.FromObject(data, settingsGorilla);
            var bytesGorilla = jbinGorilla.ToBytes();

            // 验证 Gorilla 显著压缩体积
            Assert.IsTrue(bytesGorilla.Length < bytesRaw.Length / 2, $"Gorilla 字节大小 ({bytesGorilla.Length}) 应远小于 Raw 字节大小 ({bytesRaw.Length})");

            // 自适应反序列化
            var parsedJbin = JbinObject.Parse(bytesGorilla);
            var restored = parsedJbin.ToObject<float[]>();

            Assert.IsNotNull(restored);
            Assert.AreEqual(count, restored.Length);
            TestAssertHelper.AssertFloatSequenceEqual(data, restored, 0.0001f);
        }

        [TestMethod]
        public void TestDoubleArray_Mode0_Raw_vs_Mode1_Gorilla()
        {
            int count = 5000;
            var data = new double[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = Math.Round(25.0 + (i / 50) * 0.05, 2); // 模拟温度阶梯波动
            }

            // Mode 0: Raw
            var settingsRaw = JbinObject.JsonSerializerSettings;
            var jbinRaw = JbinObject.FromObject(data, settingsRaw);
            var bytesRaw = jbinRaw.ToBytes();

            // Mode 1: Gorilla
            var settingsGorilla = JbinObject.JsonSerializerSettings;
            var converter = settingsGorilla.Converters.OfType<JbinPrimitiveArrayConverter>().First();
            converter.DoubleMode = DoubleArrayCompressMode.Gorilla;

            var jbinGorilla = JbinObject.FromObject(data, settingsGorilla);
            var bytesGorilla = jbinGorilla.ToBytes();

            Assert.IsTrue(bytesGorilla.Length < bytesRaw.Length / 2, $"Gorilla 压缩后大小 ({bytesGorilla.Length}) 应远小于 Raw ({bytesRaw.Length})");

            // 自适应反序列化
            var parsedJbin = JbinObject.Parse(bytesGorilla);
            var restored = parsedJbin.ToObject<double[]>();

            Assert.IsNotNull(restored);
            Assert.AreEqual(count, restored.Length);
            TestAssertHelper.AssertDoubleSequenceEqual(data, restored, 0.0000001);
        }

        [TestMethod]
        public void TestInt64Array_Mode0_Raw_vs_Mode1_Simple8b()
        {
            int count = 2000;
            var data = new long[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = i * 2; // 单调小整数
            }

            // Mode 1: Simple8b
            var settings = JbinObject.JsonSerializerSettings;
            var converter = settings.Converters.OfType<JbinPrimitiveArrayConverter>().First();
            converter.Int64Mode = Int64ArrayCompressMode.Simple8b;

            var jbin = JbinObject.FromObject(data, settings);
            var bytes = jbin.ToBytes();

            // 自适应反序列化
            var parsedJbin = JbinObject.Parse(bytes);
            var restored = parsedJbin.ToObject<long[]>();

            Assert.IsNotNull(restored);
            Assert.AreEqual(count, restored.Length);
            CollectionAssert.AreEqual(data, restored);
        }

        [TestMethod]
        public void TestInt16Array_Mode0_Raw_vs_Mode1_DeltaBitPacking()
        {
            int count = 2000;
            var data = new short[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = (short)(500 + (i % 10));
            }

            // Mode 1: DeltaBitPacking
            var settings = JbinObject.JsonSerializerSettings;
            var converter = settings.Converters.OfType<JbinPrimitiveArrayConverter>().First();
            converter.Int16Mode = Int16ArrayCompressMode.DeltaBitPacking;

            var jbin = JbinObject.FromObject(data, settings);
            var bytes = jbin.ToBytes();

            // 自适应反序列化
            var parsedJbin = JbinObject.Parse(bytes);
            var restored = parsedJbin.ToObject<short[]>();

            Assert.IsNotNull(restored);
            Assert.AreEqual(count, restored.Length);
            CollectionAssert.AreEqual(data, restored);
        }

        [TestMethod]
        public void TestStringArray_Mode0_Dictionary_vs_Mode1_Deflate()
        {
            // 长文本日志数组
            var logs = new string[]
            {
                "System initialized successfully at startup step 1 with all drivers loaded.",
                "High precision laser sensor calibrated with offset (0.0012, -0.0034).",
                "Wafer inspection started for Lot ID LOT_2026_08_28_A, Slot 12.",
                "Warning: Minor illumination jitter detected on optical channel B.",
                "System initialized successfully at startup step 1 with all drivers loaded." // 重复项
            };

            // Mode 1: Deflate
            var settingsDeflate = JbinObject.JsonSerializerSettings;
            var strConverter = settingsDeflate.Converters.OfType<JbinStringDictArrayConverter>().First();
            strConverter.StringArrayMode = StringArrayCompressMode.Deflate;

            var jbinDeflate = JbinObject.FromObject(logs, settingsDeflate);
            var bytesDeflate = jbinDeflate.ToBytes();

            // 自适应反序列化
            var parsedJbin = JbinObject.Parse(bytesDeflate);
            var restored = parsedJbin.ToObject<string[]>();

            Assert.IsNotNull(restored);
            Assert.AreEqual(logs.Length, restored.Length);
            CollectionAssert.AreEqual(logs, restored);
        }

        [TestMethod]
        public void TestComplexModel_MultiModeAdaptiveRoundtrip()
        {
            int n = 1000;
            var model = new MultiModeSensorData
            {
                SensorId = "SENSOR_CHAMBER_01",
                Timestamps = Enumerable.Range(0, n).Select(i => 1000 + i % 15).ToArray(),
                Voltages = Enumerable.Range(0, n).Select(i => (float)Math.Sin(i * 0.1) * 5f).ToArray(),
                Temperatures = Enumerable.Range(0, n).Select(i => 300.0 + Math.Cos(i * 0.05) * 1.5).ToArray(),
                RawCounts = Enumerable.Range(0, n).Select(i => (short)(i % 50)).ToArray(),
                MonotonicCounters = Enumerable.Range(0, n).Select(i => (long)i * 10).ToArray(),
                ErrorLogs = new string[] { "OK", "WARN_01", "OK", "ERROR_RESET", "OK" }
            };

            // 针对每个字段分别开启各自的高效压缩模式
            var settings = JbinObject.JsonSerializerSettings;
            var primitiveConverter = settings.Converters.OfType<JbinPrimitiveArrayConverter>().First();
            primitiveConverter.Int32Mode = Int32ArrayCompressMode.FrameOfReference;
            primitiveConverter.SingleMode = SingleArrayCompressMode.Gorilla;
            primitiveConverter.DoubleMode = DoubleArrayCompressMode.Gorilla;
            primitiveConverter.Int64Mode = Int64ArrayCompressMode.Simple8b;
            primitiveConverter.Int16Mode = Int16ArrayCompressMode.DeltaBitPacking;

            var strConverter = settings.Converters.OfType<JbinStringDictArrayConverter>().First();
            strConverter.StringArrayMode = StringArrayCompressMode.Deflate;

            // 序列化为压缩格式 Jbin
            var jbin = JbinObject.FromObject(model, settings);
            var bytes = jbin.ToBytes();

            // 使用未经过任何配置的默认 settings 反序列化
            var parsedJbin = JbinObject.Parse(bytes);
            var restored = parsedJbin.ToObject<MultiModeSensorData>();

            Assert.IsNotNull(restored);
            Assert.AreEqual(model.SensorId, restored.SensorId);
            CollectionAssert.AreEqual(model.Timestamps, restored.Timestamps);
            TestAssertHelper.AssertFloatSequenceEqual(model.Voltages, restored.Voltages, 0.0001f);
            TestAssertHelper.AssertDoubleSequenceEqual(model.Temperatures, restored.Temperatures, 0.00001);
            CollectionAssert.AreEqual(model.RawCounts, restored.RawCounts);
            CollectionAssert.AreEqual(model.MonotonicCounters, restored.MonotonicCounters);
            CollectionAssert.AreEqual(model.ErrorLogs, restored.ErrorLogs);
        }
    }
}
