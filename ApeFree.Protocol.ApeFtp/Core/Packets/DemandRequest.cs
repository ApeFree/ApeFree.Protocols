namespace ApeFree.Protocol.ApeFtp.Core.Packets
{
    /// <summary>
    /// 传输申请请求报文 (Sender -> Receiver)
    /// </summary>
    public class DemandRequest : ApeFtpPacket
    {
        public override PacketType PacketType => PacketType.DemandRequest;

        /// <summary>
        /// 文件总字节数
        /// </summary>
        public ulong TotalLength { get; set; }

        /// <summary>
        /// 发送端期望的单分段大小
        /// </summary>
        public uint ChunkSize { get; set; }

        /// <summary>
        /// 发送端建议的突发窗口大小（在收到 ACK 前允许连续发送的包数）
        /// </summary>
        public uint WindowSize { get; set; } = 8;

        /// <summary>
        /// 原始文件名（可选元数据）
        /// </summary>
        public string? FileName { get; set; }

        public DemandRequest(byte[] fileKey, ulong totalLength, uint chunkSize, uint windowSize = 8, string? fileName = null)
            : base(fileKey)
        {
            TotalLength = totalLength;
            ChunkSize = chunkSize;
            WindowSize = windowSize > 0 ? windowSize : 1;
            FileName = fileName;
        }
    }
}
