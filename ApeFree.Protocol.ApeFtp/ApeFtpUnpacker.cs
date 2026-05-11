using STTech.BytesIO.Core.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Buffers;

namespace ApeFree.Protocol.ApeFtp
{
    public class ApeFtpUnpacker : Unpacker<TransferResponse>
    {
        protected override int CalculatePacketLength(ReadOnlySequence<byte> buffer)
        {
            var bytes = buffer.ToArray();
            if (bytes.Length < 23)
            {
                return 0;
            }

            var code = (CommandCode)bytes.ElementAt(0);

            switch (code)
            {
                case CommandCode.DemandRequest:
                    return 25;
                case CommandCode.TransferRequest:
                    var len = 30 + (int)BitConverter.ToUInt32(bytes.Skip(26).Take(4).Reverse().ToArray(), 0);
                    return len;
                case CommandCode.TransferResponse:
                    return 23 + bytes.ElementAt(22);
                default:
                    return 0;
            }
        }

        protected override TransferResponse ResponseSerializeHandler(UnpackContext context)
        {
            var bytes = context.Data.ToArray();
            var code = (CommandCode)bytes.ElementAt(0);
            if (code == CommandCode.TransferResponse)
            {
                return new TransferResponse(bytes);
            }
            return null;
        }
    }
}
