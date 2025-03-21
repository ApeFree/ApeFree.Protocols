using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.Tests.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STTech.CodePlus.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests
{
    public class MyData
    {
        public string Name { get; set; }
        public string Des { get; set; }
        public long Id { get; set; }
        public Point Location { get; set; }
        public SizeF[] Size { get; set; }
        public byte[] Data1 { get; set; }
        public int[] Data2 { get; set; }
        public short[] Data3 { get; set; }
        public Bitmap Photo { get; set; }
        public List<MyData> Child { get; set; }
    }


    [TestClass]
    public class JbinUnitTest
    {
        private static MyData CreateTestObject()
        {
            var obj = new MyData()
            {
                Name = "parent",
                Des = "parent node",
                Location = new Point(100, 200),
                Size = Enumerable.Range(999999, 3).Select(x => new SizeF((float)(x * Math.PI), 9999.99999f)).ToArray(),
                Data1 = Enumerable.Range(0, 1024*64).Select(x => (byte)x).ToArray(),
                Data2 = Enumerable.Range(0, 100).Select(x => (int)x).ToArray(),
                Data3 = Enumerable.Range(0, 100).Select(x => (short)x).ToArray(),
                Child = new List<MyData>(),
                Id = (((long)(999 | (1 << 31))) << 32) | ((long)(888 | (1 << 31))),
            };

            //var childObj = new MyData()
            //{
            //    Name = "child",
            //    Des = "child node",
            //    Location = new Point(50, 500),
            //    Size = Enumerable.Range(999999, 3).Select(x => new SizeF((float)(x * Math.PI), 9999.99999f)).ToArray(),
            //    Data1 = Enumerable.Range(0, 65536).Select(x => (byte)x).ToArray(),
            //    Data2 = Enumerable.Range(0, 65536).Select(x => (int)x).ToArray(),
            //    Data3 = Enumerable.Range(0, 65536).Select(x => (short)x).ToArray(),
            //};

            //obj.Child.Add(childObj);

            return obj;
        }

        [TestInitialize]
        public void InitTestData()
        {
            TestObject = CreateTestObject();
            TestObjectBytes = JbinObject.FromObject(TestObject).ToBytes();
            TestObjectJson = JsonConvert.SerializeObject(TestObject, Formatting.Indented, SerializerSettings);
        }

        public static MyData TestObject;
        public static byte[] TestObjectBytes;
        public static string TestObjectJson;

        public static JsonSerializerSettings SerializerSettings => new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        [TestMethod]
        public void Save()
        {
            var obj = TestObject;
            JbinObject jbin = JbinObject.FromObject(obj);
            var bytes = jbin.ToBytes();
            File.WriteAllBytes("MyData.jbin", bytes);
        }

        [TestMethod]
        public void SavePhoto()
        {
            var bytes = TestObjectBytes;
            var jbin = JbinObject.Parse(bytes);
            jbin.ToObject<MyData>().Photo?.Save("MyPhoto.bmp", ImageFormat.Bmp);
        }

        [TestMethod]
        public void Read()
        {
            var bytes = File.ReadAllBytes("MyData.jbin");

            JbinObject jbin = JbinObject.Parse(bytes);

            var obj = jbin.ToObject<MyData>();
        }

        [TestMethod]
        public void JbinDeserializeTest()
        {
            for (int i = 0; i < 100000; i++)
            {
                var bytes = TestObjectBytes;
                var jbin = JbinObject.Parse(bytes);
                var obj = jbin.ToObject<MyData>();

                obj.Photo?.Dispose();
            }
        }

        [TestMethod]
        public void JbinSerializeTest()
        {
            for (int i = 0; i < 100000; i++)
            {
                var obj = TestObject;
                var jbin = JbinObject.FromObject(obj);
                var jbinBytes = jbin.ToBytes();
            }
        }

        [TestMethod]
        public void JsonDeserializeTest()
        {
            for (int i = 0; i < 100000; i++)
            {
                var jsonStr = TestObjectJson;
                var json = JObject.Parse(jsonStr);
                var obj = json.ToObject<MyData>();

                obj.Photo?.Dispose();
            }
        }

        [TestMethod]
        public void JsonSerializeTest()
        {
            for (int i = 0; i < 100000; i++)
            {
                var obj = TestObject;
                var json = JObject.FromObject(obj);
                var jsonString = json.ToString();
            }
        }
    }
}
