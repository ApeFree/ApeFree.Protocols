using System;
using System.Text;
using ApeFree.Protocol.ApeFtp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApeFree.Protocol.ApeFtp.Tests
{
    [TestClass]
    public class Crc32Tests
    {
        [TestMethod]
        public void Crc32_StandardVector_123456789()
        {
            byte[] data = Encoding.ASCII.GetBytes("123456789");
            uint crc = Crc32.Compute(data);

            // 标准 IEEE 802.3 CRC32("123456789") = 0xCBF43926
            Assert.AreEqual(0xCBF43926, crc);
        }

        [TestMethod]
        public void Crc32_EmptyArray_Zero()
        {
            byte[] data = Array.Empty<byte>();
            uint crc = Crc32.Compute(data);
            Assert.AreEqual(0u, crc);
        }

        [TestMethod]
        public void Crc32_DataCorruption_Detected()
        {
            byte[] data = new byte[1024];
            new Random(123).NextBytes(data);

            uint originalCrc = Crc32.Compute(data);

            // 改变其中一个字节
            data[500] ^= 0xFF;
            uint corruptedCrc = Crc32.Compute(data);

            Assert.AreNotEqual(originalCrc, corruptedCrc);
        }
    }
}
