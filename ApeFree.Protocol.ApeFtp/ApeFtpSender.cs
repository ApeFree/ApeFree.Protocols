using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ApeFree.Protocol.ApeFtp
{
    public class ApeFtpSender : ApeFtpClient
    {
        /// <summary>
        /// 传输事务列表
        /// </summary>
        public List<TransferSession> Sessions { get; }

        /// <summary>
        /// 默认段数据长度
        /// </summary>
        public uint DefaultSegmentSize { get; set; } = 512 * 1024;           // 默认段最大长度为512KB

        public ApeFtpSender(BytesClient client) : base(client)
        {
            Sessions = new List<TransferSession>();
        }

        public TransferSession GetSession(byte[] md5, uint fileLength)
        {
            return Sessions.FirstOrDefault(s => s.TotalLength == fileLength && s.MD5.SequenceEqual(md5));
        }

        public async Task SendFileAsync(string path)
        {
            var fileBytes = File.ReadAllBytes(path);
            var md5 = fileBytes.GetMD5();

            TransferSession session = new TransferSession()
            {
                MD5 = md5,
                FilePath = path,
                TotalLength = (uint)fileBytes.Length,
                SegmentLength = DefaultSegmentSize,
            };

            await OnSessionCreatedAsync(session);
        }

        protected async Task OnSessionCreatedAsync(TransferSession session)
        {
            session.State = SessionState.Created;
            session.SegmentIndex = 0;
            session.SegmentCount = 0;
            Sessions.Add(session);
            DemandRequest demandRequest = new DemandRequest(session.MD5, session.TotalLength, session.SegmentLength);
            await InnerClient.SendAsync(demandRequest);
        }

        protected void HandleSessionCompleted(TransferSession session)
        {
            session.State = SessionState.Completed;
            session.Stream?.Dispose();
            Sessions.Remove(session);

            // 触发完成事件
            OnSessionCompleted?.Invoke(this, new SessionEventArgs(session));
        }

        protected void HandleSessionCancelled(TransferSession session)
        {
            session.State = SessionState.Cancelled;
            session.Stream?.Dispose();
            Sessions.Remove(session);

            // 触发取消事件
            OnSessionCancelled?.Invoke(this, new SessionEventArgs(session));
        }

        protected void OnSessionFailedInterrupted(TransferSession session, ResultCode resultCode)
        {
            session.State = SessionState.FailedInterrupted;
            session.Stream?.Dispose();
            Sessions.Remove(session);

            // 触发失败事件
            OnSessionFailed?.Invoke(this, new SessionFailedEventArgs(session, resultCode));
        }

        protected async Task OnTransferSessionContinueAsync(TransferSession session)
        {
            // 首次段长度协商成功
            if (session.State == SessionState.Created)
            {
                // 计算总段数
                session.SegmentCount = (ushort)Math.Ceiling(session.TotalLength / (double)session.SegmentLength);
                session.SegmentIndex = 0;
                session.State = SessionState.Transferring;

                // 初始化文件流
                if (session.Stream == null)
                {
                    session.Stream = new FileStream(session.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x1000, FileOptions.SequentialScan);
                }
                else
                {
                    session.Stream.Position = 0;
                }
            }

            // 构造传输体
            TransferRequest request = new TransferRequest(session.MD5, session.TotalLength)
            {
                SegmentIndex = session.SegmentIndex++,
                FunctionCode = FunctionCode.Send,
                SegmentCount = session.SegmentCount,
            };
            var data = new byte[session.SegmentLength];
            request.CurrentSegmentLength = (uint)session.Stream.Read(data, 0, (int)session.SegmentLength);
            if (session.SegmentLength == request.CurrentSegmentLength)
            {
                request.Data = data;
            }
            else
            {
                request.Data = data.Take((int)request.CurrentSegmentLength).ToArray();
            }

            await InnerClient.SendAsync(request);
        }

        protected override async void OnUnpackerDataParsed(object sender, DataParsedEventArgs<TransferResponse> e)
        {
            var resp = e.Data;
            var session = GetSession(resp.Md5, resp.TotalLength);
            if (session == null)
            {
                return;
            }

            switch (resp.ResultCode)
            {
                case ResultCode.Continue:
                    {
                        await OnTransferSessionContinueAsync(session);
                    }
                    break;
                case ResultCode.Completed:
                    {
                        HandleSessionCompleted(session);
                    }
                    break;
                case ResultCode.Cancelled:
                    {
                        HandleSessionCancelled(session);
                    }
                    break;
                case ResultCode.SegmentSizeTooLarge:
                    {
                        // 重新协商长度
                        // 缩小单个段的长度
                        session.SegmentLength = (uint)(session.SegmentLength * 0.75);

                        // 如果段长度过小则报错
                        if (session.SegmentLength <= 1)
                        {
                            OnSessionFailedInterrupted(session, resp.ResultCode);
                            return;
                        }

                        // 重新申请文件发送
                        DemandRequest demandRequest = new DemandRequest(session.MD5, session.TotalLength, session.SegmentLength);
                        await InnerClient.SendAsync(demandRequest);
                    }
                    break;
                case ResultCode.InsufficientDiskSpace:
                case ResultCode.FileSizeTooLarge:
                case ResultCode.InvalidTransferTask:
                case ResultCode.InvalidSegmentIndex:
                case ResultCode.SameFileTransmitting:
                    {
                        OnSessionFailedInterrupted(session, resp.ResultCode);
                    }
                    break;
                case ResultCode.Md5Mismatching:
                    {
                        // 重新传输
                        Sessions.Remove(session);
                        await OnSessionCreatedAsync(session);
                    }
                    break;
            }
        }

        /// <summary>
        /// 当会话完成时触发
        /// </summary>
        public event EventHandler<SessionEventArgs> OnSessionCompleted;

        /// <summary>
        /// 当会话取消时触发
        /// </summary>
        public event EventHandler<SessionEventArgs> OnSessionCancelled;

        /// <summary>
        /// 当会话失败时触发
        /// </summary>
        public event EventHandler<SessionFailedEventArgs> OnSessionFailed;

        /// <summary>
        /// 传输事务（一次传输任务）
        /// </summary>
        public class TransferSession
        {
            /// <summary>
            /// 事务状态
            /// </summary>
            public SessionState State { get; set; }

            /// <summary>
            /// 段长度
            /// </summary>
            public uint SegmentLength { get; set; }

            /// <summary>
            /// 文件MD5
            /// </summary>
            public byte[] MD5 { get; set; }

            /// <summary>
            /// 文件总长度
            /// </summary>
            public uint TotalLength { get; set; }

            /// <summary>
            /// 总段数
            /// </summary>
            public ushort SegmentCount { get; set; }

            /// <summary>
            /// 当前段序号
            /// </summary>
            public ushort SegmentIndex { get; set; }

            /// <summary>
            /// 文件路径
            /// </summary>
            public string FilePath { get; internal set; }

            /// <summary>
            /// 文件流
            /// </summary>
            internal FileStream Stream { get; set; }
        }

        /// <summary>
        /// 会话状态
        /// </summary>
        public enum SessionState
        {
            /// <summary>
            /// 已创建
            /// </summary>
            Created,
            /// <summary>
            /// 准备传输
            /// </summary>
            ReadyToTransfer,
            /// <summary>
            /// 传输中
            /// </summary>
            Transferring,
            /// <summary>
            /// 已完成
            /// </summary>
            Completed,
            /// <summary>
            /// 已取消
            /// </summary>
            Cancelled,
            /// <summary>
            /// 错误中断
            /// </summary>
            FailedInterrupted,
        }

        /// <summary>
        /// 会话事件参数
        /// </summary>
        public class SessionEventArgs : EventArgs
        {
            /// <summary>
            /// 会话对象
            /// </summary>
            public TransferSession Session { get; }

            public SessionEventArgs(TransferSession session)
            {
                Session = session;
            }
        }

        /// <summary>
        /// 会话失败事件参数
        /// </summary>
        public class SessionFailedEventArgs : SessionEventArgs
        {
            /// <summary>
            /// 失败原因
            /// </summary>
            public ResultCode ResultCode { get; }

            public SessionFailedEventArgs(TransferSession session, ResultCode resultCode) : base(session)
            {
                ResultCode = resultCode;
            }
        }
    }
}
