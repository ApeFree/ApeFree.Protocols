using STTech.BytesIO.Core.Component;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

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

        public ApeFtpSender(Action<byte[]> sendBytesHandler) : base(sendBytesHandler)
        {
            Sessions = new List<TransferSession>();
        }

        public TransferSession GetSession(byte[] md5, uint fileLength)
        {
            return Sessions.FirstOrDefault(s => s.TotalLength == fileLength && s.MD5.SequenceEqual(md5));
        }

        public void SendFile(string path)
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

            OnSessionCreated(session);
        }

        protected void OnSessionCreated(TransferSession session)
        {
            session.State = SessionState.Created;
            session.SegmentIndex = 0;
            session.SegmentCount = 0;
            Sessions.Add(session);
            DemandRequest demandRequest = new DemandRequest(session.MD5, session.TotalLength, session.SegmentLength);
            SendBytesHandler.Invoke(demandRequest.GetBytes());
        }

        protected void OnSessionCompleted(TransferSession session)
        {
            session.State = SessionState.Completed;
            session.Stream?.Dispose();
            Sessions.Remove(session);

            // TODO: 通过事件通知
        }

        protected void OnSessionCancelled(TransferSession session)
        {
            session.State = SessionState.Cancelled;
            session.Stream?.Dispose();
            Sessions.Remove(session);

            // TODO: 通过事件通知
        }

        protected void OnSessionFailedInterrupted(TransferSession session, ResultCode resultCode)
        {
            session.State = SessionState.FailedInterrupted;
            session.Stream?.Dispose();
            Sessions.Remove(session);

            // TODO: 通过事件通知
        }

        protected void OnTransferSessionContinue(TransferSession session)
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

            var bytes = request.GetBytes();
            SendBytesHandler(bytes);
        }

        protected override void OnUnpackerDataParsed(object sender, DataParsedEventArgs e)
        {
            CommandCode command = (CommandCode)e.Data.FirstOrDefault();

            if (command != CommandCode.TransferResponse)
            {
                return;
            }

            var resp = new TransferResponse(e.Data);

            var session = GetSession(resp.Md5, resp.TotalLength);
            if (session == null)
            {
                return;
            }

            switch (resp.ResultCode)
            {
                case ResultCode.Continue:
                    {
                        OnTransferSessionContinue(session);
                    }
                    break;
                case ResultCode.Completed:
                    {
                        OnSessionCompleted(session);
                    }
                    break;
                case ResultCode.Cancelled:
                    {
                        OnSessionCancelled(session);
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
                        SendBytesHandler.Invoke(demandRequest.GetBytes());
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
                        OnSessionCreated(session);
                    }
                    break;
            }
        }

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
        /// Represents the state of a session.
        /// </summary>
        public enum SessionState
        {
            /// <summary>
            /// 已创建<br/>
            /// The session has been created.
            /// </summary>
            Created,
            /// <summary>
            /// 准备传输<br/>
            /// The session is ready to transfer.
            /// </summary>
            ReadyToTransfer,
            /// <summary>
            /// 传输中<br/>
            /// The session is currently transferring.
            /// </summary>
            Transferring,
            /// <summary>
            /// 已完成<br/>
            /// The session transfer has been completed.
            /// </summary>
            Completed,
            /// <summary>
            /// 已取消<br/>
            /// The session transfer has been cancelled.
            /// </summary>
            Cancelled,
            /// <summary>
            /// 错误中断<br/>
            /// The session transfer has been interrupted due to failure.
            /// </summary>
            FailedInterrupted,
        }
    }
}
