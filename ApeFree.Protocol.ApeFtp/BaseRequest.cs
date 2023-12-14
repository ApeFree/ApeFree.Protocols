using STTech.BytesIO.Core;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace ApeFree.Protocol.ApeFtp
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseRequest : IRequest
    {
        /// <summary>
        /// 功能码
        /// </summary>
        public CommandCode CommandCode { get; }

        /// <summary>
        /// 文件MD5
        /// </summary>
        public byte[] MD5 { get; set; }

        /// <summary>
        /// 文件总长度
        /// </summary>
        public uint TotalLength { get; set; }

        public BaseRequest(CommandCode code, byte[] mD5, uint totalLength)
        {
            CommandCode = code;
            MD5 = mD5;
            TotalLength = totalLength;
        }

        public abstract byte[] GetBytes();
    }
}
