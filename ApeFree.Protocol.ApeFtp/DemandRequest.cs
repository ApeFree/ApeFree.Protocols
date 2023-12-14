using System;
using System.Collections.Generic;
using System.Linq;

namespace ApeFree.Protocol.ApeFtp
{
    /// <summary>
    /// 申请请求
    /// </summary>
    public class DemandRequest : BaseRequest
    {
        /// <summary>
        /// 最大段长度
        /// </summary>
        public uint SegmentMaxLength { get; set; }

        public DemandRequest(byte[] data) : base(CommandCode.DemandRequest, data.Skip(1).Take(16).ToArray(), BitConverter.ToUInt32(data.Skip(17).Take(4).Reverse().ToArray(), 0))
        {
            SegmentMaxLength = BitConverter.ToUInt32(data.Skip(21).Take(4).Reverse().ToArray(), 0);
        }

        public DemandRequest(byte[] md5, uint totalLength, uint segmentMaxLength) : base(CommandCode.DemandRequest, md5, totalLength)
        {
            SegmentMaxLength = segmentMaxLength;
        }

        public override byte[] GetBytes()
        {
            var bytes =  new byte[] { (byte)CommandCode }.Merge(
                                    MD5,
                                    BitConverter.GetBytes(TotalLength).Reverse(),
                                    BitConverter.GetBytes(SegmentMaxLength).Reverse()
                                ).ToArray();
            return bytes;
        }
    }
}
