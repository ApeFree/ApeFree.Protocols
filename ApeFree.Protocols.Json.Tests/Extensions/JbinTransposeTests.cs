using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.Jbin.Attributes;
using ApeFree.Protocols.Json.Jbin.Extensions;
using ApeFree.Protocols.Json.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests.Extensions
{
    public class SampleDefectItem
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public float Area { get; set; }
        public bool IsFatal { get; set; }

        [JbinIgnore]
        public string TempDebugInfo { get; set; }

        [JsonIgnore]
        public string InternalToken { get; set; }
    }

    [TestClass]
    public class JbinTransposeTests
    {
        [TestMethod]
        public void TestTransposeToAndFromDictionary()
        {
            var records = new List<DefectRecord>
            {
                new DefectRecord { Id = 1, DefectType = "Scratch", Area = 12.34, Score = 0.95f, IsValid = true, Position = new Point(10, 20) },
                new DefectRecord { Id = 2, DefectType = "Dust", Area = 5.67, Score = 0.88f, IsValid = false, Position = new Point(30, 40) },
                new DefectRecord { Id = 3, DefectType = "Scratch", Area = 20.11, Score = 0.99f, IsValid = true, Position = new Point(50, 60) }
            };

            // 1. 行转列
            var dict = records.TransposeToDictionary();
            Assert.IsTrue(dict.ContainsKey("Id"));
            Assert.IsTrue(dict.ContainsKey("DefectType"));
            Assert.IsTrue(dict.ContainsKey("Area"));
            Assert.IsTrue(dict.ContainsKey("Score"));
            Assert.IsTrue(dict.ContainsKey("IsValid"));
            Assert.IsTrue(dict.ContainsKey("Position"));

            // 2. 列转行
            var restored = dict.TransposeFromDictionary<DefectRecord>();
            Assert.IsNotNull(restored);
            Assert.AreEqual(records.Count, restored.Length);

            for (int i = 0; i < records.Count; i++)
            {
                Assert.AreEqual(records[i].Id, restored[i].Id);
                Assert.AreEqual(records[i].DefectType, restored[i].DefectType);
                Assert.AreEqual(records[i].Area, restored[i].Area);
                Assert.AreEqual(records[i].Score, restored[i].Score);
                Assert.AreEqual(records[i].IsValid, restored[i].IsValid);
                Assert.AreEqual(records[i].Position, restored[i].Position);
            }
        }

        [TestMethod]
        public void TestTranspose_WithPropertyFilter()
        {
            var records = new List<DefectRecord>
            {
                new DefectRecord { Id = 1, DefectType = "Bubble", Area = 1.0, Score = 0.5f, IsValid = true, Position = new Point(1, 1) },
                new DefectRecord { Id = 2, DefectType = "Crack", Area = 2.0, Score = 0.6f, IsValid = false, Position = new Point(2, 2) }
            };

            // 仅保留数值型属性
            var dict = records.TransposeToDictionary(p => p.Name == "Id" || p.Name == "Area");
            Assert.AreEqual(2, dict.Count);
            Assert.IsTrue(dict.ContainsKey("Id"));
            Assert.IsTrue(dict.ContainsKey("Area"));
            Assert.IsFalse(dict.ContainsKey("DefectType"));

            var restored = dict.TransposeFromDictionary<DefectRecord>(p => p.Name == "Id" || p.Name == "Area");
            Assert.AreEqual(2, restored.Length);
            Assert.AreEqual(1, restored[0].Id);
            Assert.AreEqual(1.0, restored[0].Area);
            Assert.IsNull(restored[0].DefectType);
        }

        [TestMethod]
        public void TestTranspose_IgnoreAttributes()
        {
            var items = new List<SampleDefectItem>
            {
                new SampleDefectItem { Id = 10, Code = "A", X = 1.0, Y = 2.0, Area = 3.0f, IsFatal = true, TempDebugInfo = "DEBUG", InternalToken = "TOKEN" }
            };

            var dict = items.TransposeToDictionary();
            Assert.IsTrue(dict.ContainsKey("Id"));
            Assert.IsTrue(dict.ContainsKey("Code"));
            Assert.IsFalse(dict.ContainsKey("TempDebugInfo"), "JbinIgnoreAttribute should filter this property");
            Assert.IsFalse(dict.ContainsKey("InternalToken"), "JsonIgnoreAttribute should filter this property");

            var restored = dict.TransposeFromDictionary<SampleDefectItem>();
            Assert.AreEqual(1, restored.Length);
            Assert.AreEqual(10, restored[0].Id);
            Assert.IsNull(restored[0].TempDebugInfo);
            Assert.IsNull(restored[0].InternalToken);
        }

        [TestMethod]
        public void TestMultiChannelGroup_TransposeRoundtrip()
        {
            var channels = new Dictionary<string, List<SampleDefectItem>>
            {
                ["CH_A"] = new List<SampleDefectItem>
                {
                    new SampleDefectItem { Id = 1, Code = "A1", X = 1.1, Y = 2.2, Area = 5.0f },
                    new SampleDefectItem { Id = 2, Code = "A2", X = 3.3, Y = 4.4, Area = 6.0f }
                },
                ["CH_B"] = new List<SampleDefectItem>
                {
                    new SampleDefectItem { Id = 3, Code = "B1", X = 5.5, Y = 6.6, Area = 7.0f }
                }
            };

            // 转置
            var transposed = channels.Transpose();
            Assert.IsNotNull(transposed);
            Assert.IsTrue(transposed.ContainsKey("CH_A"));
            Assert.IsTrue(transposed.ContainsKey("CH_B"));
            Assert.AreEqual(2, ((int[])transposed["CH_A"]["Id"]).Length);
            Assert.AreEqual(1, ((int[])transposed["CH_B"]["Id"]).Length);

            // 逆转置
            var restored = transposed.Transpose<string, SampleDefectItem>();
            Assert.IsNotNull(restored);
            Assert.AreEqual(2, restored["CH_A"].Count);
            Assert.AreEqual(1, restored["CH_B"].Count);
            Assert.AreEqual(1, restored["CH_A"][0].Id);
            Assert.AreEqual("A1", restored["CH_A"][0].Code);
            Assert.AreEqual(3, restored["CH_B"][0].Id);
            Assert.AreEqual("B1", restored["CH_B"][0].Code);
        }

        [TestMethod]
        public void TestDynamicDictionary_TransposeRoundtrip()
        {
            var list = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["ColA"] = 100, ["ColB"] = "Text1", ["ColC"] = true },
                new Dictionary<string, object> { ["ColA"] = 200, ["ColB"] = "Text2" },
                null,
                new Dictionary<string, object> { ["ColA"] = 400, ["ColC"] = false }
            };

            var colDict = list.TransposeDictionariesToDictionary();
            Assert.IsNotNull(colDict);
            Assert.AreEqual(4, colDict["ColA"].Length);
            Assert.AreEqual(100, colDict["ColA"][0]);
            Assert.AreEqual(200, colDict["ColA"][1]);
            Assert.IsNull(colDict["ColA"][2]);
            Assert.AreEqual(400, colDict["ColA"][3]);

            var restored = colDict.TransposeDictionariesFromDictionary(ignoreNullValues: true);
            Assert.IsNotNull(restored);
            Assert.AreEqual(4, restored.Count);
            Assert.AreEqual(100, restored[0]["ColA"]);
            Assert.AreEqual("Text1", restored[0]["ColB"]);
            Assert.AreEqual(true, restored[0]["ColC"]);

            Assert.AreEqual(200, restored[1]["ColA"]);
            Assert.AreEqual("Text2", restored[1]["ColB"]);
            Assert.IsFalse(restored[1].ContainsKey("ColC"));
        }

        [TestMethod]
        public void TestTranspose_PerformanceWith50kRecords()
        {
            const int count = 50000;
            var records = Enumerable.Range(1, count).Select(i => new DefectRecord
            {
                Id = i,
                DefectType = i % 2 == 0 ? "TypeA" : "TypeB",
                Area = i * 1.5,
                Score = i * 0.1f,
                IsValid = i % 3 == 0,
                Position = new Point(i, i * 2)
            }).ToList();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var dict = records.TransposeToDictionary();
            sw.Stop();
            Assert.IsTrue(sw.ElapsedMilliseconds < 500, $"TransposeToDictionary should take < 500ms, took {sw.ElapsedMilliseconds}ms");

            sw.Restart();
            var restored = dict.TransposeFromDictionary<DefectRecord>();
            sw.Stop();
            Assert.IsTrue(sw.ElapsedMilliseconds < 500, $"TransposeFromDictionary should take < 500ms, took {sw.ElapsedMilliseconds}ms");

            Assert.AreEqual(count, restored.Length);
            Assert.AreEqual(records[1000].Id, restored[1000].Id);
            Assert.AreEqual(records[1000].Position, restored[1000].Position);
        }

        [TestMethod]
        public void TestTranspose_JbinSerializationRoundtrip()
        {
            var records = Enumerable.Range(1, 500).Select(i => new DefectRecord
            {
                Id = i,
                DefectType = i % 2 == 0 ? "TypeA" : "TypeB",
                Area = i * 1.5,
                Score = i * 0.1f,
                IsValid = i % 3 == 0,
                Position = new Point(i, i * 2)
            }).ToList();

            var dict = records.TransposeToDictionary();
            var jbin = JbinObject.FromObject(dict);
            var bytes = jbin.ToBytes();

            var parsedJbin = JbinObject.Parse(bytes);
            var parsedDict = parsedJbin.ToObject<Dictionary<string, System.Array>>();
            var restored = parsedDict.TransposeFromDictionary<DefectRecord>();

            Assert.AreEqual(records.Count, restored.Length);
            Assert.AreEqual(records[100].Id, restored[100].Id);
            Assert.AreEqual(records[100].DefectType, restored[100].DefectType);
            Assert.AreEqual(records[100].Area, restored[100].Area);
            Assert.AreEqual(records[100].Position, restored[100].Position);
        }
    }
}
