namespace ApeFree.Protocol.ApeFtp
{
    /// <summary>
    /// 命令标识
    /// </summary>
    public enum CommandCode : byte
    {
        // =============== 请求类命令区间 ===============
        DemandRequest = 0xA0,
        TransferRequest = 0xA1,

        // =============== 响应类命令区间 ===============
        TransferResponse = 0xF0
    }
}
