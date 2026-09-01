using System;

namespace ApeFree.Protocol.ApeFtp.Core.Packets
{
    /// <summary>
    /// 数据分片报文 (Sender -> Receiver)
    /// </summary>
    public class DataPacket : ApeFtpPacket
    {
        public override PacketType PacketType => PacketType.DataPacket;

        /// <summary>
        /// 分片序号（0-based）
        /// </summary>
        public uint ChunkIndex { get; set; }

        /// <summary>
        /// 本分片在文件中的起始字节偏移量
        /// </summary>
        public ulong Offset { get; set; }

        /// <summary>
        /// 分片数据负载
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// 本分片数据的 CRC32 校验和
        /// </summary>
        public uint ChunkCrc32 { get; set; }

        public DataPacket(byte[] fileKey, uint chunkIndex, ulong offset, byte[] data, uint? chunkCrc32 = null)
            : base(fileKey)
        {
            ChunkIndex = chunkIndex;
            Offset = offset;
            Data = data ?? Array.Empty<byte>();
            ChunkCrc32 = chunkCrc32 ?? Crc32.Compute(Data);
        }

        /// <summary>
        /// 验证当前分片数据与 CRC32 校验和是否一致
        /// </summary>
        public bool ValidateCrc()
        {
            return Crc32.Compute(Data) == ChunkCrc32;
        }
    }
}
