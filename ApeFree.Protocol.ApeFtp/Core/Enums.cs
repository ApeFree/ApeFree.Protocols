namespace ApeFree.Protocol.ApeFtp.Core
{
    /// <summary>
    /// ApeFtp 数据包类型标识
    /// </summary>
    public enum PacketType : byte
    {
        /// <summary>
        /// 传输申请请求 (Sender -> Receiver)
        /// </summary>
        DemandRequest = 0x01,

        /// <summary>
        /// 传输申请响应 (Receiver -> Sender)
        /// </summary>
        DemandResponse = 0x02,

        /// <summary>
        /// 数据分片包 (Sender -> Receiver)
        /// </summary>
        DataPacket = 0x03,

        /// <summary>
        /// 批量分片确认 (Receiver -> Sender)
        /// </summary>
        AckResponse = 0x04,

        /// <summary>
        /// 取消传输请求 (Sender -> Receiver 或 Receiver -> Sender)
        /// </summary>
        CancelRequest = 0x05,

        /// <summary>
        /// 取消传输响应
        /// </summary>
        CancelResponse = 0x06,
    }

    /// <summary>
    /// 响应结果代码
    /// </summary>
    public enum ResultCode : byte
    {
        // ================= 0~99 正常与状态响应码 =================

        /// <summary>
        /// 成功 / 继续传输
        /// </summary>
        Success = 0,

        /// <summary>
        /// 传输已完成（或目标端已有完全相同文件，触发秒传）
        /// </summary>
        Completed = 1,

        /// <summary>
        /// 任务已取消
        /// </summary>
        Cancelled = 2,


        // ================= 100~149 申请/协商阶段异常 =================

        /// <summary>
        /// 分段长度过大（超出接收端缓冲区限制）
        /// </summary>
        ChunkSizeTooLarge = 100,

        /// <summary>
        /// 磁盘或存储空间不足
        /// </summary>
        InsufficientDiskSpace = 101,

        /// <summary>
        /// 文件总大小超出限制
        /// </summary>
        FileSizeTooLarge = 102,

        /// <summary>
        /// 相同传输任务已在进行中
        /// </summary>
        SameFileTransmitting = 103,

        /// <summary>
        /// 协商被拒绝
        /// </summary>
        Rejected = 104,


        // ================= 150~199 传输阶段异常 =================

        /// <summary>
        /// 无效的传输任务（未协商或已过期）
        /// </summary>
        InvalidSession = 150,

        /// <summary>
        /// 无效的分片索引
        /// </summary>
        InvalidChunkIndex = 151,

        /// <summary>
        /// 无效的取消指令
        /// </summary>
        InvalidCancelCommand = 152,

        /// <summary>
        /// 单分段数据 CRC32 校验失败
        /// </summary>
        ChunkCrcMismatch = 153,


        // ================= 200~255 完成阶段校验异常 =================

        /// <summary>
        /// 最终完整性哈希（MD5/SHA256）校验不匹配
        /// </summary>
        HashMismatch = 200,
    }

    /// <summary>
    /// 传输会话状态
    /// </summary>
    public enum SessionState : byte
    {
        /// <summary>
        /// 已创建
        /// </summary>
        Created,

        /// <summary>
        /// 协商中
        /// </summary>
        Negotiating,

        /// <summary>
        /// 传输中
        /// </summary>
        Transferring,

        /// <summary>
        /// 传输暂停/等待重传
        /// </summary>
        WaitingForAck,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled,

        /// <summary>
        /// 异常中断
        /// </summary>
        Failed,
    }
}
