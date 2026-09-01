using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using ApeFree.Protocol.ApeFtp.Codec;
using ApeFree.Protocol.ApeFtp.Core;
using ApeFree.Protocol.ApeFtp.Core.Packets;
using ApeFree.Protocol.ApeFtp.Engine;
using ApeFree.Protocol.ApeFtp.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApeFree.Protocol.ApeFtp.Tests
{
    [TestClass]
    public class EngineTransferTests
    {
        private static void ConnectEngines(ApeFtpSenderEngine sender, ApeFtpReceiverEngine receiver)
        {
            sender.PacketReadyToSend += (s, e) => receiver.Feed(e.EncodedFrame);
            receiver.PacketReadyToSend += (s, e) => sender.Feed(e.EncodedFrame);
        }

        [TestMethod]
        public void Transfer_MemoryToMemory_BurstWindow_Success()
        {
            // 构造 500KB 测试数据，ChunkSize = 32KB，WindowSize = 4
            byte[] originalData = new byte[500 * 1024];
            new Random(101).NextBytes(originalData);

            using var source = new MemoryDataSource(originalData, "transfer_test.bin");
            using var sink = new MemoryDataSink(source.TotalLength);

            using var sender = new ApeFtpSenderEngine(source, defaultChunkSize: 32 * 1024, defaultWindowSize: 4);
            using var receiver = new ApeFtpReceiverEngine(req => sink);

            ConnectEngines(sender, receiver);

            bool senderCompleted = false;
            bool receiverCompleted = false;
            var progressList = new List<double>();

            sender.ProgressChanged += (s, e) => progressList.Add(e.ProgressPercentage);
            sender.Completed += (s, e) => senderCompleted = true;
            receiver.Completed += (s, e) => receiverCompleted = true;

            sender.Start();

            Assert.IsTrue(senderCompleted, "发送端应标记传输完成");
            Assert.IsTrue(receiverCompleted, "接收端应标记传输完成");
            Assert.AreEqual(SessionState.Completed, sender.State);
            Assert.IsTrue(progressList.Count > 0);
            Assert.AreEqual(100.0, progressList[progressList.Count - 1], 0.001);

            CollectionAssert.AreEqual(originalData, sink.GetBuffer());
        }

        [TestMethod]
        public void Transfer_FastUpload_TriggeredWhenAlreadyExists()
        {
            byte[] fileData = new byte[100 * 1024];
            new Random(202).NextBytes(fileData);

            var sessionStore = new InMemoryTransferSessionStore();
            using var source = new MemoryDataSource(fileData);

            // 预先向存储中存入已完成状态的记录
            var existingRecord = new TransferSessionRecord(source.Hash, source.TotalLength, 32 * 1024)
            {
                State = SessionState.Completed
            };
            sessionStore.SaveOrUpdateSession(existingRecord);

            using var sender = new ApeFtpSenderEngine(source, defaultChunkSize: 32 * 1024);
            using var sink = new MemoryDataSink(source.TotalLength);
            using var receiver = new ApeFtpReceiverEngine(req => sink, sessionStore);

            ConnectEngines(sender, receiver);

            bool isFastUpload = false;
            sender.Completed += (s, e) => isFastUpload = e.IsFastUpload;

            sender.Start();

            Assert.IsTrue(isFastUpload, "应触发秒传 (FastUpload)");
            Assert.AreEqual(SessionState.Completed, sender.State);
        }

        [TestMethod]
        public void Transfer_BreakpointResume_Success()
        {
            // 准备 200KB 数据
            byte[] fileData = new byte[200 * 1024];
            new Random(303).NextBytes(fileData);

            var sessionStore = new InMemoryTransferSessionStore();
            using var source = new MemoryDataSource(fileData);
            using var sink = new MemoryDataSink(source.TotalLength);

            // 模拟前 100KB 数据已被接收过
            ulong alreadyTransferred = 100 * 1024;
            sink.WriteChunk(0, fileData.AsSpan(0, (int)alreadyTransferred));

            var record = new TransferSessionRecord(source.Hash, source.TotalLength, 32 * 1024)
            {
                ReceivedBytes = alreadyTransferred,
                State = SessionState.Transferring,
            };
            sessionStore.SaveOrUpdateSession(record);

            // 启动断点续传
            using var sender = new ApeFtpSenderEngine(source, defaultChunkSize: 32 * 1024);
            using var receiver = new ApeFtpReceiverEngine(req => sink, sessionStore);

            ConnectEngines(sender, receiver);

            bool completed = false;
            sender.Completed += (s, e) => completed = true;

            sender.Start();

            Assert.IsTrue(completed, "断点续传应成功完成");
            Assert.AreEqual(SessionState.Completed, sender.State);
            CollectionAssert.AreEqual(fileData, sink.GetBuffer());
        }

        [TestMethod]
        public void Transfer_ChunkCorruption_RetransmitsAndCompletes()
        {
            byte[] fileData = new byte[128 * 1024];
            new Random(404).NextBytes(fileData);

            using var source = new MemoryDataSource(fileData);
            using var sink = new MemoryDataSink(source.TotalLength);

            using var sender = new ApeFtpSenderEngine(source, defaultChunkSize: 32 * 1024, defaultWindowSize: 2);
            using var receiver = new ApeFtpReceiverEngine(req => sink);

            bool corruptedOnce = false;

            // 模拟破坏第 2 个分片包的负载 CRC
            sender.PacketReadyToSend += (s, e) =>
            {
                byte[] frame = e.EncodedFrame;
                if (e.Packet is DataPacket dp && dp.ChunkIndex == 1 && !corruptedOnce)
                {
                    corruptedOnce = true;
                    // 构造一个负载 CRC 损坏的合法帧
                    var badPacket = new DataPacket(dp.FileKey, dp.ChunkIndex, dp.Offset, dp.Data, dp.ChunkCrc32 ^ 0xFFFFFFFF);
                    frame = ApeFtpFrameEncoder.Encode(badPacket);
                }
                receiver.Feed(frame);
            };

            receiver.PacketReadyToSend += (s, e) => sender.Feed(e.EncodedFrame);

            bool completed = false;
            sender.Completed += (s, e) => completed = true;

            sender.Start();

            Assert.IsTrue(corruptedOnce, "应触发过一次故意损坏");
            Assert.IsTrue(completed, "损坏包被重传后应能成功完成");
            CollectionAssert.AreEqual(fileData, sink.GetBuffer());
        }

        [TestMethod]
        public void Transfer_Cancellation_AbortsState()
        {
            byte[] fileData = new byte[500 * 1024];
            using var source = new MemoryDataSource(fileData);
            using var sink = new MemoryDataSink(source.TotalLength);

            using var sender = new ApeFtpSenderEngine(source, defaultChunkSize: 16 * 1024);
            using var receiver = new ApeFtpReceiverEngine(req => sink);

            ConnectEngines(sender, receiver);

            bool senderCancelled = false;
            bool receiverCancelled = false;

            sender.Cancelled += (s, e) => senderCancelled = true;
            receiver.Cancelled += (s, e) => receiverCancelled = true;

            sender.ProgressChanged += (s, e) =>
            {
                if (e.CurrentChunkIndex >= 2 && !senderCancelled)
                {
                    sender.Cancel("用户取消测试");
                }
            };

            sender.Start();

            Assert.IsTrue(senderCancelled, "发送端应标记取消");
            Assert.IsTrue(receiverCancelled, "接收端应收到并标记取消");
            Assert.AreEqual(SessionState.Cancelled, sender.State);
        }
    }
}
