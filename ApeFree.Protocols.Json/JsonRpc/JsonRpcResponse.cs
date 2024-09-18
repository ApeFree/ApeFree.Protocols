using Newtonsoft.Json;

namespace ApeFree.Protocols.Json.JsonRpc
{
    /// <summary>
    /// JsonRPC响应实体类
    /// </summary>
    public class JsonRpcResponse
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; }

        [JsonProperty("result")]
        public object Result { get; set; }

        [JsonProperty("error")]
        public JsonRpcError Error { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }
    }
}
