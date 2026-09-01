using System;
using ApeFree.Protocol.ApeFtp.Core;
using ApeFree.Protocol.ApeFtp.Core.Packets;

namespace ApeFree.Protocol.ApeFtp.Engine
{
    /// <summary>
    /// 传输进度改变事件参数
    /// </summary>
    public class TransferProgressEventArgs : EventArgs
    {
        public byte[] FileKey { get; }
        public ulong TransferredBytes { get; }
        public ulong TotalBytes { get; }
        public double ProgressPercentage => TotalBytes == 0 ? 100.0 : (double)TransferredBytes / TotalBytes * 100.0;
        public uint CurrentChunkIndex { get; }
        public uint TotalChunkCount { get; }

        public TransferProgressEventArgs(byte[] fileKey, ulong transferredBytes, ulong totalBytes, uint currentChunkIndex, uint totalChunkCount)
        {
            FileKey = fileKey;
            TransferredBytes = transferredBytes;
            TotalBytes = totalBytes;
            CurrentChunkIndex = currentChunkIndex;
            TotalChunkCount = totalChunkCount;
        }
    }

    /// <summary>
    /// 传输完成事件参数
    /// </summary>
    public class TransferCompletedEventArgs : EventArgs
    {
        public byte[] FileKey { get; }
        public ulong TotalBytes { get; }
        public bool IsFastUpload { get; }
        public string? TargetPath { get; }

        public TransferCompletedEventArgs(byte[] fileKey, ulong totalBytes, bool isFastUpload = false, string? targetPath = null)
        {
            FileKey = fileKey;
            TotalBytes = totalBytes;
            IsFastUpload = isFastUpload;
            TargetPath = targetPath;
        }
    }

    /// <summary>
    /// 传输失败事件参数
    /// </summary>
    public class TransferFailedEventArgs : EventArgs
    {
        public byte[] FileKey { get; }
        public ResultCode ResultCode { get; }
        public string Message { get; }

        public TransferFailedEventArgs(byte[] fileKey, ResultCode resultCode, string message)
        {
            FileKey = fileKey;
            ResultCode = resultCode;
            Message = message;
        }
    }

    /// <summary>
    /// 数据包待发送事件参数（提供强类型 Packet 与已编码的二进制帧）
    /// </summary>
    public class PacketToSendEventArgs : EventArgs
    {
        public IApeFtpPacket Packet { get; }
        public byte[] EncodedFrame { get; }

        public PacketToSendEventArgs(IApeFtpPacket packet, byte[] encodedFrame)
        {
            Packet = packet;
            EncodedFrame = encodedFrame;
        }
    }
}
