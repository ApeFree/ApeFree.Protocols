namespace ApeFree.Protocol.ApeFtp.Core.Packets
{
    /// <summary>
    /// 批量分片确认报文 (Receiver -> Sender)
    /// </summary>
    public class AckResponse : ApeFtpPacket
    {
        public override PacketType PacketType => PacketType.AckResponse;

        /// <summary>
        /// 确认状态结果码
        /// </summary>
        public ResultCode ResultCode { get; set; }

        /// <summary>
        /// 确认的分片序号（通常为当前连续成功接收的最大分片序号）
        /// </summary>
        public uint AckChunkIndex { get; set; }

        /// <summary>
        /// 本轮突发批次中成功接收的分片数量
        /// </summary>
        public uint AckCount { get; set; }

        /// <summary>
        /// 附加消息
        /// </summary>
        public string? Message { get; set; }

        public AckResponse(byte[] fileKey, ResultCode resultCode, uint ackChunkIndex, uint ackCount = 1, string? message = null)
            : base(fileKey)
        {
            ResultCode = resultCode;
            AckChunkIndex = ackChunkIndex;
            AckCount = ackCount;
            Message = message;
        }
    }
}
