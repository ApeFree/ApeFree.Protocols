namespace ApeFree.Protocol.ApeFtp.Core.Packets
{
    /// <summary>
    /// 取消传输请求报文
    /// </summary>
    public class CancelRequest : ApeFtpPacket
    {
        public override PacketType PacketType => PacketType.CancelRequest;

        /// <summary>
        /// 取消原因代码
        /// </summary>
        public byte ReasonCode { get; set; }

        /// <summary>
        /// 取消原因描述
        /// </summary>
        public string? Message { get; set; }

        public CancelRequest(byte[] fileKey, byte reasonCode = 0, string? message = null)
            : base(fileKey)
        {
            ReasonCode = reasonCode;
            Message = message;
        }
    }

    /// <summary>
    /// 取消传输响应报文
    /// </summary>
    public class CancelResponse : ApeFtpPacket
    {
        public override PacketType PacketType => PacketType.CancelResponse;

        /// <summary>
        /// 结果码（通常为 ResultCode.Cancelled）
        /// </summary>
        public ResultCode ResultCode { get; set; }

        public CancelResponse(byte[] fileKey, ResultCode resultCode = ResultCode.Cancelled)
            : base(fileKey)
        {
            ResultCode = resultCode;
        }
    }
}
