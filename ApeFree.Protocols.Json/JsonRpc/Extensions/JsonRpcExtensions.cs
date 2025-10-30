using ApeFree.Protocols.Json.JsonRpc;
using Newtonsoft.Json;

namespace ApeFree.Protocols.Json.JsonRpc
{
    /// <summary>
    /// JsonRpc扩展方法
    /// </summary>
    public static class JsonRpcExtensions
    {
        /// <summary>
        /// Json序列化设置
        /// </summary>
        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
        };

        /// <summary>
        /// 将JsonRpcRequest序列化为JSON字符串
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static string ToJsonString(this JsonRpcRequest req)
        {
            var json = JsonConvert.SerializeObject(req, Formatting.Indented, JsonSerializerSettings);
            return json;
        }

        /// <summary>
        /// 将JSON字符串反序列化为JsonRpcRequest对象
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static JsonRpcRequest DeserializeRequest(string json)
        {
            var req = JsonConvert.DeserializeObject<JsonRpcRequest>(json, JsonSerializerSettings);
            return req;
        }

        /// <summary>
        /// 将JsonRpcResponse序列化为JSON字符串
        /// </summary>
        /// <param name="resp"></param>
        /// <returns></returns>
        public static string ToJsonString(this JsonRpcResponse resp)
        {
            var json = JsonConvert.SerializeObject(resp, Formatting.Indented, JsonSerializerSettings);
            return json;
        }

        /// <summary>
        /// 将JSON字符串反序列化为JsonRpcResponse对象
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static JsonRpcResponse DeserializeResponse(string json)
        {
            var resp = JsonConvert.DeserializeObject<JsonRpcResponse>(json, JsonSerializerSettings);
            return resp;
        }
    }
}
