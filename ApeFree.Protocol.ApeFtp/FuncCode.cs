namespace ApeFree.Protocol.ApeFtp
{
    /// <summary>
    /// 传输功能码
    /// </summary>
    public enum FuncCode : byte
    {
        /// <summary>
        /// 取消传输任务
        /// </summary>
        Cancel = 0,

        /// <summary>
        /// 正常传输
        /// </summary>
        Send,
    }
}
