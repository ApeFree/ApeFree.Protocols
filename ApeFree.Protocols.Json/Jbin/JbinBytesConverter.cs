using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ApeFree.Protocols.Json.Jbin
{

    public class JbinBytesConverter : JbinConverter<byte[]>
    {
        protected override byte[] ConvertBytesToValue(byte[] bytes, Type objType)
        {
            return bytes;
        }

        protected override byte[] ConvertValueToBytes(byte[] value)
        {
            return value;
        }
    }
}
