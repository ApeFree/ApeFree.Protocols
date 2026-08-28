using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Drawing;

namespace ApeFree.Protocols.Json.Tests.Core
{
    [TestClass]
    public class JbinPolymorphismTests
    {
        [TestMethod]
        public void TestPolymorphicCollection()
        {
            var zoo = new ZooModel
            {
                ZooName = "Central Safari",
                LeadAnimal = new Dog
                {
                    Name = "Commander Dog",
                    BarkVolume = 95,
                    BarkVoiceData = new byte[] { 0x12, 0x34, 0x56 }
                },
                Animals = new List<Animal>
                {
                    new Dog
                    {
                        Name = "Buddy",
                        BarkVolume = 80,
                        BarkVoiceData = new byte[] { 0xAA, 0xBB }
                    },
                    new Cat
                    {
                        Name = "Misty",
                        MeowPitch = 1200,
                        FavouriteSpot = new Point(45, 90)
                    }
                }
            };

            var jbin = JbinObject.FromObject(zoo);
            var bytes = jbin.ToBytes();

            var parsedJbin = JbinObject.Parse(bytes);
            var result = parsedJbin.ToObject<ZooModel>();

            Assert.IsNotNull(result);
            Assert.AreEqual("Central Safari", result.ZooName);

            // 验证 LeadAnimal 多态还原
            Assert.IsInstanceOfType(result.LeadAnimal, typeof(Dog));
            var leadDog = (Dog)result.LeadAnimal;
            Assert.AreEqual("Commander Dog", leadDog.Name);
            Assert.AreEqual(95, leadDog.BarkVolume);
            TestAssertHelper.AssertSequenceEqual(new byte[] { 0x12, 0x34, 0x56 }, leadDog.BarkVoiceData);

            // 验证 Animals 列表中不同派生类的多态还原
            Assert.IsNotNull(result.Animals);
            Assert.AreEqual(2, result.Animals.Count);

            Assert.IsInstanceOfType(result.Animals[0], typeof(Dog));
            var dog1 = (Dog)result.Animals[0];
            Assert.AreEqual("Buddy", dog1.Name);
            TestAssertHelper.AssertSequenceEqual(new byte[] { 0xAA, 0xBB }, dog1.BarkVoiceData);

            Assert.IsInstanceOfType(result.Animals[1], typeof(Cat));
            var cat1 = (Cat)result.Animals[1];
            Assert.AreEqual("Misty", cat1.Name);
            Assert.AreEqual(new Point(45, 90), cat1.FavouriteSpot);
        }
    }
}
