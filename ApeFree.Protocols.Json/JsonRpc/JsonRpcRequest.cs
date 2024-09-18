using Newtonsoft.Json;

namespace ApeFree.Protocols.Json.JsonRpc
{
    /// <summary>
    /// JsonRPC请求实体类
    /// </summary>
    public class JsonRpcRequest
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public object[] Params { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }


    }
}
