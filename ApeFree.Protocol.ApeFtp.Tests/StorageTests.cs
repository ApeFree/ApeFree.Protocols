using System;
using System.IO;
using System.Security.Cryptography;
using ApeFree.Protocol.ApeFtp.Core;
using ApeFree.Protocol.ApeFtp.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApeFree.Protocol.ApeFtp.Tests
{
    [TestClass]
    public class StorageTests
    {
        [TestMethod]
        public void SessionStore_InMemory_CRUD()
        {
            var store = new InMemoryTransferSessionStore();
            byte[] key = new byte[] { 1, 2, 3, 4, 5 };

            Assert.IsFalse(store.Exists(key));
            Assert.IsNull(store.GetSession(key));

            var record = new TransferSessionRecord(key, 1024 * 1024, 64 * 1024, "sample.txt");
            store.SaveOrUpdateSession(record);

            Assert.IsTrue(store.Exists(key));
            var retrieved = store.GetSession(key);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual((ulong)1024 * 1024, retrieved.TotalLength);
            Assert.AreEqual("sample.txt", retrieved.FileName);
            Assert.AreEqual(SessionState.Created, retrieved.State);

            store.UpdateProgress(key, 512 * 1024, 8);
            retrieved = store.GetSession(key);
            Assert.AreEqual((ulong)512 * 1024, retrieved!.ReceivedBytes);
            Assert.AreEqual(8u, retrieved.LastAckedChunkIndex);

            store.UpdateState(key, SessionState.Completed);
            retrieved = store.GetSession(key);
            Assert.AreEqual(SessionState.Completed, retrieved!.State);

            Assert.IsTrue(store.RemoveSession(key));
            Assert.IsFalse(store.Exists(key));
        }

        [TestMethod]
        public void MemoryDataSinkSource_OutOfOrderWrite_ValidatesHash()
        {
            byte[] originalData = new byte[100 * 1024];
            new Random(99).NextBytes(originalData);

            using var source = new MemoryDataSource(originalData, "memory.dat");
            using var sink = new MemoryDataSink(source.TotalLength);

            // 乱序写入分片 (先写后半段，再写前半段)
            int chunkSize = 20 * 1024;
            ulong offset2 = (ulong)chunkSize * 2; // 40KB
            byte[] chunk2 = source.ReadChunk(offset2, chunkSize);
            sink.WriteChunk(offset2, chunk2);

            ulong offset0 = 0; // 0KB
            byte[] chunk0 = source.ReadChunk(offset0, chunkSize);
            sink.WriteChunk(offset0, chunk0);

            ulong offset1 = (ulong)chunkSize; // 20KB
            byte[] chunk1 = source.ReadChunk(offset1, chunkSize);
            sink.WriteChunk(offset1, chunk1);

            ulong offset3 = (ulong)chunkSize * 3; // 60KB
            byte[] chunk3 = source.ReadChunk(offset3, chunkSize);
            sink.WriteChunk(offset3, chunk3);

            ulong offset4 = (ulong)chunkSize * 4; // 80KB
            byte[] chunk4 = source.ReadChunk(offset4, chunkSize);
            sink.WriteChunk(offset4, chunk4);

            Assert.IsTrue(sink.VerifyAndFinalize(source.Hash));
            CollectionAssert.AreEqual(originalData, sink.GetBuffer());
        }

        [TestMethod]
        public void FileDataSinkSource_FileLifecycleAndVerification()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "ApeFtpTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string srcFile = Path.Combine(tempDir, "source.bin");
                string dstFile = Path.Combine(tempDir, "target.bin");

                byte[] data = new byte[256 * 1024];
                new Random(88).NextBytes(data);
                File.WriteAllBytes(srcFile, data);

                using (var source = new FileDataSource(srcFile))
                using (var sink = new FileDataSink(dstFile, source.TotalLength))
                {
                    Assert.AreEqual((ulong)data.Length, source.TotalLength);

                    int chunkSize = 64 * 1024;
                    for (ulong offset = 0; offset < source.TotalLength; offset += (ulong)chunkSize)
                    {
                        byte[] chunk = source.ReadChunk(offset, chunkSize);
                        sink.WriteChunk(offset, chunk);
                    }

                    Assert.IsTrue(sink.VerifyAndFinalize(source.Hash));
                }

                Assert.IsTrue(File.Exists(dstFile));
                byte[] writtenData = File.ReadAllBytes(dstFile);
                CollectionAssert.AreEqual(data, writtenData);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }
    }
}
