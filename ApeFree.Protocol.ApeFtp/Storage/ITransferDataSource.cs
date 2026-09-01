using System;

namespace ApeFree.Protocol.ApeFtp.Storage
{
    /// <summary>
    /// 发送端数据源接口（提供文件/流/内存块的分片读取与哈希计算）
    /// </summary>
    public interface ITransferDataSource : IDisposable
    {
        /// <summary>
        /// 数据总字节大小
        /// </summary>
        ulong TotalLength { get; }

        /// <summary>
        /// 数据源唯一内容哈希指纹（如 MD5）
        /// </summary>
        byte[] Hash { get; }

        /// <summary>
        /// 原始文件名（可选）
        /// </summary>
        string? FileName { get; }

        /// <summary>
        /// 从指定字节偏移量读取数据分片
        /// </summary>
        /// <param name="offset">起始字节偏移量</param>
        /// <param name="destination">目标写入缓冲区</param>
        /// <returns>实际读取的字节数</returns>
        int ReadChunk(ulong offset, Span<byte> destination);

        /// <summary>
        /// 从指定字节偏移量读取数据分片并返回新字节数组
        /// </summary>
        byte[] ReadChunk(ulong offset, int count);
    }
}
