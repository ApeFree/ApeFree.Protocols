using System;
using System.IO;
using System.Text;
using ApeFree.Protocol.ApeFtp.Core;
using ApeFree.Protocol.ApeFtp.Core.Packets;

namespace ApeFree.Protocol.ApeFtp.Codec
{
    /// <summary>
    /// ApeFtp 报文帧编码器
    /// </summary>
    public static class ApeFtpFrameEncoder
    {
        /// <summary>
        /// 帧头魔数 (0xAF, 0x46 -> 'A', 'F')
        /// </summary>
        public static readonly byte[] Magic = new byte[] { 0xAF, 0x46 };

        /// <summary>
        /// 将数据包编码为二进制帧字节数组
        /// </summary>
        public static byte[] Encode(IApeFtpPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            using var payloadStream = new MemoryStream();
            EncodePayload(packet, payloadStream);
            var payloadBytes = payloadStream.ToArray();

            // 计算 Payload CRC32
            uint payloadCrc = Crc32.Compute(payloadBytes);

            using var frameStream = new MemoryStream();
            // 1. 写入魔数 (2B)
            frameStream.Write(Magic, 0, Magic.Length);
            // 2. 写入数据包类型 (1B)
            frameStream.WriteByte((byte)packet.PacketType);
            // 3. 写入 Payload 长度 (VarInt)
            VarInt.Write((ulong)payloadBytes.Length, frameStream);
            // 4. 写入 Payload 字节流
            frameStream.Write(payloadBytes, 0, payloadBytes.Length);
            // 5. 写入 CRC32 (4B Big-Endian)
            frameStream.WriteByte((byte)(payloadCrc >> 24));
            frameStream.WriteByte((byte)(payloadCrc >> 16));
            frameStream.WriteByte((byte)(payloadCrc >> 8));
            frameStream.WriteByte((byte)payloadCrc);

            return frameStream.ToArray();
        }

        private static void EncodePayload(IApeFtpPacket packet, Stream stream)
        {
            // 写入 FileKey (VarInt length + raw bytes)
            var fileKey = packet.FileKey ?? Array.Empty<byte>();
            VarInt.Write((ulong)fileKey.Length, stream);
            if (fileKey.Length > 0)
            {
                stream.Write(fileKey, 0, fileKey.Length);
            }

            switch (packet)
            {
                case DemandRequest req:
                    VarInt.Write(req.TotalLength, stream);
                    VarInt.Write(req.ChunkSize, stream);
                    VarInt.Write(req.WindowSize, stream);
                    WriteString(req.FileName, stream);
                    break;

                case DemandResponse resp:
                    stream.WriteByte((byte)resp.ResultCode);
                    VarInt.Write(resp.AcceptedChunkSize, stream);
                    VarInt.Write(resp.AcceptedWindowSize, stream);
                    VarInt.Write(resp.ResumedOffset, stream);
                    WriteString(resp.Message, stream);
                    break;

                case DataPacket data:
                    VarInt.Write(data.ChunkIndex, stream);
                    VarInt.Write(data.Offset, stream);
                    // 写入 4B ChunkCrc32
                    stream.WriteByte((byte)(data.ChunkCrc32 >> 24));
                    stream.WriteByte((byte)(data.ChunkCrc32 >> 16));
                    stream.WriteByte((byte)(data.ChunkCrc32 >> 8));
                    stream.WriteByte((byte)data.ChunkCrc32);
                    // 写入数据
                    var chunkData = data.Data ?? Array.Empty<byte>();
                    VarInt.Write((ulong)chunkData.Length, stream);
                    if (chunkData.Length > 0)
                    {
                        stream.Write(chunkData, 0, chunkData.Length);
                    }
                    break;

                case AckResponse ack:
                    stream.WriteByte((byte)ack.ResultCode);
                    VarInt.Write(ack.AckChunkIndex, stream);
                    VarInt.Write(ack.AckCount, stream);
                    WriteString(ack.Message, stream);
                    break;

                case CancelRequest cancelReq:
                    stream.WriteByte(cancelReq.ReasonCode);
                    WriteString(cancelReq.Message, stream);
                    break;

                case CancelResponse cancelResp:
                    stream.WriteByte((byte)cancelResp.ResultCode);
                    break;

                default:
                    throw new NotSupportedException($"不支持的数据包类型: {packet.GetType().FullName}");
            }
        }

        private static void WriteString(string? str, Stream stream)
        {
            if (string.IsNullOrEmpty(str))
            {
                VarInt.Write(0, stream);
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(str);
            VarInt.Write((ulong)bytes.Length, stream);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
