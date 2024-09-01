using System.ComponentModel;

namespace ApeFree.Protocols.Json.JsonRpc
{
    public enum JsonRpcErrorCode
    {
        [Description("无错误")]
        None = 0,

        [Description("解析错误，无效的 JSON 被接收")]
        ParseError = -32700,

        [Description("无效的请求，JSON 不符合 JSON-RPC 规范")]
        InvalidRequest = -32600,

        [Description("方法不存在，方法找不到")]
        MethodNotFound = -32601,

        [Description("无效的参数，无效的方法参数")]
        InvalidParams = -32602,

        [Description("内部错误，JSON-RPC 服务器内部错误")]
        InternalError = -32603,

        [Description("服务器端错误，供扩展使用")]
        ServerError = -32000,
    }
}
