using STTech.BytesIO.Core;
using System;
using System.Collections.Generic;

namespace ApeFree.Protocol.ApeFtp
{
    /// <summary>
    /// 基础请求类
    /// </summary>
    public abstract class BaseRequest : IRequest
    {
        /// <summary>
        /// 命令码
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
