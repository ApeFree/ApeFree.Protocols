using System;
using System.Linq;

namespace ApeFree.Protocols.Json.Jbin
{
    public class JbinObjectConverter : JbinConverter<object>
    {
        protected override object ConvertBytesToValue(byte[] bytes, Type objectType)
        {
            var converter = GetConverter(objectType);
            if (converter != null)
            {
                return converter.ConvertBytesToObject(bytes, objectType);
            }
            else
            {
                return null;
            }
        }

        protected override byte[] ConvertValueToBytes(object value)
        {
            return GetConverter(value.GetType()).ConvertObjectToBytes(value);
        }

        private JbinConverter GetConverter(Type type)
        {
            return (JbinConverter)Settings.Converters.Where(x => x is JbinConverter && x != this).FirstOrDefault(x => x.CanConvert(type));
        }
    }
}
