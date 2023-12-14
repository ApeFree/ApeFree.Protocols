namespace ApeFree.Protocol.ApeFtp
{
    /// <summary>
    /// 命令标识
    /// </summary>
    public enum CommandCode : byte
    {
        // =============== 请求类命令区间 ===============
        DemandRequest = 0x01,
        TransferRequest = 0x02,

        // =============== 响应类命令区间 ===============
        TransferResponse = 0x03
    }
}
