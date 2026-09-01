using System;
using System.Collections.Concurrent;
using System.Text;
using ApeFree.Protocol.ApeFtp.Codec;
using ApeFree.Protocol.ApeFtp.Core;
using ApeFree.Protocol.ApeFtp.Core.Packets;
using ApeFree.Protocol.ApeFtp.Storage;

namespace ApeFree.Protocol.ApeFtp.Engine
{
    /// <summary>
    /// ApeFtp 接收端纯协议状态机引擎（Sans-I/O 架构，负责申请裁决、突发批次聚合 ACK、断点续传与完整性提交）
    /// </summary>
    public class ApeFtpReceiverEngine : IDisposable
    {
        private readonly ApeFtpFrameDecoder _decoder = new ApeFtpFrameDecoder();
        private readonly ConcurrentDictionary<string, ActiveReceiveSession> _activeSessions = new ConcurrentDictionary<string, ActiveReceiveSession>();
        private readonly object _lock = new object();
        private bool _isDisposed = false;

        /// <summary>
        /// 会话状态与进度仓储
        /// </summary>
        public ITransferSessionStore SessionStore { get; }

        /// <summary>
        /// 数据存储目标工厂委托（根据申请请求创建或获取 ITransferDataSink）
        /// </summary>
        public Func<DemandRequest, ITransferDataSink?> DataSinkFactory { get; }

        /// <summary>
        /// 单分段最大允许尺寸（默认 512KB）
        /// </summary>
        public uint MaxChunkSize { get; set; } = 512 * 1024;

        /// <summary>
        /// 突发窗口最大允许尺寸（默认 16 包）
        /// </summary>
        public uint MaxWindowSize { get; set; } = 16;

        /// <summary>
        /// 单文件最大允许字节数
        /// </summary>
        public ulong MaxFileSize { get; set; } = ulong.MaxValue;

        /// <summary>
        /// 当产生待发送的数据包/二进制帧时触发
        /// </summary>
        public event EventHandler<PacketToSendEventArgs>? PacketReadyToSend;

        /// <summary>
        /// 接收进度改变事件
        /// </summary>
        public event EventHandler<TransferProgressEventArgs>? ProgressChanged;

        /// <summary>
        /// 传输完成事件
        /// </summary>
        public event EventHandler<TransferCompletedEventArgs>? Completed;

        /// <summary>
        /// 传输失败事件
        /// </summary>
        public event EventHandler<TransferFailedEventArgs>? Failed;

        /// <summary>
        /// 传输取消事件
        /// </summary>
        public event EventHandler? Cancelled;

        public ApeFtpReceiverEngine(Func<DemandRequest, ITransferDataSink?> dataSinkFactory, ITransferSessionStore? sessionStore = null)
        {
            DataSinkFactory = dataSinkFactory ?? throw new ArgumentNullException(nameof(dataSinkFactory));
            SessionStore = sessionStore ?? new InMemoryTransferSessionStore();

            _decoder.PacketDecoded += ProcessIncomingPacket;
        }

        /// <summary>
        /// 输入接收到的原始字节流
        /// </summary>
        public void Feed(ReadOnlySpan<byte> rawBytes)
        {
            lock (_lock)
            {
                _decoder.Feed(rawBytes);
            }
        }

        /// <summary>
        /// 直接输入已解码的数据包
        /// </summary>
        public void ProcessIncomingPacket(IApeFtpPacket packet)
        {
            lock (_lock)
            {
                if (_isDisposed) return;

                switch (packet)
                {
                    case DemandRequest demandReq:
                        HandleDemandRequest(demandReq);
                        break;

                    case DataPacket dataPacket:
                        HandleDataPacket(dataPacket);
                        break;

                    case CancelRequest cancelReq:
                        HandleCancelRequest(cancelReq);
                        break;
                }
            }
        }

        private void HandleDemandRequest(DemandRequest req)
        {
            string keyStr = ToHex(req.FileKey);

            // 1. 检查是否超出最大单段限制
            if (req.ChunkSize > MaxChunkSize)
            {
                SendPacket(new DemandResponse(req.FileKey, ResultCode.ChunkSizeTooLarge, acceptedChunkSize: MaxChunkSize));
                return;
            }

            // 2. 检查是否超出最大文件大小
            if (req.TotalLength > MaxFileSize)
            {
                SendPacket(new DemandResponse(req.FileKey, ResultCode.FileSizeTooLarge, message: "文件大小超出接收端允许的最大限制"));
                return;
            }

            // 3. 检查仓储中是否已存在已完成的会话（秒传）
            var existing = SessionStore.GetSession(req.FileKey);
            if (existing != null && existing.State == SessionState.Completed)
            {
                SendPacket(new DemandResponse(req.FileKey, ResultCode.Completed, message: "目标端已存在该文件，秒传完成"));
                Completed?.Invoke(this, new TransferCompletedEventArgs(req.FileKey, req.TotalLength, isFastUpload: true));
                return;
            }

            // 4. 计算协商参数
            uint acceptedChunk = Math.Min(req.ChunkSize, MaxChunkSize);
            uint acceptedWin = Math.Min(req.WindowSize, MaxWindowSize);
            if (acceptedWin == 0) acceptedWin = 1;

            // 5. 检查断点续传偏移
            ulong resumedOffset = 0;
            if (existing != null && existing.State == SessionState.Transferring && existing.ReceivedBytes > 0)
            {
                resumedOffset = existing.ReceivedBytes;
            }

            // 6. 创建或获取 DataSink
            ITransferDataSink? sink;
            try
            {
                sink = DataSinkFactory.Invoke(req);
                if (sink == null)
                {
                    SendPacket(new DemandResponse(req.FileKey, ResultCode.Rejected, message: "数据存储目标创建失败"));
                    return;
                }
            }
            catch (Exception ex)
            {
                SendPacket(new DemandResponse(req.FileKey, ResultCode.InsufficientDiskSpace, message: ex.Message));
                return;
            }

            var sessionRecord = existing ?? new TransferSessionRecord(req.FileKey, req.TotalLength, acceptedChunk, req.FileName);
            sessionRecord.State = SessionState.Transferring;
            sessionRecord.ChunkSize = acceptedChunk;
            SessionStore.SaveOrUpdateSession(sessionRecord);

            var active = new ActiveReceiveSession(req.FileKey, req.TotalLength, acceptedChunk, acceptedWin, sink);
            _activeSessions.AddOrUpdate(keyStr, active, (k, old) =>
            {
                old.DataSink.Dispose();
                return active;
            });

            SendPacket(new DemandResponse(req.FileKey, ResultCode.Success, acceptedChunk, acceptedWin, resumedOffset));
        }

        private void HandleDataPacket(DataPacket data)
        {
            string keyStr = ToHex(data.FileKey);
            if (!_activeSessions.TryGetValue(keyStr, out var active))
            {
                SendPacket(new AckResponse(data.FileKey, ResultCode.InvalidSession, data.ChunkIndex, message: "未找到对应的传输会话"));
                return;
            }

            // 1. 验证单包 CRC32
            if (!data.ValidateCrc())
            {
                SendPacket(new AckResponse(data.FileKey, ResultCode.ChunkCrcMismatch, data.ChunkIndex, message: "分片 CRC 校验失败，请求重传"));
                return;
            }

            // 2. 写入存储介质
            try
            {
                active.DataSink.WriteChunk(data.Offset, data.Data);
            }
            catch (Exception ex)
            {
                SendPacket(new AckResponse(data.FileKey, ResultCode.InsufficientDiskSpace, data.ChunkIndex, message: ex.Message));
                Fail(data.FileKey, ResultCode.InsufficientDiskSpace, ex.Message);
                return;
            }

            active.ReceivedBytes = Math.Max(active.ReceivedBytes, data.Offset + (ulong)data.Data.Length);
            active.LastChunkIndex = data.ChunkIndex;
            active.BurstCount++;

            uint totalChunkCount = (uint)Math.Ceiling(active.TotalLength / (double)active.ChunkSize);
            ProgressChanged?.Invoke(this, new TransferProgressEventArgs(data.FileKey, active.ReceivedBytes, active.TotalLength, data.ChunkIndex + 1, totalChunkCount));
            SessionStore.UpdateProgress(data.FileKey, active.ReceivedBytes, data.ChunkIndex);

            // 3. 检查是否接收完毕
            if (active.ReceivedBytes >= active.TotalLength)
            {
                bool finalized = active.DataSink.VerifyAndFinalize(data.FileKey);
                if (finalized)
                {
                    SessionStore.UpdateState(data.FileKey, SessionState.Completed);
                    SendPacket(new AckResponse(data.FileKey, ResultCode.Completed, data.ChunkIndex, active.BurstCount, message: "文件传输并校验成功"));
                    Completed?.Invoke(this, new TransferCompletedEventArgs(data.FileKey, active.TotalLength, false, active.DataSink.TargetPath));
                    _activeSessions.TryRemove(keyStr, out _);
                }
                else
                {
                    SessionStore.UpdateState(data.FileKey, SessionState.Failed);
                    SendPacket(new AckResponse(data.FileKey, ResultCode.HashMismatch, data.ChunkIndex, active.BurstCount, message: "全量完整性哈希比对失败"));
                    Fail(data.FileKey, ResultCode.HashMismatch, "全量完整性哈希比对失败");
                    _activeSessions.TryRemove(keyStr, out _);
                }
                return;
            }

            // 4. 突发批次 ACK 产生逻辑
            if (active.BurstCount >= active.WindowSize)
            {
                SendPacket(new AckResponse(data.FileKey, ResultCode.Success, data.ChunkIndex, active.BurstCount));
                active.BurstCount = 0;
            }
        }

        private void HandleCancelRequest(CancelRequest cancel)
        {
            string keyStr = ToHex(cancel.FileKey);
            if (_activeSessions.TryRemove(keyStr, out var active))
            {
                active.DataSink.Abort();
                SessionStore.UpdateState(cancel.FileKey, SessionState.Cancelled);
            }

            SendPacket(new CancelResponse(cancel.FileKey, ResultCode.Cancelled));
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void SendPacket(IApeFtpPacket packet)
        {
            byte[] encoded = ApeFtpFrameEncoder.Encode(packet);
            PacketReadyToSend?.Invoke(this, new PacketToSendEventArgs(packet, encoded));
        }

        private void Fail(byte[] fileKey, ResultCode code, string message)
        {
            Failed?.Invoke(this, new TransferFailedEventArgs(fileKey, code, message));
        }

        private static string ToHex(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_isDisposed)
                {
                    foreach (var pair in _activeSessions)
                    {
                        pair.Value.DataSink.Dispose();
                    }
                    _activeSessions.Clear();
                    _isDisposed = true;
                }
            }
        }

        private class ActiveReceiveSession
        {
            public byte[] FileKey { get; }
            public ulong TotalLength { get; }
            public uint ChunkSize { get; }
            public uint WindowSize { get; }
            public ITransferDataSink DataSink { get; }

            public ulong ReceivedBytes { get; set; }
            public uint LastChunkIndex { get; set; }
            public uint BurstCount { get; set; }

            public ActiveReceiveSession(byte[] fileKey, ulong totalLength, uint chunkSize, uint windowSize, ITransferDataSink dataSink)
            {
                FileKey = fileKey;
                TotalLength = totalLength;
                ChunkSize = chunkSize;
                WindowSize = windowSize;
                DataSink = dataSink;
            }
        }
    }
}
