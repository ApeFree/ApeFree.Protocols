using STTech.BytesIO.Core.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ApeFree.Protocol.ApeFtp
{
    public class ApeFtpUnpacker : Unpacker
    {
        protected override int CalculatePacketLength(byte[] bytes)
        {
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
                    // TODO:
                    return 0;
            }

        }
    }
}
