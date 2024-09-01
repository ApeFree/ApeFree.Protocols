using System;
using System.Collections.Generic;

namespace ApeFree.Protocols.Json.Jbin
{
    public class JbinStringConverter : JbinConverter<string>
    {
        protected override string ConvertBytesToValue(byte[] bytes, Type objectType)
        {
            return bytes.EncodeToString();
        }

        protected override byte[] ConvertValueToBytes(string value)
        {
            return value.GetBytes();
        }
    }
}
