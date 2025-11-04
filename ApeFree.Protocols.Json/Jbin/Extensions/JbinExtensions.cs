using STTech.CodePlus.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ApeFree.Protocols.Json.Jbin.Extensions
{
    public static class JbinExtensions
    {
        /// <summary>
        /// 将对象集合转置为属性字典
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="propertyFilter">属性过滤器</param>
        /// <returns></returns>
        public static Dictionary<string, Array> TransposeToDictionary<T>(this IEnumerable<T> list, Func<PropertyInfo, bool> propertyFilter = null) where T : class
        {
            // 获取所有可读写的属性
            var props = typeof(T).GetProperties().Where(p => p.CanWrite && p.CanRead).ToArray();

            if (propertyFilter != null)
            {
                props = props.Where(x => propertyFilter(x)).ToArray();
            }

            // 创建结果字典
            var table = new Dictionary<string, Array>();

            // 遍历所有属性
            foreach (var pi in props)
            {
                var propName = pi.Name;
                var array = list.ToArray();
                var propValues = Array.CreateInstance(pi.PropertyType, array.Length);

                for (int i = 0; i < array.Length; i++)
                {
                    var cp = array[i];
                    var pv = pi.GetValue(cp);
                    propValues.SetValue(pv, i);
                }
                table[propName] = propValues;
            }

            return table;
        }

        /// <summary>
        /// 将属性字典转置为对象数组
        /// </summary>
        /// <typeparam name="T">缺陷类型</typeparam>
        /// <param name="dict"></param>
        /// <param name="propertyFilter">属性过滤器</param>
        /// <returns></returns>
        public static T[] TransposeFromDictionary<T>(this Dictionary<string, Array> dict, Func<PropertyInfo, bool> propertyFilter = null)
        {
            if (dict == null)
            {
                return null;
            }

            if (dict.Count == 0)
            {
                return new T[0];
            }

            // 获取所有可读写的属性
            var props = typeof(T).GetProperties().Where(p => p.CanWrite && p.CanRead).ToArray();

            if (propertyFilter != null)
            {
                props = props.Where(x => propertyFilter(x)).ToArray();
            }

            int len = dict.Values.First().Length;
            var array = (T[])Array.CreateInstance(typeof(T), len);

            // 初始化对象（如果 T 是引用类型且有无参构造函数）
            if (typeof(T).IsClass)
            {
                for (int i = 0; i < len; i++)
                {
                    array[i] = Activator.CreateInstance<T>();
                }
            }

            // 外层循环数据 → 内层循环属性
            for (int i = 0; i < len; i++)
            {
                object item = array[i];
                foreach (var prop in props)
                {
                    if (!dict.TryGetValue(prop.Name, out Array sourceArray))
                    {
                        continue;
                    }

                    var setter = _settersCache.GetOrAdd(prop, CreateSetter);
                    var value = sourceArray.GetValue(i);

                    // 如果`value`是值类型就直接赋值，如果是引用类型就拷贝后赋值
                    if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string))
                    {
                        setter(item, value);
                    }
                    else
                    {
                        setter(item, ObjectUtils.DeepCopy(value));
                    }
                }
            }

            return array;
        }

        #region 使用表达式树构造赋值委托以替代反射SetValue强制赋值，提升性能
        private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object>> _settersCache = new ConcurrentDictionary<PropertyInfo, Action<object, object>>();
        private static Action<object, object> CreateSetter(PropertyInfo prop)
        {
            var objParam = Expression.Parameter(typeof(object));
            var valueParam = Expression.Parameter(typeof(object));
            var castObj = Expression.Convert(objParam, prop.DeclaringType);
            var castValue = Expression.Convert(valueParam, prop.PropertyType);
            var body = Expression.Call(castObj, prop.SetMethod, castValue);
            return Expression.Lambda<Action<object, object>>(body, objParam, valueParam).Compile();
        }
        #endregion
    }

    public static class ObjectUtils
    {
        /// <summary>
        /// 深度拷贝对象
        /// </summary>
        /// <param name="obj">要拷贝的对象</param>
        /// <returns>拷贝后的新对象</returns>
        public static object DeepCopy(object obj)
        {
            if (obj == null)
                return null;

            Type type = obj.GetType();

            // 处理字符串和值类型（包括可空类型）
            if (type.IsValueType || type == typeof(string))
                return obj;

            // 处理数组
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                Array array = obj as Array;
                Array copiedArray = Array.CreateInstance(elementType, array.Length);

                for (int i = 0; i < array.Length; i++)
                {
                    copiedArray.SetValue(DeepCopy(array.GetValue(i)), i);
                }
                return copiedArray;
            }

            // 处理引用类型
            if (type.IsClass)
            {
                // 尝试使用序列化（如果对象标记为[Serializable]）
                if (type.IsSerializable)
                {
                    try
                    {
                        using (var memoryStream = new System.IO.MemoryStream())
                        {
                            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                            formatter.Serialize(memoryStream, obj);
                            memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                            return formatter.Deserialize(memoryStream);
                        }
                    }
                    catch
                    {
                        // 如果序列化失败，回退到反射方式
                        return CopyUsingReflection(obj, type);
                    }
                }
                else
                {
                    // 使用反射进行拷贝
                    return CopyUsingReflection(obj, type);
                }
            }

            return null;
        }

        private static object CopyUsingReflection(object obj, Type type)
        {
            //object copy = Activator.CreateInstance(type);
            //var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            //                    .Where(p => p.CanRead && p.CanWrite);

            //foreach (var prop in properties)
            //{
            //    object value = prop.GetValue(obj);
            //    if (value != null)
            //    {
            //        prop.SetValue(copy, DeepCopy(value));
            //    }
            //}

            var jbin = JbinObject.FromObject(obj);
            var copy = jbin.ToObject(type);

            return copy;
        }
    }
}
