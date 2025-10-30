using Newtonsoft.Json;
using STTech.CodePlus.Components;
using STTech.CodePlus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApeFree.Protocols.Json.JsonRpc.Reflectors
{
    /// <summary>
    /// JsonRPC反射器
    /// </summary>
    public class JsonRpcReflector : Reflector
    {
        /// <summary>
        /// Json序列化设置
        /// </summary>
        public readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            NullValueHandling = NullValueHandling.Ignore,
        };

        #region 反射调用方法
        /// <inheritdoc/>
        public override byte[] ReflectInvokeMethod(object reflectObject, byte[] bytes)
        {
            // 调用内部方法进行反射处理，并将结果转换为字符串
            var jsonResp = ReflectInvokeMethod(reflectObject, bytes.EncodeToString());
            // 将字符串转换为字节数组返回
            var respBytes = jsonResp.GetBytes();
            return respBytes;
        }

        /// <summary>
        /// JsonRPC反射调用方法
        /// </summary>
        /// <param name="reflectObject">对象</param>
        /// <param name="reqJson">请求的Json字符串</param>
        /// <returns>字符串</returns>
        public string ReflectInvokeMethod(object reflectObject, string reqJson)
        {
            JsonRpcRequest req = null;

            try
            {
                // 尝试反序列化请求的Json字符串为JsonRpcRequest对象
                req = JsonRpcExtensions.DeserializeRequest(reqJson);
            }
            catch (Exception ex)
            {
                // 如果反序列化失败，返回包含错误信息的JsonRpcResponse的Json字符串
                return new JsonRpcResponse()
                {
                    JsonRpc = req.JsonRpc,
                    Id = req.Id,
                    Result = null,
                    Error = new JsonRpcError()
                    {
                        Code = JsonRpcErrorCode.ParseError,
                        Message = ex.Message,
                    }
                }.ToJsonString();
            }

            var resp = ReflectInvokeMethod(reflectObject, req);
            var respJson = resp.ToJsonString();

            if (resp.Result is IDisposable o)
            {
                // 如果结果可释放，进行释放操作
                o.Dispose();
            }

            return respJson;
        }

        /// <summary>
        /// 内部的JsonRPC反射调用方法，处理对象和JsonRpcRequest对象
        /// </summary>
        /// <param name="reflectObject">反射的目标对象</param>
        /// <param name="req">JsonRpcRequest对象</param>
        /// <returns>JsonRpcResponse对象</returns>
        public static JsonRpcResponse ReflectInvokeMethod(object reflectObject, JsonRpcRequest req)
        {
            var mi = ReflectionUtils.MatchMethod(reflectObject, req.Method, req.Params);

            if (mi == null)
            {
                // 如果未找到匹配的方法，返回包含错误信息的JsonRpcResponse对象
                return new JsonRpcResponse()
                {
                    JsonRpc = req.JsonRpc,
                    Id = req.Id,
                    Result = null,
                    Error = new JsonRpcError()
                    {
                        Code = JsonRpcErrorCode.MethodNotFound,
                        Message = $"{nameof(JsonRpcErrorCode.MethodNotFound)}({req.Method})",
                    }
                };
            }

            try
            {
                // 转换方法的参数类型
                req.Params = ReflectionUtils.ConvertMethodParameterType(mi, req.Params);

                // 调用方法并获取结果
                object result = mi.Invoke(reflectObject, req.Params);

                // 返回包含结果的JsonRpcResponse对象
                return new JsonRpcResponse()
                {
                    JsonRpc = req.JsonRpc,
                    Id = req.Id,
                    Result = result,
                    Error = null,
                };
            }
            catch (Exception ex)
            {
                List<string> errorMessageList = new List<string>();
                while (ex != null)
                {
                    errorMessageList.Add(ex.Message);
                    ex = ex.InnerException;
                }

                // 如果调用方法时出错，返回包含错误信息的JsonRpcResponse对象
                return new JsonRpcResponse()
                {
                    JsonRpc = req.JsonRpc,
                    Id = req.Id,
                    Result = null,
                    Error = new JsonRpcError()
                    {
                        Code = JsonRpcErrorCode.InternalError,
                        Message = errorMessageList.Join("\r\n"),
                    }
                };
            }
        }
        #endregion


        #region 反射触发事件
        /// <inheritdoc/>
        public override void ReflectRaiseEvent(object reflectObject, byte[] bytes)
        {
            // 调用内部方法进行反射处理
            ReflectRaiseEvent(reflectObject, bytes.EncodeToString());
        }

        /// <summary>
        /// 内部的JsonRPC反射触发事件的方法，处理对象和请求的Json字符串
        /// </summary>
        /// <param name="reflectObject">反射的目标对象</param>
        /// <param name="reqJson">请求的Json字符串</param>
        public void ReflectRaiseEvent(object reflectObject, string reqJson)
        {
            JsonRpcRequest req;
            try
            {
                // 尝试反序列化请求的Json字符串为JsonRpcRequest对象
                req = JsonRpcExtensions.DeserializeRequest(reqJson);
            }
            catch (Exception ex)
            {
                // 如果反序列化失败，抛出异常
                throw new Exception("反序列化失败。", ex);
            }

            ReflectRaiseEvent(reflectObject, req);
        }

        /// <summary>
        /// JsonRPC反射触发事件的方法
        /// </summary>
        /// <param name="reflectObject">反射的目标对象</param>
        /// <param name="req">JsonRpcRequest对象</param>
        public static void ReflectRaiseEvent(object reflectObject, JsonRpcRequest req)
        {
            ReflectionUtils.ReflectRaiseEvent(reflectObject, req.Method, req.Params);
        }
        #endregion
    }
}
