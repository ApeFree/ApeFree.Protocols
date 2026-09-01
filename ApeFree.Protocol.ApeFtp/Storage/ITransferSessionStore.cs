using System;
using ApeFree.Protocol.ApeFtp.Core;

namespace ApeFree.Protocol.ApeFtp.Storage
{
    /// <summary>
    /// 传输会话状态记录（用于进度保存与断点续传）
    /// </summary>
    public class TransferSessionRecord
    {
        /// <summary>
        /// 任务唯一标识（如 MD5）
        /// </summary>
        public byte[] FileKey { get; set; }

        /// <summary>
        /// 文件总字节数
        /// </summary>
        public ulong TotalLength { get; set; }

        /// <summary>
        /// 分片大小
        /// </summary>
        public uint ChunkSize { get; set; }

        /// <summary>
        /// 已连续接收写入的字节数
        /// </summary>
        public ulong ReceivedBytes { get; set; }

        /// <summary>
        /// 已确认的最大连续分片序号
        /// </summary>
        public uint LastAckedChunkIndex { get; set; }

        /// <summary>
        /// 会话状态
        /// </summary>
        public SessionState State { get; set; }

        /// <summary>
        /// 文件名（可选）
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdatedTime { get; set; } = DateTime.UtcNow;

        public TransferSessionRecord(byte[] fileKey, ulong totalLength, uint chunkSize, string? fileName = null)
        {
            FileKey = fileKey ?? throw new ArgumentNullException(nameof(fileKey));
            TotalLength = totalLength;
            ChunkSize = chunkSize;
            FileName = fileName;
            State = SessionState.Created;
        }
    }

    /// <summary>
    /// 传输会话仓储接口（提供任务状态存储、查询与断点续传支持）
    /// </summary>
    public interface ITransferSessionStore
    {
        /// <summary>
        /// 获取指定任务的会话记录
        /// </summary>
        TransferSessionRecord? GetSession(byte[] fileKey);

        /// <summary>
        /// 保存或更新会话记录
        /// </summary>
        void SaveOrUpdateSession(TransferSessionRecord session);

        /// <summary>
        /// 更新任务接收进度
        /// </summary>
        void UpdateProgress(byte[] fileKey, ulong receivedBytes, uint ackedChunkIndex);

        /// <summary>
        /// 更新会话状态
        /// </summary>
        void UpdateState(byte[] fileKey, SessionState state);

        /// <summary>
        /// 移除会话记录
        /// </summary>
        bool RemoveSession(byte[] fileKey);

        /// <summary>
        /// 检查会话是否存在
        /// </summary>
        bool Exists(byte[] fileKey);
    }
}
