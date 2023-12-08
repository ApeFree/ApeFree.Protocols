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
        public byte[] MD5 { get; set; }
        public uint TotalLength { get; set; }
        public FuncCode Opcode { get; set; }
        public ushort SegmentCount { get; set; }
        public ushort SegmentIndex { get; set; }
        public uint CurrentSegmentLength { get; set; }
        public IEnumerable<byte> Data { get; set; }

        public TransferRequest() : base(CommandCode.TransferRequest) { }
        public TransferRequest(IEnumerable<byte> bytes) : base(CommandCode.TransferRequest)
        {
            MD5 = bytes.Skip(1).Take(16);
            Opcode = (FuncCode)bytes.ElementAt(17);
            SegmentCount = BitConverter.ToUInt16(bytes.Skip(18).Take(2).Reverse().ToArray(),0);
            SegmentIndex = BitConverter.ToUInt16(bytes.Skip(20).Take(2).Reverse().ToArray(),0);
            CurrentSegmentLength = BitConverter.ToUInt32(bytes.Skip(22).Take(4).Reverse().ToArray(),0);
            Data = bytes.Skip(26).Take((int)CurrentSegmentLength);
        }

        public override byte[] GetBytes()
        {
            return new byte[] { (byte)CommandCode }.Merge(
                                    MD5,
                                    new byte[] { (byte)Opcode },
                                    BitConverter.GetBytes(SegmentCount).Reverse(),
                                    BitConverter.GetBytes(SegmentIndex).Reverse(),
                                    BitConverter.GetBytes(CurrentSegmentLength).Reverse(),
                                    Data??new List<byte>()
                                ).ToArray();
        }
    }
}
