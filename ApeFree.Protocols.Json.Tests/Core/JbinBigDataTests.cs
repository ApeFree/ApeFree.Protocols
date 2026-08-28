using ApeFree.Protocols.Json.Jbin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace ApeFree.Protocols.Json.Tests.Core
{
    [TestClass]
    public class JbinBigDataTests
    {
        public class BigDataModel
        {
            public byte[] Data1 { get; set; }
            public int[] Data2 { get; set; }
        }

        [TestMethod]
        public void TestBigData_StreamSerialization()
        {
            var data1Size = 1024 * 1024 * 16; // 16MB
            var data2Count = 1024 * 1024 * 2;  // 2M ints = 8MB

            var obj = new BigDataModel
            {
                Data1 = new byte[data1Size],
                Data2 = new int[data2Count]
            };

            for (int i = 0; i < obj.Data1.Length; i += 1024)
            {
                obj.Data1[i] = (byte)(i % 256);
            }

            for (int i = 0; i < obj.Data2.Length; i += 1024)
            {
                obj.Data2[i] = i;
            }

            var jbin = JbinObject.FromObject(obj);

            using (var ms = new MemoryStream())
            {
                jbin.WriteTo(ms);
                ms.Position = 0;

                var parsedJbin = JbinObject.Parse(ms);
                var result = parsedJbin.ToObject<BigDataModel>();

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Data1);
                Assert.IsNotNull(result.Data2);
                Assert.AreEqual(data1Size, result.Data1.Length);
                Assert.AreEqual(data2Count, result.Data2.Length);

                Assert.AreEqual(obj.Data1[1024], result.Data1[1024]);
                Assert.AreEqual(obj.Data2[1024], result.Data2[1024]);
            }
        }
    }
}
