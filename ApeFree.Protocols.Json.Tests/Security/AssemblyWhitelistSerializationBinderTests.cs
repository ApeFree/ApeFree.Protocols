using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.Jbin.Binders;
using ApeFree.Protocols.Json.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Drawing;

namespace ApeFree.Protocols.Json.Tests.Security
{
    [TestClass]
    public class AssemblyWhitelistSerializationBinderTests
    {
        [TestMethod]
        public void TestWhitelistBinder_AllowedAssembly_Success()
        {
            var binder = new AssemblyWhitelistSerializationBinder(typeof(Dog), typeof(Cat), typeof(Point));

            var settings = JbinObject.JsonSerializerSettings;
            settings.SerializationBinder = binder;

            var zoo = new ZooModel
            {
                ZooName = "SafeZoo",
                LeadAnimal = new Dog { Name = "SafeDog", BarkVolume = 90, BarkVoiceData = new byte[] { 1, 2, 3 } }
            };

            var jbin = JbinObject.FromObject(zoo, settings);
            var bytes = jbin.ToBytes();

            var parsed = JbinObject.Parse(bytes);
            var result = parsed.ToObject<ZooModel>(settings);

            Assert.IsNotNull(result);
            Assert.AreEqual("SafeZoo", result.ZooName);
            Assert.IsInstanceOfType(result.LeadAnimal, typeof(Dog));
            Assert.AreEqual("SafeDog", result.LeadAnimal.Name);
        }

        [TestMethod]
        public void TestWhitelistBinder_DisallowedAssembly_ThrowsException()
        {
            // 仅允许 System.Drawing，不包含当前测试程序集
            var binder = new AssemblyWhitelistSerializationBinder("System.Drawing");

            var settings = JbinObject.JsonSerializerSettings;
            settings.SerializationBinder = binder;

            var zoo = new ZooModel
            {
                ZooName = "BlockedZoo",
                LeadAnimal = new Dog { Name = "BlockedDog" }
            };

            // 序列化成功（写入带 $type 的 JSON）
            var jbin = JbinObject.FromObject(zoo);
            var bytes = jbin.ToBytes();

            var parsed = JbinObject.Parse(bytes);

            // 反序列化时，binder 会拦截非白名单程序集中的类型并抛出 JsonSerializationException
            Assert.ThrowsException<JsonSerializationException>(() =>
            {
                parsed.ToObject<ZooModel>(settings);
            });
        }
    }
}
