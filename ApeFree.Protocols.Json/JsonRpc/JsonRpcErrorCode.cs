using System.ComponentModel;

namespace ApeFree.Protocols.Json.JsonRpc
{
    /// <summary>
    /// JSON-RPC 错误代码
    /// </summary>
    public enum JsonRpcErrorCode
    {
        /// <summary>
        /// 无错误
        /// </summary>
        [Description("无错误")]
        None = 0,

        /// <summary>
        /// 解析错误， 无效的 JSON 被接收
        /// </summary>
        [Description("解析错误，无效的 JSON 被接收")]
        ParseError = -32700,

        /// <summary>
        /// 无效的请求，JSON 不符合 JSON-RPC 规范
        /// </summary>
        [Description("无效的请求，JSON 不符合 JSON-RPC 规范")]
        InvalidRequest = -32600,

        /// <summary>
        /// 方法不存在，方法找不到
        /// </summary>
        [Description("方法不存在，方法找不到")]
        MethodNotFound = -32601,

        /// <summary>
        /// 无效的参数，无效的方法参数
        /// </summary>
        [Description("无效的参数，无效的方法参数")]
        InvalidParams = -32602,

        /// <summary>
        /// 内部错误，JSON-RPC 服务器内部错误
        /// </summary>
        [Description("内部错误，JSON-RPC 服务器内部错误")]
        InternalError = -32603,

        /// <summary>
        /// 服务器端错误，供扩展使用
        /// </summary>
        [Description("服务器端错误")]
        ServerError = -32000,
    }
}
