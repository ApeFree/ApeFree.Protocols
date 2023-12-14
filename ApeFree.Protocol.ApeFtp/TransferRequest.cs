using System;
using System.Collections.Generic;
using System.Linq;

namespace ApeFree.Protocol.ApeFtp
{
    /// <summary>
    /// 传输请求
    /// </summary>
    public class TransferRequest : BaseRequest
    {
        /// <summary>
        /// 功能码
        /// </summary>
        public FunctionCode FunctionCode { get; set; }

        /// <summary>
        /// 总段数
        /// </summary>
        public ushort SegmentCount { get; set; }

        /// <summary>
        /// 当前段序号
        /// </summary>
        public ushort SegmentIndex { get; set; }

        /// <summary>
        /// 当前段长度
        /// </summary>
        public uint CurrentSegmentLength { get; set; }

        /// <summary>
        /// 段数据
        /// </summary>
        public byte[] Data { get; set; }

        public TransferRequest(byte[] md5, uint totalLength) : base(CommandCode.TransferRequest, md5, totalLength) { }

        public TransferRequest(IEnumerable<byte> bytes) : base(CommandCode.TransferRequest, bytes.Skip(1).Take(16).ToArray(), BitConverter.ToUInt32(bytes.Skip(17).Take(4).Reverse().ToArray(), 0))
        {
            FunctionCode = (FunctionCode)bytes.ElementAt(21);
            SegmentCount = BitConverter.ToUInt16(bytes.Skip(22).Take(2).Reverse().ToArray(), 0);
            SegmentIndex = BitConverter.ToUInt16(bytes.Skip(24).Take(2).Reverse().ToArray(), 0);
            CurrentSegmentLength = BitConverter.ToUInt32(bytes.Skip(26).Take(4).Reverse().ToArray(), 0);
            Data = bytes.Skip(30).Take((int)CurrentSegmentLength).ToArray();
        }

        public override byte[] GetBytes()
        {
            return new byte[] { (byte)CommandCode }.Merge(
                                    MD5,
                                      BitConverter.GetBytes(TotalLength).Reverse(),
                                    new byte[] { (byte)FunctionCode },
                                    BitConverter.GetBytes(SegmentCount).Reverse(),
                                    BitConverter.GetBytes(SegmentIndex).Reverse(),
                                    BitConverter.GetBytes(CurrentSegmentLength).Reverse(),
                                    Data ?? new byte[0]
                                ).ToArray();
        }
    }
}
