using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// 泛型数组/列表转换器 (处理可被其它转换器执行的容器)
    /// <para>1. 当容器中的元素不是简单的基元类型，而也是需要 Jbin 特殊转换的对象时（例如：<c>List&lt;Point&gt;</c> 或 <c>List&lt;byte[]&gt;</c>），本类将被激活。</para>
    /// <para>2. 它实现了递归转换逻辑：它通过 <see cref="FieldConverters"/> 查找最适合元素类型的具体子转换器。</para>
    /// <para>3. 每个元素先由其对应的具体转换器转为字节，然后再由本类统一打包，体现了 Jbin 对嵌套数据结构的强大支持。</para>
    /// </summary>
    public class JbinGenericArrayConverter : JbinSerializer<object>, IJbinFieldConverter
    {
        private IEnumerable<IJbinFieldConverter> FieldConverters => Context?.Settings?.Converters?.OfType<IJbinFieldConverter>() ?? Enumerable.Empty<IJbinFieldConverter>();

        public bool CanDeserialize(Type defineType, Type realType)
        {
            var elementType = GetElementType(realType);

            if (elementType == null)
            {
                return false;
            }
            else
            {
                return FieldConverters.Any(x => x.CanDeserialize(elementType, elementType));
            }
        }

        public override bool CanSerialize(Type realType)
        {
            var elementType = GetElementType(realType);

            if (elementType == null)
            {
                return false;
            }
            else
            {
                return FieldConverters.Any(x => x.CanSerialize(elementType));
            }
        }

        private Type GetElementType(Type type)
        {
            Type elementType;
            if (type.IsArray)
            {
                elementType = type.GetElementType();
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type[] genericArgs = type.GetGenericArguments();
                if (genericArgs.Length == 1)
                {
                    elementType = genericArgs.First();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                // 如果类型既不是Array也不是List则跳过
                return null;
            }

            return elementType;
        }

        public object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType)
        {
            Dictionary<Type, IJbinFieldConverter> dictConverter = new Dictionary<Type, IJbinFieldConverter>();

            var elementType = GetElementType(realType);

            object group = CreateList(elementType);

            using (var ms = new MemoryStream(bytes))
            {
                using (var br = new BinaryReader(ms))
                {
                    var size = br.ReadInt32();
                    for (int i = 0; i < size; i++)
                    {
                        var blockLen = br.ReadInt32();

                        if (blockLen == -1)
                        {
                            AddDataToList(group, null);
                        }
                        else
                        {
                            var block = br.ReadBytes(blockLen);

                            IJbinFieldConverter converter;
                            if (!dictConverter.TryGetValue(elementType, out converter))
                            {
                                converter = FieldConverters.First(x => x.CanDeserialize(elementType, elementType));
                                dictConverter[elementType] = converter;
                            }

                            var item = converter.ConvertBytesToValue(block, elementType, elementType);
                            AddDataToList(group, item);
                        }
                    }
                }
            }

            if (realType.IsArray)
            {
                return ListToArray(group);
            }
            else
            {
                return group;
            }
        }



        public override byte[] ConvertValueToBytes(Type type, object value)
        {
            Dictionary<Type, IJbinFieldConverter> dictConverter = new Dictionary<Type, IJbinFieldConverter>();
            Type elementType;
            var group = new List<object>();
            if (type.IsArray)
            {
                var array = value as Array;
                foreach (var item in array)
                {
                    group.Add(item);
                }
                elementType = type.GetElementType();
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var array = value as IList;
                foreach (var item in array)
                {
                    group.Add(item);
                }
                elementType = type.GetGenericArguments().FirstOrDefault();
            }
            else
            {
                return null;
            }

            using (var ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(group.Count);
                    foreach (var block in group)
                    {
                        if (block == null)
                        {
                            bw.Write(-1);
                        }
                        else
                        {
                            // 选择一个转换器
                            IJbinFieldConverter converter;
                            if (!dictConverter.TryGetValue(elementType, out converter))
                            {
                                converter = FieldConverters.First(x => x.CanSerialize(elementType));
                                dictConverter[elementType] = converter;
                            }
                          
                            var data = converter.ConvertValueToBytes(block);
                            bw.Write(data.Length);
                            bw.Write(data);
                        }
                    }
                }
                return ms.ToArray();
            }
        }

        #region 反射列表操作
        private static object CreateList(Type elementType)
        {
            // 获取 List<> 的泛型类型定义
            Type listType = typeof(List<>).GetGenericTypeDefinition();

            // 构建具体的泛型类型
            Type specificListType = listType.MakeGenericType(elementType);

            // 创建实例
            return Activator.CreateInstance(specificListType);
        }

        private static void AddDataToList(object list, object data)
        {
            if (list == null)
            {
                return;
            }

            // 获取 Add 方法
            MethodInfo addMethod = list.GetType().GetMethod("Add");
            if (addMethod != null)
            {
                // 调用 Add 方法添加数据
                addMethod.Invoke(list, new object[] { data });
            }
        }

        private static object ListToArray(object list)
        {
            if (list == null)
            {
                return null;
            }

            // 获取 ToArray 方法
            MethodInfo toArrayMethod = list.GetType().GetMethod("ToArray");
            if (toArrayMethod != null)
            {
                // 调用 ToArray 方法将列表转换为数组
                return toArrayMethod.Invoke(list, null);
            }
            return null;
        }
        #endregion
    }
}
