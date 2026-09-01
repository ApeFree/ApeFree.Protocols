using System;
using ApeFree.Protocol.ApeFtp.Codec;
using ApeFree.Protocol.ApeFtp.Core;
using ApeFree.Protocol.ApeFtp.Core.Packets;
using ApeFree.Protocol.ApeFtp.Storage;

namespace ApeFree.Protocol.ApeFtp.Engine
{
    /// <summary>
    /// ApeFtp 发送端纯协议状态机引擎（Sans-I/O 架构，负责协商、突发分片传输与窗口推进）
    /// </summary>
    public class ApeFtpSenderEngine : IDisposable
    {
        private readonly ApeFtpFrameDecoder _decoder = new ApeFtpFrameDecoder();
        private readonly object _lock = new object();

        private ulong _currentOffset = 0;
        private uint _nextChunkIndex = 0;
        private uint _totalChunkCount = 0;
        private uint _ackedChunkIndex = 0;
        private bool _isDisposed = false;

        /// <summary>
        /// 数据源
        /// </summary>
        public ITransferDataSource DataSource { get; }

        /// <summary>
        /// 当前分片大小
        /// </summary>
        public uint ChunkSize { get; private set; }

        /// <summary>
        /// 当前突发确认窗口大小（连续发送的最大包数）
        /// </summary>
        public uint WindowSize { get; private set; }

        /// <summary>
        /// 当前传输状态
        /// </summary>
        public SessionState State { get; private set; } = SessionState.Created;

        /// <summary>
        /// 当产生待发送的数据包/二进制帧时触发
        /// </summary>
        public event EventHandler<PacketToSendEventArgs>? PacketReadyToSend;

        /// <summary>
        /// 传输进度改变事件
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

        public ApeFtpSenderEngine(ITransferDataSource dataSource, uint defaultChunkSize = 64 * 1024, uint defaultWindowSize = 8)
        {
            DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            ChunkSize = defaultChunkSize > 0 ? defaultChunkSize : 64 * 1024;
            WindowSize = defaultWindowSize > 0 ? defaultWindowSize : 1;

            _decoder.PacketDecoded += ProcessIncomingPacket;
        }

        /// <summary>
        /// 启动文件传输协商
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (State != SessionState.Created)
                {
                    throw new InvalidOperationException($"当前状态无法启动传输: {State}");
                }

                State = SessionState.Negotiating;
                var demandReq = new DemandRequest(DataSource.Hash, DataSource.TotalLength, ChunkSize, WindowSize, DataSource.FileName);
                SendPacket(demandReq);
            }
        }

        /// <summary>
        /// 输入接收到的原始字节流（由外部网络/信道触发）
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
                if (State == SessionState.Cancelled || State == SessionState.Completed || State == SessionState.Failed)
                {
                    return;
                }

                switch (packet)
                {
                    case DemandResponse demandResp:
                        HandleDemandResponse(demandResp);
                        break;

                    case AckResponse ackResp:
                        HandleAckResponse(ackResp);
                        break;

                    case CancelResponse cancelResp:
                        State = SessionState.Cancelled;
                        Cancelled?.Invoke(this, EventArgs.Empty);
                        break;
                }
            }
        }

        private void HandleDemandResponse(DemandResponse resp)
        {
            if (resp.ResultCode == ResultCode.Completed)
            {
                // 触发秒传（目标端已有相同文件）
                State = SessionState.Completed;
                ProgressChanged?.Invoke(this, new TransferProgressEventArgs(DataSource.Hash, DataSource.TotalLength, DataSource.TotalLength, 0, 0));
                Completed?.Invoke(this, new TransferCompletedEventArgs(DataSource.Hash, DataSource.TotalLength, isFastUpload: true));
                return;
            }

            if (resp.ResultCode == ResultCode.ChunkSizeTooLarge)
            {
                // 缩小单段大小重新协商
                uint newChunkSize = resp.AcceptedChunkSize > 0 ? resp.AcceptedChunkSize : (uint)(ChunkSize * 0.75);
                if (newChunkSize < 64)
                {
                    Fail(ResultCode.ChunkSizeTooLarge, "分段协商尺寸过小，无法继续传输");
                    return;
                }

                ChunkSize = newChunkSize;
                var demandReq = new DemandRequest(DataSource.Hash, DataSource.TotalLength, ChunkSize, WindowSize, DataSource.FileName);
                SendPacket(demandReq);
                return;
            }

            if (resp.ResultCode != ResultCode.Success)
            {
                Fail(resp.ResultCode, resp.Message ?? $"协商被拒绝，错误码: {resp.ResultCode}");
                return;
            }

            // 协商成功，进入传输阶段
            if (resp.AcceptedChunkSize > 0) ChunkSize = resp.AcceptedChunkSize;
            if (resp.AcceptedWindowSize > 0) WindowSize = resp.AcceptedWindowSize;

            _totalChunkCount = (uint)Math.Ceiling(DataSource.TotalLength / (double)ChunkSize);
            if (_totalChunkCount == 0 && DataSource.TotalLength == 0)
            {
                // 0 字节空文件直接完成
                State = SessionState.Completed;
                Completed?.Invoke(this, new TransferCompletedEventArgs(DataSource.Hash, 0));
                return;
            }

            // 支持断点续传起始偏移
            if (resp.ResumedOffset > 0 && resp.ResumedOffset < DataSource.TotalLength)
            {
                _currentOffset = resp.ResumedOffset;
                _nextChunkIndex = (uint)(_currentOffset / ChunkSize);
                _ackedChunkIndex = _nextChunkIndex > 0 ? _nextChunkIndex - 1 : 0;
            }
            else
            {
                _currentOffset = 0;
                _nextChunkIndex = 0;
                _ackedChunkIndex = 0;
            }

            State = SessionState.Transferring;
            SendNextWindow();
        }

        private void HandleAckResponse(AckResponse ack)
        {
            if (ack.ResultCode == ResultCode.Completed)
            {
                State = SessionState.Completed;
                ProgressChanged?.Invoke(this, new TransferProgressEventArgs(DataSource.Hash, DataSource.TotalLength, DataSource.TotalLength, _totalChunkCount, _totalChunkCount));
                Completed?.Invoke(this, new TransferCompletedEventArgs(DataSource.Hash, DataSource.TotalLength, isFastUpload: false));
                return;
            }

            if (ack.ResultCode == ResultCode.ChunkCrcMismatch || ack.ResultCode == ResultCode.InvalidChunkIndex)
            {
                // 回退到请求重传的分片序号
                _nextChunkIndex = ack.AckChunkIndex;
                _currentOffset = (ulong)_nextChunkIndex * ChunkSize;
                SendNextWindow();
                return;
            }

            if (ack.ResultCode != ResultCode.Success)
            {
                Fail(ack.ResultCode, ack.Message ?? $"传输被中断，错误码: {ack.ResultCode}");
                return;
            }

            // 正常 ACK 确认
            _ackedChunkIndex = ack.AckChunkIndex;
            ulong ackedBytes = Math.Min(DataSource.TotalLength, (ulong)(_ackedChunkIndex + 1) * ChunkSize);
            ProgressChanged?.Invoke(this, new TransferProgressEventArgs(DataSource.Hash, ackedBytes, DataSource.TotalLength, _ackedChunkIndex + 1, _totalChunkCount));

            if (_currentOffset < DataSource.TotalLength)
            {
                SendNextWindow();
            }
        }

        private void SendNextWindow()
        {
            uint sentInBurst = 0;

            while (sentInBurst < WindowSize && _currentOffset < DataSource.TotalLength)
            {
                int count = (int)Math.Min((ulong)ChunkSize, DataSource.TotalLength - _currentOffset);
                byte[] chunkData = DataSource.ReadChunk(_currentOffset, count);

                var dataPacket = new DataPacket(DataSource.Hash, _nextChunkIndex, _currentOffset, chunkData);
                SendPacket(dataPacket);

                _currentOffset += (ulong)count;
                _nextChunkIndex++;
                sentInBurst++;
            }
        }

        private void SendPacket(IApeFtpPacket packet)
        {
            byte[] encoded = ApeFtpFrameEncoder.Encode(packet);
            PacketReadyToSend?.Invoke(this, new PacketToSendEventArgs(packet, encoded));
        }

        /// <summary>
        /// 取消当前传输任务
        /// </summary>
        public void Cancel(string? reason = null)
        {
            lock (_lock)
            {
                if (State == SessionState.Completed || State == SessionState.Cancelled || State == SessionState.Failed)
                {
                    return;
                }

                State = SessionState.Cancelled;
                var cancelReq = new CancelRequest(DataSource.Hash, 0, reason);
                SendPacket(cancelReq);
                Cancelled?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Fail(ResultCode code, string message)
        {
            State = SessionState.Failed;
            Failed?.Invoke(this, new TransferFailedEventArgs(DataSource.Hash, code, message));
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_isDisposed)
                {
                    DataSource.Dispose();
                    _isDisposed = true;
                }
            }
        }
    }
}
