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
        public FuncCode Opcode { get; set; }
        public ushort SegmentCount { get; set; }
        public ushort SegmentIndex { get; set; }
        public uint CurrentSegmentLength { get; set; }
        public IEnumerable<byte> Data { get; set; }

        public TransferRequest(byte[] mD5,uint totalLength) : base(CommandCode.TransferRequest, mD5, totalLength) 
        {
        }
        public TransferRequest(IEnumerable<byte> bytes) : base(CommandCode.TransferRequest, bytes.Skip(1).Take(16).ToArray(), BitConverter.ToUInt32(bytes.Skip(17).Take(4).Reverse().ToArray(),0))
        {
            
            Opcode = (FuncCode)bytes.ElementAt(22);
            SegmentCount = BitConverter.ToUInt16(bytes.Skip(23).Take(2).Reverse().ToArray(),0);
            SegmentIndex = BitConverter.ToUInt16(bytes.Skip(25).Take(2).Reverse().ToArray(),0);
            CurrentSegmentLength = BitConverter.ToUInt32(bytes.Skip(27).Take(4).Reverse().ToArray(),0);
            Data = bytes.Skip(31).Take((int)CurrentSegmentLength);
        }

        public override byte[] GetBytes()
        {
            return new byte[] { (byte)CommandCode }.Merge(
                                    MD5,
                                      BitConverter.GetBytes(TotalLength).Reverse(),
                                    new byte[] { (byte)Opcode },
                                    BitConverter.GetBytes(SegmentCount).Reverse(),
                                    BitConverter.GetBytes(SegmentIndex).Reverse(),
                                    BitConverter.GetBytes(CurrentSegmentLength).Reverse(),
                                    Data??new List<byte>()
                                ).ToArray();
        }
    }
}
