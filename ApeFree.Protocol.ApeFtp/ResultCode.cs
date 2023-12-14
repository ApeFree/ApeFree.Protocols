namespace ApeFree.Protocol.ApeFtp
{
    /// <summary>
    /// 响应码
    /// </summary>
    public enum ResultCode : byte
    {
        // =============== 0~99 普通响应码 ===============
        Continue = 0,
        Completed = 1,
        Cancelled = 2,



        // =============== 100~255 错误响应码 ===============

        // =============== 100~149 错误响应码:申请阶段   ===============

        /// <summary>
        /// 段长度过大
        /// </summary>
        SegmentSizeTooLarge = 100,

        /// <summary>
        /// 磁盘空间不足
        /// </summary>
        InsufficientDiskSpace = 101,

        /// <summary>
        /// 文件总长度过大
        /// </summary>
        FileSizeTooLarge = 102,

        /// <summary>
        /// 相同文件正在传输中
        /// </summary>
        SameFileTransmitting = 103,

        // =============== 150~199 错误响应码:发送阶段   ===============

        /// <summary>
        /// 无效的传输任务(传输任务未经申请)
        /// </summary>
        InvalidTransferTask = 150,

        /// <summary>
        /// 无效的数据段序号
        /// </summary>
        InvalidSegmentIndex = 151,

        /// <summary>
        /// 无效的取消指令
        /// </summary>
        InvalidCancelCommand = 151,

        // =============== 200~255 错误响应码:完成阶段   ===============

        /// <summary>
        /// MD5不匹配(传输结束后验证)
        /// </summary>
        Md5Mismatching = 200,
    }
}
