using System.Collections.Generic;

namespace ApeFree.Protocol.ApeFtp
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseRequest
    {
        public uint RequestId {  get; set; }
        public CommandCode CommandCode { get; }

        public BaseRequest(CommandCode code)
        {
            CommandCode = code;
        }

        public abstract byte[] GetBytes();
    }

    public abstract class BaseResponse
    {
        public uint RequestId { get; set; }

        public CommandCode CommandCode { get; }

        public BaseResponse(CommandCode code)
        {
            CommandCode = code;
        }

        public abstract byte[] GetBytes();
    }
}
