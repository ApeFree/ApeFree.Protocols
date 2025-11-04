using ApeFree.Protocols.Json.Jbin;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApeFree.Protocols.Json.JsonRpc
{
    /// <summary>
    /// RpcDataFormat扩展方法
    /// </summary>
    public static class RpcDataFormatExtensions
    {
        /// <inheritdoc/>
        public static JsonRpcResponse ConvertBytesToResponseObject(this RpcDataFormat format, byte[] data)
        {
            switch (format)
            {
                case RpcDataFormat.Jbin:
                    return ConvertBytesToJbinResponseObject(data);
                case RpcDataFormat.Json:
                    return ConvertBytesToJsonResponseObject(data);
                default:
                    throw new NotSupportedException($"不支持的RPC数据格式：{format}");
            }
        }

        /// <inheritdoc/>
        public static JsonRpcRequest ConvertBytesToRequestObject(this RpcDataFormat format, byte[] data)
        {
            switch (format)
            {
                case RpcDataFormat.Jbin:
                    return ConvertBytesToJbinRequestObject(data);
                case RpcDataFormat.Json:
                    return ConvertBytesToJsonRequestObject(data);
                default:
                    throw new NotSupportedException($"不支持的RPC数据格式：{format}");
            }
        }

        /// <inheritdoc/>
        public static byte[] ConvertRequestObjectToBytes(this RpcDataFormat format, JsonRpcRequest req)
        {
            switch (format)
            {
                case RpcDataFormat.Jbin:
                    return ConvertJbinRequestObjectToBytes(req);
                case RpcDataFormat.Json:
                    return ConvertJsonRequestObjectToBytes(req);
                default:
                    throw new NotSupportedException($"不支持的RPC数据格式：{format}");
            }
        }

        /// <inheritdoc/>
        public static byte[] ConvertResponseObjectToBytes(this RpcDataFormat format, JsonRpcResponse resp)
        {
            switch (format)
            {
                case RpcDataFormat.Jbin:
                    return ConvertJbinResponseObjectToBytes(resp);
                case RpcDataFormat.Json:
                    return ConvertJsonResponseObjectToBytes(resp);
                default:
                    throw new NotSupportedException($"不支持的RPC数据格式：{format}");
            }
        }


        /// <inheritdoc/>
        public static JsonRpcResponse ConvertBytesToJbinResponseObject(byte[] data)
        {
            // 解析 JsonRpc 响应数据包到 Jbin 对象
            var jbinReq = JbinObject.Parse(data);

            // 由 Jbin 对象反序列化为JsonRpc响应对象
            var resp = jbinReq.ToObject<JsonRpcResponse>();

            return resp;
        }

        /// <inheritdoc/>
        public static JsonRpcRequest ConvertBytesToJbinRequestObject(byte[] data)
        {
            // 解析 JsonRpc 请求数据包到 Jbin 对象
            var jbin = JbinObject.Parse(data);

            // 由 Jbin 对象反序列化为JsonRpc请求对象
            var req = jbin.ToObject<JsonRpcRequest>();

            return req;
        }

        /// <inheritdoc/>
        public static byte[] ConvertJbinRequestObjectToBytes(JsonRpcRequest req)
        {
            // 将 JsonRpc 请求对象序列化成为 Jbin 对象
            var jbinReq = JbinObject.FromObject(req);

            // 将 Jbin 格式的请求数据转为字节数组
            var reqBytes = jbinReq.ToBytes();

            return reqBytes;
        }

        /// <inheritdoc/>
        public static byte[] ConvertJbinResponseObjectToBytes(JsonRpcResponse resp)
        {
            // 将 JsonRpc 响应对象序列化成为 Jbin 对象
            var jbinResp = JbinObject.FromObject(resp);

            // 将 Jbin 格式的响应数据转为字节数组
            var respBytes = jbinResp.ToBytes();

            return respBytes;
        }

        /// <summary>
        /// 将字节数组转换为JsonRpc响应对象
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static JsonRpcResponse ConvertBytesToJsonResponseObject(byte[] data)
        {
            var json = data.EncodeToString();
            var resp = JsonRpcExtensions.DeserializeResponse(json);
            return resp;
        }

        /// <summary>
        /// 将字节数组转换为JsonRpc请求对象
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static JsonRpcRequest ConvertBytesToJsonRequestObject(byte[] data)
        {
            var json = data.EncodeToString();
            var req = JsonRpcExtensions.DeserializeRequest(json);
            return req;
        }

        /// <summary>
        /// 将请求对象转换为字节数组
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static byte[] ConvertJsonRequestObjectToBytes(JsonRpcRequest req)
        {
            var json = req.ToJsonString();
            return json.GetBytes();
        }

        /// <summary>
        /// 将JsonRpc响应对象转换为字节数组
        /// </summary>
        /// <param name="resp"></param>
        /// <returns></returns>
        public static byte[] ConvertJsonResponseObjectToBytes(JsonRpcResponse resp)
        {
            var json = resp.ToJsonString();
            return json.GetBytes();
        }
    }
}
