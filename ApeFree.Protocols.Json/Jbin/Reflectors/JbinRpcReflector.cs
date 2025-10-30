using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.JsonRpc;
using ApeFree.Protocols.Json.JsonRpc.Reflectors;
using STTech.CodePlus.Components;
using System;

namespace ApeFree.Protocols.Json.Jbin.Reflectors
{
    /// <summary>
    /// JbinRpc反射器
    /// </summary>
    public class JbinRpcReflector : Reflector
    {
        /// <inheritdoc/>
        public override byte[] ReflectInvokeMethod(object reflectObject, byte[] bytes)
        {
            // 解析JsonRpc请求数据包到 Jbin 对象
            var jbinReq = JbinObject.Parse(bytes);

            // 由 Jbin 对象反序列化为JsonRpc请求对象
            var req = jbinReq.ToObject<JsonRpcRequest>();

            // 执行JsonRpc的反射，得到返回的JsonRpc响应对象
            var resp = JsonRpcReflector.ReflectInvokeMethod(reflectObject, req);

            // 将JsonRpc响应对象序列化成为 Jbin 对象
            var jbinResp = JbinObject.FromObject(resp);

            // 将Jbin格式的响应数据转为字节数组
            var respBytes = jbinResp.ToBytes();

            // 如果返回结果是可销毁的对象，则执行销毁方法
            if (resp.Result is IDisposable o)
            {
                o.Dispose();
            }

            return respBytes;
        }

        /// <inheritdoc/>
        public override void ReflectRaiseEvent(object reflectObject, byte[] bytes)
        {
            // 解析JsonRpc请求数据包到 Jbin 对象
            var jbinReq = JbinObject.Parse(bytes);

            // 由 Jbin 对象反序列化为JsonRpc请求对象
            var req = jbinReq.ToObject<JsonRpcRequest>();

            // 执行JsonRpc的反射，得到返回的JsonRpc响应对象
            JsonRpcReflector.ReflectRaiseEvent(reflectObject, req);
        }
    }
}
