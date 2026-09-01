using System;

namespace ApeFree.Protocol.ApeFtp.Storage
{
    /// <summary>
    /// 接收端数据存储目标接口（支持乱序/断点分段写入与最终哈希校验）
    /// </summary>
    public interface ITransferDataSink : IDisposable
    {
        /// <summary>
        /// 预期接收的总字节长度
        /// </summary>
        ulong TotalLength { get; }

        /// <summary>
        /// 目标资源标识或路径（可选）
        /// </summary>
        string? TargetPath { get; }

        /// <summary>
        /// 在指定偏移量处写入分片数据（支持乱序分段写入）
        /// </summary>
        /// <param name="offset">写入起始偏移量</param>
        /// <param name="data">分段数据</param>
        void WriteChunk(ulong offset, ReadOnlySpan<byte> data);

        /// <summary>
        /// 获取已写入并 Flush 的有效连续字节长度（用于断点续传初始检查）
        /// </summary>
        ulong GetCurrentLength();

        /// <summary>
        /// 校验数据完整性（比对哈希）并完成落盘提交
        /// </summary>
        /// <param name="expectedHash">期望的哈希值（如 MD5）</param>
        /// <returns>校验是否通过</returns>
        bool VerifyAndFinalize(byte[] expectedHash);

        /// <summary>
        /// 中止传输并清理未完成的临时数据
        /// </summary>
        void Abort();
    }
}
