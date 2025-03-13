using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ApeFree.Protocols.Json.Jbin
{
    public class TypeExtensions
    {
        private static readonly ConcurrentDictionary<string, Type> KnownTypes = new ConcurrentDictionary<string, Type>();

        public static Type GetType(string typeFullName)
        {
            var type = KnownTypes.GetOrAdd(typeFullName, ParseType(typeFullName));
            return type;
        }

        public static Type ParseType(string typeName)
        {
            // 预处理外层方括号
            var trimmed = typeName.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                return GetType(trimmed.Substring(1, trimmed.Length - 2));

            // 分离程序集信息和类型定义
            var tuple = SplitFullyQualifiedTypeName(trimmed);

            // 分解泛型、数组和嵌套类型
            var result = ParseTypeDefinition(tuple.Item2, tuple.Item1);

            // 缓存类型
            if (result != null)
            {
                KnownTypes[typeName] = result;
            }

            return result;
        }

        private static Type ParseTypeDefinition(string typeDef, string? assemblyName)
        {
            // 分解数组维度
            var arraySuffix = "";
            while (typeDef.EndsWith("[]"))
            {
                arraySuffix += "[]";
                typeDef = typeDef.Substring(0, typeDef.Length - 2);
            }

            // 分解泛型参数
            Type baseType;
            var genericMarkerIndex = typeDef.IndexOf('`');
            if (genericMarkerIndex > 0)
            {
                var argsStart = typeDef.IndexOf('[', genericMarkerIndex);
                var typeName = typeDef.Substring(0, argsStart);
                baseType = GetBaseType(typeName, assemblyName);


                var argsString = typeDef.Substring(argsStart + 1, typeDef.Length - argsStart - 2);
                var genericArgs = ParseGenericArguments(argsString);

                baseType = baseType.MakeGenericType(genericArgs);
            }
            else
            {
                baseType = GetBaseType(typeDef, assemblyName);
            }

            // 创建数组类型
            return CreateArrayType(baseType, arraySuffix);
        }

        private static Type CreateArrayType(Type elementType, string arraySuffix)
        {
            if (string.IsNullOrEmpty(arraySuffix)) return elementType;

            var rank = arraySuffix.Length / 2;
            return rank == 1
                ? elementType.MakeArrayType()
                : elementType.MakeArrayType(rank);
        }

        private static Type GetBaseType(string typeName, string? assemblyName)
        {
            // 尝试默认解析（含程序集上下文）
            var fullName = assemblyName != null
                ? $"{typeName}, {assemblyName}"
                : typeName;

            if (Type.GetType(fullName, throwOnError: false) is Type t1 && t1 != null)
                return t1;

            // 显式加载程序集
            if (assemblyName != null)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    if (assembly.GetType(typeName) is Type t2 && t2 != null)
                        return t2;
                }
                catch { /* 忽略加载错误 */ }
            }

            // 搜索已加载程序集
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType(typeName) is Type t3 && t3 != null)
                    return t3;
            }

            throw new TypeLoadException($"Could not load type '{typeName}'");
        }

        private static Type[] ParseGenericArguments(string argsString)
        {
            var typeStringList = new List<string>();
            var current = new StringBuilder();
            int depth = 0;

            foreach (char c in argsString)
            {
                switch (c)
                {
                    case '[' when depth++ == 0:
                        continue; // 忽略最外层[
                    case ']' when --depth == 0:
                        continue; // 忽略最外层]
                    case ',' when depth == 0:
                        typeStringList.Add(current.ToString());
                        current.Clear();
                        break;
                    default:
                        current.Append(c);
                        break;
                }
            }

            if (current.Length > 0)
            {
                typeStringList.Add(current.ToString());
            }

            var types = typeStringList.Select(GetType).ToArray();

            return types;
        }

        public static Tuple<string?, string> SplitFullyQualifiedTypeName(string fullyQualifiedTypeName)
        {
            int? assemblyDelimiterIndex = GetAssemblyDelimiterIndex(fullyQualifiedTypeName);

            string typeName;
            string? assemblyName;

            if (assemblyDelimiterIndex != null)
            {
                assemblyName = fullyQualifiedTypeName.Substring(assemblyDelimiterIndex.Value + 1).Trim();
                typeName = fullyQualifiedTypeName.Substring(0, assemblyDelimiterIndex.Value).Trim();
            }
            else
            {
                typeName = fullyQualifiedTypeName;
                assemblyName = null;
            }

            return new Tuple<string?, string>(assemblyName, typeName);
        }

        private static int? GetAssemblyDelimiterIndex(string typeName)
        {
            int scope = 0;
            for (int i = 0; i < typeName.Length; i++)
            {
                switch (typeName[i])
                {
                    case '[': scope++; break;
                    case ']': scope--; break;
                    case ',' when scope == 0: return i;
                }
            }
            return null;
        }
    }
}
