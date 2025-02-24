using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ApeFree.Protocols.Json.Jbin
{
    public class JbinGenericArrayConverter : JbinSerializer<object>, IJbinFieldConverter
    {
        private static readonly IJbinFieldConverter[] Converters = new IJbinFieldConverter[]
        {
            new JbinGenericArrayConverter(),
            new JbinBytesConverter(),
            new JbinGenericStructConverter(),
        };

        public bool CanDeserialize(Type defineType, Type realType)
        {
            var elementType = GetElementType(realType);

            if (elementType == null)
            {
                return false;
            }
            else
            {
                return Converters.Any(x => x.CanDeserialize(elementType, elementType));
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
                return Converters.Any(x => x.CanSerialize(elementType));
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

                            // 选择一个转换器
                            var converter = Converters.First(x => x.CanDeserialize(elementType, elementType));
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

        public override byte[] ConvertValueToBytes(object value)
        {
            var type = value.GetType();
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
                            var converter = Converters.First(x => x.CanSerialize(elementType));
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
