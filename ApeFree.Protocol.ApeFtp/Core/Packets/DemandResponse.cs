namespace ApeFree.Protocol.ApeFtp.Core.Packets
{
    /// <summary>
    /// 传输申请响应报文 (Receiver -> Sender)
    /// </summary>
    public class DemandResponse : ApeFtpPacket
    {
        public override PacketType PacketType => PacketType.DemandResponse;

        /// <summary>
        /// 协商结果码
        /// </summary>
        public ResultCode ResultCode { get; set; }

        /// <summary>
        /// 接收端确认/协商后的分段大小
        /// </summary>
        public uint AcceptedChunkSize { get; set; }

        /// <summary>
        /// 接收端确认/协商后的突发窗口大小
        /// </summary>
        public uint AcceptedWindowSize { get; set; }

        /// <summary>
        /// 断点续传起始偏移量（若目标端已有部分连续数据，可指示发送端从该偏移开始发送）
        /// </summary>
        public ulong ResumedOffset { get; set; }

        /// <summary>
        /// 附加文本消息（如错误详情）
        /// </summary>
        public string? Message { get; set; }

        public DemandResponse(byte[] fileKey, ResultCode resultCode, uint acceptedChunkSize = 0, uint acceptedWindowSize = 0, ulong resumedOffset = 0, string? message = null)
            : base(fileKey)
        {
            ResultCode = resultCode;
            AcceptedChunkSize = acceptedChunkSize;
            AcceptedWindowSize = acceptedWindowSize;
            ResumedOffset = resumedOffset;
            Message = message;
        }
    }
}
