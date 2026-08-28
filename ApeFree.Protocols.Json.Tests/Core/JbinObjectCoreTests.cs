using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ApeFree.Protocols.Json.Tests.Core
{
    [TestClass]
    public class JbinObjectCoreTests
    {
        [TestMethod]
        public void TestComplexObject_FullRoundtrip()
        {
            var root = new ComplexJbinModel
            {
                Id = 1001,
                Name = "RootNode",
                Status = TaskStatus.Running,
                Location = new Point(500, 600),
                Sizes = new SizeF[] { new SizeF(10.5f, 20.5f), new SizeF(30.5f, 40.5f) },
                RawData = new byte[] { 0x11, 0x22, 0x33, 0x44 },
                Waveform = Enumerable.Range(0, 100).ToArray(),
                Tags = new string[] { "Alpha", "Beta", "Alpha" },
                Children = new List<ComplexJbinModel>
                {
                    new ComplexJbinModel
                    {
                        Id = 2001,
                        Name = "ChildNode1",
                        Status = TaskStatus.RanToCompletion,
                        Location = new Point(10, 20),
                        RawData = new byte[] { 0xAA, 0xBB }
                    }
                }
            };

            var jbin = JbinObject.FromObject(root);
            var bytes = jbin.ToBytes();

            var parsedJbin = JbinObject.Parse(bytes);
            var result = parsedJbin.ToObject<ComplexJbinModel>();

            Assert.IsNotNull(result);
            Assert.AreEqual(root.Id, result.Id);
            Assert.AreEqual(root.Name, result.Name);
            Assert.AreEqual(root.Status, result.Status);
            Assert.AreEqual(root.Location, result.Location);
            TestAssertHelper.AssertSequenceEqual(root.RawData, result.RawData);
            TestAssertHelper.AssertSequenceEqual(root.Waveform, result.Waveform);
            TestAssertHelper.AssertSequenceEqual(root.Tags, result.Tags);

            Assert.IsNotNull(result.Children);
            Assert.AreEqual(1, result.Children.Count);
            Assert.AreEqual(2001, result.Children[0].Id);
            Assert.AreEqual("ChildNode1", result.Children[0].Name);
            TestAssertHelper.AssertSequenceEqual(root.Children[0].RawData, result.Children[0].RawData);
        }

        [TestMethod]
        public void TestStreamWriteAndParse()
        {
            var model = new ComplexJbinModel
            {
                Id = 42,
                Name = "StreamTest",
                RawData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            };

            var jbin = JbinObject.FromObject(model);

            using (var memoryStream = new MemoryStream())
            {
                jbin.WriteTo(memoryStream);
                memoryStream.Position = 0;

                var parsedJbin = JbinObject.Parse(memoryStream);
                var result = parsedJbin.ToObject<ComplexJbinModel>();

                Assert.IsNotNull(result);
                Assert.AreEqual(42, result.Id);
                Assert.AreEqual("StreamTest", result.Name);
                TestAssertHelper.AssertSequenceEqual(model.RawData, result.RawData);
            }
        }

        [TestMethod]
        public void TestHeaderMetadataAndProperties()
        {
            var model = new ComplexJbinModel
            {
                Id = 99,
                Name = "HeaderCheck",
                Location = new Point(10, 20),
                RawData = new byte[] { 1, 2, 3 }
            };

            var jbin = JbinObject.FromObject(model);

            Assert.IsNotNull(jbin.Header);
            Assert.IsFalse(string.IsNullOrEmpty(jbin.Json));
            Assert.IsTrue(jbin.DataBlocks.Count > 0);
            Assert.IsTrue(jbin.Header.Types.Length > 0);
        }

        [TestMethod]
        public void TestDisposeLifecycle()
        {
            var model = new ComplexJbinModel { Id = 1, Name = "DisposeTest" };
            var jbin = JbinObject.FromObject(model);

            Assert.IsNotNull(jbin.DataBlocks);
            jbin.Dispose();
            Assert.IsNull(jbin.DataBlocks);
        }
    }
}
