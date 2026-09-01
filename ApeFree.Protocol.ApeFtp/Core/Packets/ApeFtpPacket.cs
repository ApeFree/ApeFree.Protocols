using System;

namespace ApeFree.Protocol.ApeFtp.Core.Packets
{
    /// <summary>
    /// ApeFtp 数据包基础接口
    /// </summary>
    public interface IApeFtpPacket
    {
        /// <summary>
        /// 数据包类型
        /// </summary>
        PacketType PacketType { get; }

        /// <summary>
        /// 传输任务唯一标识键（如 16 字节 MD5 或哈希指纹）
        /// </summary>
        byte[] FileKey { get; }
    }

    /// <summary>
    /// 抽象基础数据包
    /// </summary>
    public abstract class ApeFtpPacket : IApeFtpPacket
    {
        public abstract PacketType PacketType { get; }
        public byte[] FileKey { get; set; }

        protected ApeFtpPacket(byte[] fileKey)
        {
            FileKey = fileKey ?? throw new ArgumentNullException(nameof(fileKey));
        }
    }
}
