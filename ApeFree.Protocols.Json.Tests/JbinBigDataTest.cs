using ApeFree.Protocols.Json.Jbin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace ApeFree.Protocols.Json.Tests
{
    [TestClass]
    public class JbinBigDataTest
    {
        [TestMethod]
        public void JbinBigDataSerializeTest()
        {
            var obj = new TestBigData()
            {
                Data1 = new byte[1024 * 1024 * 1024],
                Data2 = new byte[1024 * 1024 * 1024],
            };
            var jbin = JbinObject.FromObject(obj);

            using (var stream = File.OpenWrite("testbigdata.jbin"))
            {
                jbin.WriteTo(stream);
            }
        }

        [TestMethod]
        public void JbinBigDataDeserializeTest()
        {
            using (var stream = File.OpenRead("testbigdata.jbin"))
            {
                var jbin = JbinObject.Parse(stream);
                var obj = jbin.ToObject<TestBigData>();

                Assert.IsNotNull(obj);
                Assert.IsNotNull(obj.Data1);
                Assert.IsNotNull(obj.Data2);
                Assert.AreEqual(1024 * 1024 * 1024, obj.Data1.Length);
                Assert.AreEqual(1024 * 1024 * 1024, obj.Data2.Length);
            }
        }
    }

    public class TestBigData
    {
        public TestBigData()
        {
        }

        public byte[] Data1 { get; set; }
        public byte[] Data2 { get; set; }
    }
}
