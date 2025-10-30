using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// MethodInfo 扩展方法
/// </summary>
public static class MethodInfoExtensions
{
    /// <summary>
    /// 获取方法的所有自定义属性（包括属性访问器和方法本身的属性）
    /// </summary>
    /// <param name="methodInfo"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 获取方法的所有指定类型的自定义属性（包括属性访问器和方法本身的属性）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="methodInfo"></param>
    /// <returns></returns>
    public static T[] GetAllCustomAttributes<T>(this MethodInfo methodInfo) where T : Attribute
    {
        var attrs = methodInfo.GetAllCustomAttributes().OfType<T>().ToArray();
        return attrs;
    }
}
