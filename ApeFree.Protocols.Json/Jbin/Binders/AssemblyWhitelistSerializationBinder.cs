using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using STTech.CodePlus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ApeFree.Protocols.Json.Jbin.Binders
{
    /// <summary>
    /// 程序集白名单序列化绑定器（保障多态反序列化安全）
    /// </summary>
    public class AssemblyWhitelistSerializationBinder : ISerializationBinder
    {
        private readonly HashSet<string> _allowedAssemblies;

        /// <summary>
        /// 构造程序集白名单序列化绑定器
        /// </summary>
        /// <param name="allowedTypes">允许通过的类型列表</param>
        public AssemblyWhitelistSerializationBinder(params Type[] allowedTypes)
            : this((IEnumerable<Type>)allowedTypes)
        {
        }

        /// <summary>
        /// 构造程序集白名单序列化绑定器
        /// </summary>
        /// <param name="allowedTypes">允许通过的类型集合</param>
        public AssemblyWhitelistSerializationBinder(IEnumerable<Type> allowedTypes)
        {
            _allowedAssemblies = new HashSet<string>(allowedTypes.Select(t => t.Assembly.GetName().Name), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 构造程序集白名单序列化绑定器
        /// </summary>
        /// <param name="allowedAssemblies">允许通过的程序集列表</param>
        public AssemblyWhitelistSerializationBinder(IEnumerable<Assembly> allowedAssemblies)
        {
            _allowedAssemblies = new HashSet<string>(allowedAssemblies.Select(x => x.GetName().Name), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 构造程序集白名单序列化绑定器
        /// </summary>
        /// <param name="allowedAssemblyNames">允许通过的程序集名称列表</param>
        public AssemblyWhitelistSerializationBinder(params string[] allowedAssemblyNames)
        {
            _allowedAssemblies = new HashSet<string>(allowedAssemblyNames, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public Type BindToType(string assemblyName, string typeName)
        {
            var fullTypeIdentifier = $"{typeName}, {assemblyName}";

            if (_allowedAssemblies.Contains(assemblyName))
            {
                var type = TypeParser.GetType(fullTypeIdentifier);
                if (type != null)
                {
                    return type;
                }
            }

            throw new JsonSerializationException(
                $"Type {fullTypeIdentifier} is not allowed by assembly whitelist. " +
                $"Allowed assemblies: {string.Join(", ", _allowedAssemblies)}");
        }

        /// <inheritdoc/>
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = serializedType.Assembly.FullName;
            typeName = serializedType.FullName;
        }
    }
}
