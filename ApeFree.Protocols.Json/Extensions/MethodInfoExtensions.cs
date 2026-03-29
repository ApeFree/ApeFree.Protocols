using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace System.Reflection
{
    public static class MethodInfoExtensions
    {

        public static List<object> GetAllCustomAttributes(this MethodInfo methodInfo)
        {
            List<object> attrs = new List<object>();

            attrs.AddRange(methodInfo.GetCustomAttributes(true));

            // 如果是属性访问器
            if (methodInfo.Name.StartsWith("get_") || methodInfo.Name.StartsWith("set_"))
            {
                var pi = methodInfo.DeclaringType.GetProperty(methodInfo.Name.Substring(4));
                attrs.AddRange(pi.GetCustomAttributes());
            }

            var mi = methodInfo.DeclaringType.GetMethod(methodInfo.Name);
            attrs.AddRange(mi.GetCustomAttributes());

            return attrs;
        }
    }
}
