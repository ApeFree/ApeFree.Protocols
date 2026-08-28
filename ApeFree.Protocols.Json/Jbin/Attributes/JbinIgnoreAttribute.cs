using System;

namespace ApeFree.Protocols.Json.Jbin.Attributes
{
    /// <summary>
    /// Jbin序列化或列式转置时忽略该属性或字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class JbinIgnoreAttribute : Attribute
    {
    }
}
