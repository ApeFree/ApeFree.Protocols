using STTech.BytesIO.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace ApeFree.Protocol.ApeFtp
{
    /// <summary>
    /// 传输响应
    /// </summary>
    public class TransferResponse : Response
    {
        public CommandCode CommandCode { get; } = CommandCode.TransferResponse;

        public uint TotalLength { get; set; }
        public byte[] MD5 { get; set; }
        public ResultCode ResultCode { get; set; }
        public byte MessageLength => (byte)Message.Length;
        public string Message { get; set; } = "";


        public TransferResponse(byte[] bytes) : base(bytes)
        {
            MD5 = bytes.Skip(1).Take(16).ToArray();
            TotalLength = BitConverter.ToUInt32(bytes.Skip(17).Take(4).Reverse().ToArray(), 0);
            ResultCode = (ResultCode)bytes.ElementAt(22);
            var messageLength = bytes.ElementAt(23);
            Message = bytes.Skip(23).Take(messageLength).EncodeToString();
        }

        public byte[] GetBytes()
        {
            return new byte[] { (byte)CommandCode }.Merge(
                                    MD5,
                                    BitConverter.GetBytes(TotalLength).Reverse(),
                                    new byte[] { (byte)ResultCode },
                                    new byte[] { MessageLength },
                                    Message.GetBytes()).ToArray();
        }
    }
}
