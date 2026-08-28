using ApeFree.Protocols.Json.Jbin.Attributes;
using Newtonsoft.Json;
using STTech.CodePlus.Utils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ApeFree.Protocols.Json.Jbin.Extensions
{
    public static class JbinExtensions
    {
        #region 单对象集合行列转置

        /// <summary>
        /// 将对象集合转置为属性字典（列式存储）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="list">数据源列表</param>
        /// <param name="propertyFilter">自定义属性过滤器</param>
        /// <returns>属性名与对应列数组的字典</returns>
        public static Dictionary<string, Array> TransposeToDictionary<T>(this IEnumerable<T> list, Func<PropertyInfo, bool> propertyFilter = null) where T : class
        {
            if (list == null) return null;

            var props = typeof(T).GetProperties()
                .Where(p => p.CanWrite && p.CanRead)
                .Where(p => p.GetCustomAttribute<JbinIgnoreAttribute>() == null)
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null);

            if (propertyFilter != null)
            {
                props = props.Where(propertyFilter);
            }

            return TransposeToDictionary(list, props.ToArray());
        }

        /// <summary>
        /// 将对象集合转置为属性字典（列式存储，指定属性信息）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="list">数据源列表</param>
        /// <param name="props">指定的属性信息数组</param>
        /// <returns>属性名与对应列数组的字典</returns>
        public static Dictionary<string, Array> TransposeToDictionary<T>(this IEnumerable<T> list, PropertyInfo[] props) where T : class
        {
            if (list == null) return null;

            if (props == null)
            {
                props = typeof(T).GetProperties()
                    .Where(p => p.CanWrite && p.CanRead)
                    .Where(p => p.GetCustomAttribute<JbinIgnoreAttribute>() == null)
                    .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                    .ToArray();
            }

            var items = list as IList<T> ?? list.ToArray();
            var count = items.Count;

            var table = new Dictionary<string, Array>(props.Length);

            foreach (var pi in props)
            {
                var propName = pi.Name;
                var propValues = Array.CreateInstance(pi.PropertyType, count);
                var getter = _gettersCache.GetOrAdd(pi, CreateGetter);

                for (int i = 0; i < count; i++)
                {
                    var cp = items[i];
                    var pv = cp != null ? getter(cp) : null;
                    propValues.SetValue(pv, i);
                }
                table[propName] = propValues;
            }

            return table;
        }

        /// <summary>
        /// 将属性字典转置为对象数组
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="dict">属性列字典</param>
        /// <param name="propertyFilter">自定义属性过滤器</param>
        /// <returns>对象数组</returns>
        public static T[] TransposeFromDictionary<T>(this Dictionary<string, Array> dict, Func<PropertyInfo, bool> propertyFilter = null)
        {
            if (dict == null) return null;
            if (dict.Count == 0) return new T[0];

            var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.CanWrite)
                .Where(p => p.GetCustomAttribute<JbinIgnoreAttribute>() == null)
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null);

            if (propertyFilter != null)
            {
                props = props.Where(propertyFilter);
            }

            return TransposeFromDictionary<T>(dict, props.ToArray());
        }

        /// <summary>
        /// 将属性字典转置为对象数组（指定属性信息）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="dict">属性列字典</param>
        /// <param name="props">指定的属性信息数组</param>
        /// <returns>对象数组</returns>
        public static T[] TransposeFromDictionary<T>(this Dictionary<string, Array> dict, PropertyInfo[] props)
        {
            if (dict == null) return null;
            if (dict.Count == 0) return new T[0];

            if (props == null)
            {
                props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(p => p.CanWrite)
                    .Where(p => p.GetCustomAttribute<JbinIgnoreAttribute>() == null)
                    .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                    .ToArray();
            }

            int len = dict.Values.First().Length;
            var array = (T[])Array.CreateInstance(typeof(T), len);

            if (typeof(T).IsClass)
            {
                for (int i = 0; i < len; i++)
                {
                    array[i] = Activator.CreateInstance<T>();
                }
            }

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

                    if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string) || value == null)
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

        #endregion

        #region 多通道 / 分组嵌套字典转置

        /// <summary>
        /// 将分组对象列表字典转置为分组列式属性字典
        /// </summary>
        /// <typeparam name="K">分组键类型</typeparam>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="dict">多分组数据源</param>
        /// <param name="propertyFilter">属性过滤器</param>
        /// <returns>多分组列式属性字典</returns>
        public static Dictionary<K, Dictionary<string, Array>> Transpose<K, T>(this Dictionary<K, List<T>> dict, Func<PropertyInfo, bool> propertyFilter = null) where T : class
        {
            if (dict == null) return null;

            var props = typeof(T).GetProperties()
                .Where(p => p.CanWrite && p.CanRead)
                .Where(p => p.GetCustomAttribute<JbinIgnoreAttribute>() == null)
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null);

            if (propertyFilter != null)
            {
                props = props.Where(propertyFilter);
            }

            var propArray = props.ToArray();
            var result = new Dictionary<K, Dictionary<string, Array>>(dict.Count);

            foreach (var kvp in dict)
            {
                result[kvp.Key] = kvp.Value?.TransposeToDictionary(propArray);
            }

            return result;
        }

        /// <summary>
        /// 将分组列式属性字典转置还原为分组对象列表字典
        /// </summary>
        /// <typeparam name="K">分组键类型</typeparam>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="dict">多分组列式数据</param>
        /// <param name="propertyFilter">属性过滤器</param>
        /// <returns>多分组实体列表字典</returns>
        public static Dictionary<K, List<T>> Transpose<K, T>(this Dictionary<K, Dictionary<string, Array>> dict, Func<PropertyInfo, bool> propertyFilter = null) where T : class
        {
            if (dict == null) return null;

            var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.CanWrite)
                .Where(p => p.GetCustomAttribute<JbinIgnoreAttribute>() == null)
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null);

            if (propertyFilter != null)
            {
                props = props.Where(propertyFilter);
            }

            var propArray = props.ToArray();
            var result = new Dictionary<K, List<T>>(dict.Count);

            foreach (var kvp in dict)
            {
                result[kvp.Key] = kvp.Value?.TransposeFromDictionary<T>(propArray)?.ToList();
            }

            return result;
        }

        #endregion

        #region 动态弱类型字典列表转置

        /// <summary>
        /// 将字典列表转置为列式存储（自动合并各字典的 Key）
        /// </summary>
        /// <param name="list">字典数据源</param>
        /// <returns>列存储字典</returns>
        public static Dictionary<string, object[]> TransposeDictionariesToDictionary(this IEnumerable<Dictionary<string, object>> list)
        {
            if (list == null) return null;

            var items = list as IList<Dictionary<string, object>> ?? list.ToList();
            var rowCount = items.Count;

            var table = new Dictionary<string, object[]>();
            if (rowCount == 0) return table;

            var allKeys = new HashSet<string>();
            foreach (var dict in items)
            {
                if (dict != null)
                {
                    foreach (var key in dict.Keys)
                    {
                        allKeys.Add(key);
                    }
                }
            }

            foreach (var key in allKeys)
            {
                table[key] = new object[rowCount];
            }

            for (int i = 0; i < rowCount; i++)
            {
                var currentDict = items[i];
                if (currentDict == null) continue;

                foreach (var key in allKeys)
                {
                    if (currentDict.TryGetValue(key, out var value))
                    {
                        table[key][i] = value;
                    }
                }
            }

            return table;
        }

        /// <summary>
        /// 将列式存储还原为字典列表
        /// </summary>
        /// <param name="dict">列式数据字典</param>
        /// <param name="ignoreNullValues">是否忽略 null 值</param>
        /// <returns>字典列表</returns>
        public static List<Dictionary<string, object>> TransposeDictionariesFromDictionary(this Dictionary<string, object[]> dict, bool ignoreNullValues = true)
        {
            if (dict == null) return null;
            if (dict.Count == 0) return new List<Dictionary<string, object>>();

            int rowCount = dict.Values.First().Length;
            var list = new List<Dictionary<string, object>>(rowCount);

            for (int i = 0; i < rowCount; i++)
            {
                var rowDict = new Dictionary<string, object>();

                foreach (var kvp in dict)
                {
                    string colName = kvp.Key;
                    object[] colData = kvp.Value;
                    object value = colData != null && i < colData.Length ? colData[i] : null;

                    if (value == null && ignoreNullValues)
                    {
                        continue;
                    }

                    if (value != null && !value.GetType().IsValueType && !(value is string))
                    {
                        rowDict[colName] = ObjectUtils.DeepCopy(value);
                    }
                    else
                    {
                        rowDict[colName] = value;
                    }
                }

                list.Add(rowDict);
            }

            return list;
        }

        #endregion

        #region 表达式树 Getter / Setter 编译与缓存
        private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object>> _gettersCache = new ConcurrentDictionary<PropertyInfo, Func<object, object>>();
        private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object>> _settersCache = new ConcurrentDictionary<PropertyInfo, Action<object, object>>();

        private static Func<object, object> CreateGetter(PropertyInfo prop)
        {
            var objParam = Expression.Parameter(typeof(object), "obj");
            var castObj = Expression.Convert(objParam, prop.DeclaringType);
            var propAccess = Expression.Property(castObj, prop);
            var castResult = Expression.Convert(propAccess, typeof(object));
            return Expression.Lambda<Func<object, object>>(castResult, objParam).Compile();
        }

        private static Action<object, object> CreateSetter(PropertyInfo prop)
        {
            var objParam = Expression.Parameter(typeof(object), "obj");
            var valueParam = Expression.Parameter(typeof(object), "val");
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

            if (obj is string str)
                return str;

            Type type = obj.GetType();

            if (type.IsValueType)
                return obj;

            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                if (elementType != null && (elementType.IsValueType || elementType == typeof(string)))
                {
                    return ((Array)obj).Clone();
                }

                Array array = obj as Array;
                Array copiedArray = Array.CreateInstance(elementType, array.Length);

                for (int i = 0; i < array.Length; i++)
                {
                    copiedArray.SetValue(DeepCopy(array.GetValue(i)), i);
                }
                return copiedArray;
            }

            if (obj is IList list)
            {
                IList copiedList = (IList)Activator.CreateInstance(type);
                foreach (var item in list)
                {
                    copiedList.Add(DeepCopy(item));
                }
                return copiedList;
            }

            var jbin = JbinObject.FromObject(obj);
            var copy = jbin.ToObject(type);
            return copy;
        }
    }
}
