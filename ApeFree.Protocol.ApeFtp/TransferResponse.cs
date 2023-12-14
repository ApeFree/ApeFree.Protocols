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
    public class TransferResponse
    {
        /// <summary>
        /// 命令码
        /// </summary>
        public CommandCode CommandCode { get; } = CommandCode.TransferResponse;

        /// <summary>
        /// 总长度
        /// </summary>
        public uint TotalLength { get; set; }

        /// <summary>
        /// 文件MD5
        /// </summary>
        public byte[] Md5 { get; set; }

        /// <summary>
        /// 响应码
        /// </summary>
        public ResultCode ResultCode { get; set; }

        /// <summary>
        /// 附带消息长度
        /// </summary>
        public byte MessageLength => (byte)Message.Length;

        /// <summary>
        /// 附带文本消息
        /// </summary>
        public string Message { get; set; } = "";

        public TransferResponse() { }

        public TransferResponse(byte[] bytes)
        {
            Md5 = bytes.Skip(1).Take(16).ToArray();
            TotalLength = BitConverter.ToUInt32(bytes.Skip(17).Take(4).Reverse().ToArray(), 0);
            ResultCode = (ResultCode)bytes.ElementAt(21);
            var messageLength = bytes.ElementAt(22);
            if (messageLength > 0)
            {
                Message = bytes.Skip(23).Take(messageLength).EncodeToString();
            }
        }

        public byte[] GetBytes()
        {
            return new byte[] { (byte)CommandCode }.Merge(
                                    Md5,
                                    BitConverter.GetBytes(TotalLength).Reverse(),
                                    new byte[] { (byte)ResultCode },
                                    new byte[] { MessageLength },
                                    Message.GetBytes()).ToArray();
        }
    }
}
