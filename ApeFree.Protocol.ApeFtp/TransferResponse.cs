using System;
using System.Collections.Generic;
using System.Linq;

namespace ApeFree.Protocol.ApeFtp
{
    /// <summary>
    /// 传输响应
    /// </summary>
    public class TransferResponse : BaseRequest
    {
        public IEnumerable<byte> MD5 { get; set; }
        public uint TotalLength { get; set; }
        public ResultCode ResultCode { get; set; }
        public byte MessageLength => (byte)Message.Length;
        public string Message { get; set; } = "";

        public TransferResponse() : base(CommandCode.TransferResponse) { }
        public TransferResponse(IEnumerable<byte> bytes) : base(CommandCode.TransferResponse)
        {
            MD5 = bytes.Skip(1).Take(16);
            ResultCode = (ResultCode)bytes.ElementAt(17);
            var messageLength = bytes.ElementAt(18);
            Message = bytes.Skip(19).Take(messageLength).EncodeToString();
        }

        public override byte[] GetBytes()
        {
            return new byte[] { (byte)CommandCode }.Merge(
                                    MD5,
                                    new byte[] { (byte)ResultCode },
                                    new byte[] { MessageLength },
                                    Message.GetBytes()
                                ).ToArray();
        }
    }
}
