using System;
using System.Linq;

namespace ApeFree.Protocols.Json.Jbin
{
    //public class JbinObjectConverter : JbinSerializer<object>
    //{
    //    protected override object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType)
    //    {
    //        var converter = GetConverter(realType);
    //        if (converter != null)
    //        {
    //            return converter.ConvertBytesToObject(bytes, defineType, realType);
    //        }
    //        else
    //        {
    //            return null;
    //        }
    //    }

    //    protected override byte[] ConvertValueToBytes(object value)
    //    {
    //        return GetConverter(value.GetType()).ConvertObjectToBytes(value);
    //    }

    //    private JbinConverter GetConverter(Type type)
    //    {
    //        return (JbinConverter)Settings.Converters.Where(x => x is JbinConverter && x != this).FirstOrDefault(x => x.CanConvert(type));
    //    }
    //}
}
