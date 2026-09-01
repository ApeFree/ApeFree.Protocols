using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using ApeFree.Protocol.ApeFtp.Codec;
using ApeFree.Protocol.ApeFtp.Core;
using ApeFree.Protocol.ApeFtp.Core.Packets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApeFree.Protocol.ApeFtp.Tests
{
    [TestClass]
    public class FrameCodecTests
    {
        private static byte[] GenerateRandomKey()
        {
            byte[] key = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(key);
            return key;
        }

        [TestMethod]
        public void Codec_DemandRequest_Roundtrip()
        {
            var key = GenerateRandomKey();
            var req = new DemandRequest(key, 1024 * 1024 * 10, 64 * 1024, 16, "test_document.pdf");

            byte[] encoded = ApeFtpFrameEncoder.Encode(req);
            Assert.IsTrue(ApeFtpFrameDecoder.TryDecodeSingleFrame(encoded, out var decoded, out int consumed));
            Assert.AreEqual(encoded.Length, consumed);

            var decodedReq = decoded as DemandRequest;
            Assert.IsNotNull(decodedReq);
            CollectionAssert.AreEqual(req.FileKey, decodedReq.FileKey);
            Assert.AreEqual(req.TotalLength, decodedReq.TotalLength);
            Assert.AreEqual(req.ChunkSize, decodedReq.ChunkSize);
            Assert.AreEqual(req.WindowSize, decodedReq.WindowSize);
            Assert.AreEqual(req.FileName, decodedReq.FileName);
        }

        [TestMethod]
        public void Codec_DemandResponse_Roundtrip()
        {
            var key = GenerateRandomKey();
            var resp = new DemandResponse(key, ResultCode.Success, 32 * 1024, 8, 1024 * 512, "OK to proceed");

            byte[] encoded = ApeFtpFrameEncoder.Encode(resp);
            Assert.IsTrue(ApeFtpFrameDecoder.TryDecodeSingleFrame(encoded, out var decoded, out int consumed));

            var decodedResp = decoded as DemandResponse;
            Assert.IsNotNull(decodedResp);
            CollectionAssert.AreEqual(resp.FileKey, decodedResp.FileKey);
            Assert.AreEqual(resp.ResultCode, decodedResp.ResultCode);
            Assert.AreEqual(resp.AcceptedChunkSize, decodedResp.AcceptedChunkSize);
            Assert.AreEqual(resp.AcceptedWindowSize, decodedResp.AcceptedWindowSize);
            Assert.AreEqual(resp.ResumedOffset, decodedResp.ResumedOffset);
            Assert.AreEqual(resp.Message, decodedResp.Message);
        }

        [TestMethod]
        public void Codec_DataPacket_Roundtrip()
        {
            var key = GenerateRandomKey();
            byte[] payloadData = new byte[1024];
            new Random(42).NextBytes(payloadData);

            var packet = new DataPacket(key, 5, 5 * 1024, payloadData);
            Assert.IsTrue(packet.ValidateCrc());

            byte[] encoded = ApeFtpFrameEncoder.Encode(packet);
            Assert.IsTrue(ApeFtpFrameDecoder.TryDecodeSingleFrame(encoded, out var decoded, out int consumed));

            var decodedPacket = decoded as DataPacket;
            Assert.IsNotNull(decodedPacket);
            CollectionAssert.AreEqual(packet.FileKey, decodedPacket.FileKey);
            Assert.AreEqual(packet.ChunkIndex, decodedPacket.ChunkIndex);
            Assert.AreEqual(packet.Offset, decodedPacket.Offset);
            Assert.AreEqual(packet.ChunkCrc32, decodedPacket.ChunkCrc32);
            CollectionAssert.AreEqual(packet.Data, decodedPacket.Data);
            Assert.IsTrue(decodedPacket.ValidateCrc());
        }

        [TestMethod]
        public void Codec_AckResponse_Roundtrip()
        {
            var key = GenerateRandomKey();
            var ack = new AckResponse(key, ResultCode.Completed, 128, 16, "Finished successfully");

            byte[] encoded = ApeFtpFrameEncoder.Encode(ack);
            Assert.IsTrue(ApeFtpFrameDecoder.TryDecodeSingleFrame(encoded, out var decoded, out _));

            var decodedAck = decoded as AckResponse;
            Assert.IsNotNull(decodedAck);
            CollectionAssert.AreEqual(ack.FileKey, decodedAck.FileKey);
            Assert.AreEqual(ack.ResultCode, decodedAck.ResultCode);
            Assert.AreEqual(ack.AckChunkIndex, decodedAck.AckChunkIndex);
            Assert.AreEqual(ack.AckCount, decodedAck.AckCount);
            Assert.AreEqual(ack.Message, decodedAck.Message);
        }

        [TestMethod]
        public void Codec_CancelRequestAndResponse_Roundtrip()
        {
            var key = GenerateRandomKey();
            var cancelReq = new CancelRequest(key, 1, "User clicked cancel");
            var cancelResp = new CancelResponse(key, ResultCode.Cancelled);

            byte[] encReq = ApeFtpFrameEncoder.Encode(cancelReq);
            byte[] encResp = ApeFtpFrameEncoder.Encode(cancelResp);

            Assert.IsTrue(ApeFtpFrameDecoder.TryDecodeSingleFrame(encReq, out var decReq, out _));
            Assert.IsTrue(ApeFtpFrameDecoder.TryDecodeSingleFrame(encResp, out var decResp, out _));

            var req = decReq as CancelRequest;
            var resp = decResp as CancelResponse;

            Assert.IsNotNull(req);
            Assert.IsNotNull(resp);
            Assert.AreEqual((byte)1, req.ReasonCode);
            Assert.AreEqual("User clicked cancel", req.Message);
            Assert.AreEqual(ResultCode.Cancelled, resp.ResultCode);
        }

        [TestMethod]
        public void Decoder_StickyPackets_SuccessfullyParsedAll()
        {
            var key = GenerateRandomKey();
            var p1 = new DemandRequest(key, 1000, 100);
            var p2 = new DemandResponse(key, ResultCode.Success, 100, 4);
            var p3 = new DataPacket(key, 0, 0, new byte[] { 1, 2, 3, 4 });

            byte[] stream = ApeFtpFrameEncoder.Encode(p1)
                .Concat(ApeFtpFrameEncoder.Encode(p2))
                .Concat(ApeFtpFrameEncoder.Encode(p3))
                .ToArray();

            var decoder = new ApeFtpFrameDecoder();
            var decodedList = new List<IApeFtpPacket>();
            decoder.PacketDecoded += p => decodedList.Add(p);

            decoder.Feed(stream);

            Assert.AreEqual(3, decodedList.Count);
            Assert.IsInstanceOfType(decodedList[0], typeof(DemandRequest));
            Assert.IsInstanceOfType(decodedList[1], typeof(DemandResponse));
            Assert.IsInstanceOfType(decodedList[2], typeof(DataPacket));
        }

        [TestMethod]
        public void Decoder_ByteByByte_HalfPacketsHandled()
        {
            var key = GenerateRandomKey();
            var p = new DemandRequest(key, 5000, 256, 8, "test.bin");
            byte[] encoded = ApeFtpFrameEncoder.Encode(p);

            var decoder = new ApeFtpFrameDecoder();
            var decodedList = new List<IApeFtpPacket>();
            decoder.PacketDecoded += decodedList.Add;

            // 逐字节 Feed 模拟极端半包网络环境
            foreach (byte b in encoded)
            {
                decoder.Feed(new byte[] { b });
            }

            Assert.AreEqual(1, decodedList.Count);
            var result = decodedList[0] as DemandRequest;
            Assert.IsNotNull(result);
            Assert.AreEqual((ulong)5000, result.TotalLength);
            Assert.AreEqual("test.bin", result.FileName);
        }

        [TestMethod]
        public void Decoder_NoisePrefix_SyncsAndParses()
        {
            var key = GenerateRandomKey();
            var p = new DemandResponse(key, ResultCode.Success, 512, 4);
            byte[] encoded = ApeFtpFrameEncoder.Encode(p);

            // 前置噪声/垃圾数据
            byte[] noise = new byte[] { 0x12, 0x34, 0xAF, 0x99, 0x00, 0xFF };
            byte[] combined = noise.Concat(encoded).ToArray();

            var decoder = new ApeFtpFrameDecoder();
            var decodedList = new List<IApeFtpPacket>();
            decoder.PacketDecoded += decodedList.Add;

            decoder.Feed(combined);

            Assert.AreEqual(1, decodedList.Count);
            Assert.IsInstanceOfType(decodedList[0], typeof(DemandResponse));
        }

        [TestMethod]
        public void Decoder_CorruptedFrameCrc_DiscardsAndRecovers()
        {
            var key = GenerateRandomKey();
            var badPacket = new DemandRequest(key, 100, 10);
            var goodPacket = new DemandResponse(key, ResultCode.Success);

            byte[] badBytes = ApeFtpFrameEncoder.Encode(badPacket);
            // 篡改 Payload 中的一个字节破坏 CRC
            badBytes[badBytes.Length - 5] ^= 0xEE;

            byte[] goodBytes = ApeFtpFrameEncoder.Encode(goodPacket);
            byte[] combined = badBytes.Concat(goodBytes).ToArray();

            var decoder = new ApeFtpFrameDecoder();
            var decodedList = new List<IApeFtpPacket>();
            int errorCount = 0;

            decoder.PacketDecoded += decodedList.Add;
            decoder.DecodeErrorOccurred += msg => errorCount++;

            decoder.Feed(combined);

            Assert.IsTrue(errorCount >= 1);
            Assert.AreEqual(1, decodedList.Count);
            Assert.IsInstanceOfType(decodedList[0], typeof(DemandResponse));
        }
    }
}
