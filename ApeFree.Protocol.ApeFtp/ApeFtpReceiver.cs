using System;
using System.Collections.Generic;
using System.Text;

namespace ApeFree.Protocol.ApeFtp
{
    public abstract class ApeFtpReceiver
    {
        /// <summary>
        /// 最大段数据长度
        /// </summary>
        public int MaxSegmentSize { get; set; } = 512 * 1024;           // 默认段最大长度为512KB

        /// <summary>
        /// 单文件最大长度
        /// </summary
        public int MaxFileSize { get; set; } = (int)Math.Pow(1024, 3);  // 默认单文件上限为1GB

        /// <summary>
        /// 传输缓存路径
        /// </summary>
        public string TransferCachePath { get; set; }

        /// <summary>
        /// 传输完成路径
        /// </summary>
        public string TransferCompletedPath { get; set; }

        protected virtual TransferResponse OnDemandReceived(DemandRequest request)
        {
            // 构造响应实体
            var resp = new TransferResponse()
            {
                MD5 = request.MD5,
                TotalLength = request.TotalLength,
            };

            // 检查申请传输的段长度是否大于预设的最大段长度（最大段长度的取值小于缓冲区）
            if (request.SegmentMaxLength > MaxSegmentSize)
            {
                resp.ResultCode = ResultCode.SegmentSizeTooLarge;
                return resp;
            }

            // 检查申请传输的文件总长度是否大于预设的最大文件长度
            if (request.TotalLength > MaxFileSize)
            {
                resp.ResultCode = ResultCode.FileSizeTooLarge;
                return resp;
            }

            // 检查缓存区中是否已存在相同文件(通过文件的MD5和总长度匹配)
            var state = GetTransferTaskState(request.MD5, request.TotalLength);
            if (state == TransferTaskState.Completed)
            {
                resp.ResultCode = ResultCode.Completed;
                return resp;
            }

            // 如果相同的传输任务正在进行
            if (state == TransferTaskState.Transmitting)
            {
                resp.ResultCode = ResultCode.SameFileTransmitting;
                return resp;
            }


            // 创建文件缓存，创建失败或异常时返回结果码“InsufficientDiskSpace”
            try
            {
                var success = CreateFileCache(request.MD5, request.TotalLength);
                if (!success)
                {
                    resp.ResultCode = ResultCode.InsufficientDiskSpace;
                    return resp;
                }
            }
            catch (Exception ex)
            {
                resp.ResultCode = ResultCode.InsufficientDiskSpace;
                resp.Message = ex.Message;
                return resp;
            }

            // 运行本次文件传输
            resp.ResultCode = ResultCode.Continue;
            return resp;
        }

        protected virtual TransferResponse OnTransferReceived(TransferRequest request)
        {
            // 构造响应实体
            var resp = new TransferResponse()
            {
                MD5 = request.MD5,
                TotalLength = request.TotalLength,
            };

            var state = GetTransferTaskState(request.MD5, request.TotalLength);

            // 如果取消了文件传输
            if (request.Opcode == FuncCode.Cancel)
            {
                // 取消传输的指令仅对“传输中”的任务有效
                if (state == TransferTaskState.Transmitting)
                {
                    OnTransferCancelled(request.MD5, request.TotalLength);
                    return resp.With(r => r.ResultCode = ResultCode.Cancelled);
                }
                else
                {
                    return resp.With(r => r.ResultCode = ResultCode.InvalidCancelCommand);
                }
            }

            // 如果是文件传输
            else
            {
                // 检查传输任务是否经过了申请
                if (state == TransferTaskState.Nonexistent)
                {
                    return resp.With(r => r.ResultCode = ResultCode.InvalidTransferTask);
                }

                // 检查段序号是否合法（当前段序号应小于总段数）
                if (request.SegmentIndex >= request.SegmentCount)
                {
                    return resp.With(r => r.ResultCode = ResultCode.InvalidSegmentIndex);
                }

                // TODO: 还可以检查当前已接收文件的大小

                // 写入文件、写入后检查文件完整性
                return AppendSegmentToFile(request);
            }
        }

        /// <summary>
        /// 创建文件缓存
        /// </summary>
        /// <param name="md5"></param>
        /// <param name="fileLength"></param>
        /// <returns></returns>
        protected abstract bool CreateFileCache(byte[] md5, uint fileLength);

        /// <summary>
        /// 当文件传输取消时
        /// </summary>
        /// <param name="md5"></param>
        /// <param name="fileLength"></param>
        /// <returns></returns>
        protected abstract bool OnTransferCancelled(byte[] md5, uint fileLength);

        /// <summary>
        /// 获取一个传输任务的状态
        /// </summary>
        /// <param name="md5"></param>
        /// <param name="fileLength"></param>
        /// <returns></returns>
        protected abstract TransferTaskState GetTransferTaskState(byte[] md5, uint fileLength);

        protected abstract TransferResponse AppendSegmentToFile(TransferRequest request);

        public enum TransferTaskState
        {
            /// <summary>
            /// 不存在
            /// </summary>
            Nonexistent,

            /// <summary>
            /// 传输中
            /// </summary>
            Transmitting,

            /// <summary>
            /// 已完成
            /// </summary>
            Completed,
        }
    }
}
