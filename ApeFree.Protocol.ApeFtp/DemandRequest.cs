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
        public byte[] MD5 { get; set; }
        public uint TotalLength { get; set; }
        public uint SegmentMaxLength { get; set; }

        public DemandRequest() : base(CommandCode.DemandRequest) { }
        public DemandRequest(IEnumerable<byte> data) : base(CommandCode.DemandRequest)
        {
            List<byte> bytes = data.ToList();
            MD5 = bytes.Skip(1).Take(16).ToArray();
            TotalLength = BitConverter.ToUInt32(bytes.Skip(17).Take(4).Reverse().ToArray(), 0);
            SegmentMaxLength = BitConverter.ToUInt32(bytes.Skip(21).Take(4).Reverse().ToArray(), 0);
        }

        public override byte[] GetBytes()
        {
            return new byte[] { (byte)CommandCode }.Merge(
                                    MD5,
                                    BitConverter.GetBytes(TotalLength).Reverse(),
                                    BitConverter.GetBytes(SegmentMaxLength).Reverse()
                                ).ToArray();
        }
    }
}
