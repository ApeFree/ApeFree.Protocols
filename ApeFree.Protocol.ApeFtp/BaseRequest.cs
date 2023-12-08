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
        public CommandCode CommandCode { get; }
        public byte[] MD5 { get; set; }

        public uint TotalLength { get; set; }

        public BaseRequest(CommandCode code, byte[] mD5, uint totalLength)
        {
            CommandCode = code;
            MD5 = mD5;
            TotalLength = totalLength;
        }

        public abstract byte[] GetBytes();
    }

    public abstract class BaseResponse : Response
    {

        public CommandCode CommandCode { get; }

        public MD5 MD5 { get; set; }

        public int TotalLength { get; set; }

        public BaseResponse(byte[] bytes) : base(bytes)
        {

        }

        public abstract byte[] GetBytes();
    }
}
