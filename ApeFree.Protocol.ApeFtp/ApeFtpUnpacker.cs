using STTech.BytesIO.Core.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ApeFree.Protocol.ApeFtp
{
    internal class ApeFtpUnpacker : Unpacker
    {
        protected override int CalculatePacketLength(byte[] bytes)
        {
         
            if (bytes.Length < 25)
            {
                return 0;
            }

            var code = (CommandCode)bytes.ElementAt(0);

            switch (code)
            {
                case CommandCode.DemandRequest:
                    return 25;
                case CommandCode.TransferRequest:
                    return 30+ (int)BitConverter.ToUInt32(bytes.Skip(27).Take(4).Reverse().ToArray(), 0);
                case CommandCode.TransferResponse:
                    return 23 + bytes.ElementAt(23);
                default:
                    return 0;
            }

        }
    }
}
