using ApeFree.Protocols.Json.Jbin;
using ApeFree.Protocols.Json.Jbin.Reflectors;
using ApeFree.Protocols.Json.JsonRpc;
using ApeFree.Protocols.Json.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests.Reflectors
{
    [TestClass]
    public class JbinRpcReflectorTests
    {
        [TestMethod]
        public void TestRpcInvoke_AddMethod()
        {
            var service = new CalculatorService();
            var reflector = new JbinRpcReflector();

            var req = new JsonRpcRequest
            {
                Id = 101L,
                Method = nameof(ICalculatorService.Add),
                Params = new object[] { 15, 27 }
            };

            var reqJbin = JbinObject.FromObject(req);
            var reqBytes = reqJbin.ToBytes();

            var respBytes = reflector.ReflectInvokeMethod(service, reqBytes);
            Assert.IsNotNull(respBytes);

            var respJbin = JbinObject.Parse(respBytes);
            var resp = respJbin.ToObject<JsonRpcResponse>();

            Assert.IsNotNull(resp);
            Assert.AreEqual(101L, resp.Id);
            Assert.IsNull(resp.Error);
            Assert.AreEqual(42L, Convert.ToInt64(resp.Result));
        }

        [TestMethod]
        public void TestRpcInvoke_WaveformMethod()
        {
            var service = new CalculatorService();
            var reflector = new JbinRpcReflector();

            var waveform = new double[] { 1.2, 5.8, 9.9, 3.4, -2.1 };
            var req = new JsonRpcRequest
            {
                Id = 102L,
                Method = nameof(ICalculatorService.ComputeWaveformMax),
                Params = new object[] { waveform }
            };

            var reqBytes = JbinObject.FromObject(req).ToBytes();
            var respBytes = reflector.ReflectInvokeMethod(service, reqBytes);

            var resp = JbinObject.Parse(respBytes).ToObject<JsonRpcResponse>();

            Assert.IsNotNull(resp);
            Assert.AreEqual(102L, resp.Id);
            Assert.AreEqual(9.9, Convert.ToDouble(resp.Result), 0.0001);
        }

        [TestMethod]
        public void TestRpcInvoke_BytesMethod()
        {
            var service = new CalculatorService();
            var reflector = new JbinRpcReflector();

            var rawBytes = new byte[] { 1, 2, 3, 4, 5 };
            var req = new JsonRpcRequest
            {
                Id = 103L,
                Method = nameof(ICalculatorService.ReverseBytes),
                Params = new object[] { rawBytes }
            };

            var reqBytes = JbinObject.FromObject(req).ToBytes();
            var respBytes = reflector.ReflectInvokeMethod(service, reqBytes);

            var resp = JbinObject.Parse(respBytes).ToObject<JsonRpcResponse>();

            Assert.IsNotNull(resp);
            Assert.AreEqual(103L, resp.Id);
            var resultBytes = resp.Result as byte[];
            Assert.IsNotNull(resultBytes);
            TestAssertHelper.AssertSequenceEqual(new byte[] { 5, 4, 3, 2, 1 }, resultBytes);
        }
    }
}
