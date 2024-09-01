using Newtonsoft.Json;

namespace ApeFree.Protocols.Json.JsonRpc
{
    /// <summary>
    /// JsonRPC响应错误信息
    /// </summary>
    public class JsonRpcError
    {
        [JsonProperty("code")]
        public JsonRpcErrorCode Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        //[JsonProperty("data")]
        //public object Data { get; set; }
    }
}
