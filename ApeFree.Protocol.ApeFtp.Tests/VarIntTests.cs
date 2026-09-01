using System;
using System.IO;
using ApeFree.Protocol.ApeFtp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApeFree.Protocol.ApeFtp.Tests
{
    [TestClass]
    public class VarIntTests
    {
        [TestMethod]
        [DataRow((ulong)0, 1)]
        [DataRow((ulong)1, 1)]
        [DataRow((ulong)127, 1)]
        [DataRow((ulong)128, 2)]
        [DataRow((ulong)255, 2)]
        [DataRow((ulong)16383, 2)]
        [DataRow((ulong)16384, 3)]
        [DataRow((ulong)65535, 3)]
        [DataRow((ulong)int.MaxValue, 5)]
        [DataRow((ulong)uint.MaxValue, 5)]
        [DataRow(ulong.MaxValue, 10)]
        public void VarInt_Roundtrip_Span(ulong value, int expectedBytes)
        {
            int byteCount = VarInt.GetByteCount(value);
            Assert.AreEqual(expectedBytes, byteCount);

            Span<byte> buffer = stackalloc byte[16];
            bool written = VarInt.TryWrite(value, buffer, out int bytesWritten);
            Assert.IsTrue(written);
            Assert.AreEqual(expectedBytes, bytesWritten);

            bool read = VarInt.TryRead(buffer.Slice(0, bytesWritten), out ulong decodedValue, out int bytesRead);
            Assert.IsTrue(read);
            Assert.AreEqual(bytesWritten, bytesRead);
            Assert.AreEqual(value, decodedValue);
        }

        [TestMethod]
        public void VarInt_Roundtrip_Stream()
        {
            ulong[] testValues = new ulong[]
            {
                0, 1, 127, 128, 255, 1024, 65535, 1000000, uint.MaxValue, (ulong)uint.MaxValue + 100, ulong.MaxValue
            };

            using var ms = new MemoryStream();
            foreach (var val in testValues)
            {
                VarInt.Write(val, ms);
            }

            ms.Position = 0;
            foreach (var expectedVal in testValues)
            {
                bool read = VarInt.TryRead(ms, out ulong decodedVal, out int bytesRead);
                Assert.IsTrue(read);
                Assert.IsTrue(bytesRead > 0);
                Assert.AreEqual(expectedVal, decodedVal);
            }
        }

        [TestMethod]
        public void VarInt_BufferTooSmall_ReturnsFalse()
        {
            Span<byte> buffer = stackalloc byte[1];
            bool written = VarInt.TryWrite(128, buffer, out int bytesWritten);
            Assert.IsFalse(written);
        }

        [TestMethod]
        public void VarInt_RandomRoundtrip()
        {
            var rand = new Random(42);
            byte[] raw = new byte[8];
            Span<byte> buffer = stackalloc byte[16];

            for (int i = 0; i < 1000; i++)
            {
                rand.NextBytes(raw);
                ulong value = BitConverter.ToUInt64(raw, 0);

                Assert.IsTrue(VarInt.TryWrite(value, buffer, out int written));
                Assert.IsTrue(VarInt.TryRead(buffer.Slice(0, written), out ulong decoded, out int read));
                Assert.AreEqual(value, decoded);
                Assert.AreEqual(written, read);
            }
        }
    }
}
