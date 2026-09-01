using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ApeFree.Protocol.ApeFtp.Core;
using ApeFree.Protocol.ApeFtp.Core.Packets;

namespace ApeFree.Protocol.ApeFtp.Codec
{
    /// <summary>
    /// ApeFtp 报文帧解码器（支持零拷贝 Span 解析、流式粘包/半包与防重入递归处理）
    /// </summary>
    public class ApeFtpFrameDecoder
    {
        private readonly List<byte> _buffer = new List<byte>();
        private readonly object _lock = new object();
        private bool _isProcessing = false;

        /// <summary>
        /// 当成功解析出一个完整的数据包时触发
        /// </summary>
        public event Action<IApeFtpPacket>? PacketDecoded;

        /// <summary>
        /// 当检测到损坏的数据包或 CRC 校验失败时触发
        /// </summary>
        public event Action<string>? DecodeErrorOccurred;

        /// <summary>
        /// 输入接收到的字节流片段进行解包处理（防重入设计，支持同步环路调用）
        /// </summary>
        public void Feed(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            lock (_lock)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    _buffer.Add(data[i]);
                }

                if (_isProcessing)
                {
                    // 如果已经在上层调用栈中循环处理，直接追加数据并返回，由外层循环继续消费
                    return;
                }

                _isProcessing = true;
            }

            try
            {
                while (true)
                {
                    IApeFtpPacket? packetToDispatch = null;
                    string? errorToDispatch = null;

                    lock (_lock)
                    {
                        if (!TryExtractNextPacket(out packetToDispatch, out errorToDispatch))
                        {
                            break;
                        }
                    }

                    if (errorToDispatch != null)
                    {
                        DecodeErrorOccurred?.Invoke(errorToDispatch);
                    }

                    if (packetToDispatch != null)
                    {
                        PacketDecoded?.Invoke(packetToDispatch);
                    }
                }
            }
            finally
            {
                lock (_lock)
                {
                    _isProcessing = false;
                }
            }
        }

        /// <summary>
        /// 输入字节数组
        /// </summary>
        public void Feed(byte[] data, int offset, int count)
        {
            Feed(data.AsSpan(offset, count));
        }

        /// <summary>
        /// 从缓冲区中尝试提取下一个完整数据包
        /// </summary>
        private bool TryExtractNextPacket(out IApeFtpPacket? packet, out string? error)
        {
            packet = null;
            error = null;

            while (_buffer.Count >= 3)
            {
                // 1. 寻找帧头魔数 (0xAF, 0x46)
                int magicIndex = FindMagicIndex();
                if (magicIndex < 0)
                {
                    if (_buffer.Count > 1)
                    {
                        byte lastByte = _buffer[_buffer.Count - 1];
                        _buffer.Clear();
                        if (lastByte == ApeFtpFrameEncoder.Magic[0])
                        {
                            _buffer.Add(lastByte);
                        }
                    }
                    return false;
                }

                if (magicIndex > 0)
                {
                    _buffer.RemoveRange(0, magicIndex);
                }

                if (_buffer.Count < 3)
                {
                    return false;
                }

                PacketType packetType = (PacketType)_buffer[2];

                // 2. 读取 PayloadLength (VarInt)
                ReadOnlySpan<byte> spanAfterType = _buffer.ToArray().AsSpan(3);
                if (!VarInt.TryRead(spanAfterType, out ulong payloadLength, out int varIntBytes))
                {
                    return false;
                }

                int totalFrameSize = 2 + 1 + varIntBytes + (int)payloadLength + 4;
                if (_buffer.Count < totalFrameSize)
                {
                    // 半包，等待后续数据
                    return false;
                }

                // 3. 提取 Payload 与 CRC
                int payloadOffset = 3 + varIntBytes;
                byte[] frameBytes = _buffer.ToArray();
                ReadOnlySpan<byte> payloadSpan = frameBytes.AsSpan(payloadOffset, (int)payloadLength);

                int crcOffset = payloadOffset + (int)payloadLength;
                uint expectedCrc = ((uint)frameBytes[crcOffset] << 24) |
                                  ((uint)frameBytes[crcOffset + 1] << 16) |
                                  ((uint)frameBytes[crcOffset + 2] << 8) |
                                  (uint)frameBytes[crcOffset + 3];

                uint actualCrc = Crc32.Compute(payloadSpan);
                if (actualCrc != expectedCrc)
                {
                    error = $"CRC32 校验失败: 期望 0x{expectedCrc:X8}, 实际 0x{actualCrc:X8}。跳过当前魔数以恢复同步。";
                    _buffer.RemoveRange(0, 2);
                    return true;
                }

                // 4. 解析 Payload 为 Packet 实体
                try
                {
                    packet = DecodePayload(packetType, payloadSpan);
                    _buffer.RemoveRange(0, totalFrameSize);
                    return true;
                }
                catch (Exception ex)
                {
                    error = $"Payload 解析异常: {ex.Message}";
                    _buffer.RemoveRange(0, totalFrameSize);
                    return true;
                }
            }

            return false;
        }

        private int FindMagicIndex()
        {
            for (int i = 0; i < _buffer.Count - 1; i++)
            {
                if (_buffer[i] == ApeFtpFrameEncoder.Magic[0] && _buffer[i + 1] == ApeFtpFrameEncoder.Magic[1])
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 尝试单帧直接解码
        /// </summary>
        public static bool TryDecodeSingleFrame(ReadOnlySpan<byte> frame, out IApeFtpPacket? packet, out int bytesConsumed)
        {
            packet = null;
            bytesConsumed = 0;

            if (frame.Length < 7)
            {
                return false;
            }

            if (frame[0] != ApeFtpFrameEncoder.Magic[0] || frame[1] != ApeFtpFrameEncoder.Magic[1])
            {
                return false;
            }

            PacketType packetType = (PacketType)frame[2];
            if (!VarInt.TryRead(frame.Slice(3), out ulong payloadLength, out int varIntBytes))
            {
                return false;
            }

            int totalFrameSize = 3 + varIntBytes + (int)payloadLength + 4;
            if (frame.Length < totalFrameSize)
            {
                return false;
            }

            int payloadOffset = 3 + varIntBytes;
            ReadOnlySpan<byte> payloadSpan = frame.Slice(payloadOffset, (int)payloadLength);

            int crcOffset = payloadOffset + (int)payloadLength;
            uint expectedCrc = ((uint)frame[crcOffset] << 24) |
                              ((uint)frame[crcOffset + 1] << 16) |
                              ((uint)frame[crcOffset + 2] << 8) |
                              (uint)frame[crcOffset + 3];

            if (Crc32.Compute(payloadSpan) != expectedCrc)
            {
                return false;
            }

            packet = DecodePayload(packetType, payloadSpan);
            bytesConsumed = totalFrameSize;
            return true;
        }

        private static IApeFtpPacket DecodePayload(PacketType type, ReadOnlySpan<byte> payload)
        {
            int offset = 0;

            // 1. 读取 FileKey
            if (!VarInt.TryRead(payload.Slice(offset), out ulong keyLen, out int keyVarIntLen))
            {
                throw new InvalidDataException("无法读取 FileKey 长度");
            }
            offset += keyVarIntLen;

            byte[] fileKey = payload.Slice(offset, (int)keyLen).ToArray();
            offset += (int)keyLen;

            switch (type)
            {
                case PacketType.DemandRequest:
                    {
                        VarInt.TryRead(payload.Slice(offset), out ulong totalLen, out int totalVarInt);
                        offset += totalVarInt;

                        VarInt.TryRead(payload.Slice(offset), out ulong chunkSize, out int chunkVarInt);
                        offset += chunkVarInt;

                        VarInt.TryRead(payload.Slice(offset), out ulong windowSize, out int winVarInt);
                        offset += winVarInt;

                        string? fileName = ReadString(payload, ref offset);
                        return new DemandRequest(fileKey, totalLen, (uint)chunkSize, (uint)windowSize, fileName);
                    }

                case PacketType.DemandResponse:
                    {
                        ResultCode code = (ResultCode)payload[offset++];

                        VarInt.TryRead(payload.Slice(offset), out ulong acceptedChunk, out int chunkVarInt);
                        offset += chunkVarInt;

                        VarInt.TryRead(payload.Slice(offset), out ulong acceptedWin, out int winVarInt);
                        offset += winVarInt;

                        VarInt.TryRead(payload.Slice(offset), out ulong resumedOff, out int offVarInt);
                        offset += offVarInt;

                        string? msg = ReadString(payload, ref offset);
                        return new DemandResponse(fileKey, code, (uint)acceptedChunk, (uint)acceptedWin, resumedOff, msg);
                    }

                case PacketType.DataPacket:
                    {
                        VarInt.TryRead(payload.Slice(offset), out ulong chunkIndex, out int indexVarInt);
                        offset += indexVarInt;

                        VarInt.TryRead(payload.Slice(offset), out ulong chunkOffset, out int offVarInt);
                        offset += offVarInt;

                        uint chunkCrc = ((uint)payload[offset] << 24) |
                                        ((uint)payload[offset + 1] << 16) |
                                        ((uint)payload[offset + 2] << 8) |
                                        (uint)payload[offset + 3];
                        offset += 4;

                        VarInt.TryRead(payload.Slice(offset), out ulong dataLen, out int dataVarInt);
                        offset += dataVarInt;

                        byte[] data = payload.Slice(offset, (int)dataLen).ToArray();
                        offset += (int)dataLen;

                        return new DataPacket(fileKey, (uint)chunkIndex, chunkOffset, data, chunkCrc);
                    }

                case PacketType.AckResponse:
                    {
                        ResultCode code = (ResultCode)payload[offset++];

                        VarInt.TryRead(payload.Slice(offset), out ulong ackIndex, out int ackVarInt);
                        offset += ackVarInt;

                        VarInt.TryRead(payload.Slice(offset), out ulong ackCount, out int cntVarInt);
                        offset += cntVarInt;

                        string? msg = ReadString(payload, ref offset);
                        return new AckResponse(fileKey, code, (uint)ackIndex, (uint)ackCount, msg);
                    }

                case PacketType.CancelRequest:
                    {
                        byte reason = payload[offset++];
                        string? msg = ReadString(payload, ref offset);
                        return new CancelRequest(fileKey, reason, msg);
                    }

                case PacketType.CancelResponse:
                    {
                        ResultCode code = (ResultCode)payload[offset++];
                        return new CancelResponse(fileKey, code);
                    }

                default:
                    throw new NotSupportedException($"未知的 PacketType: {type}");
            }
        }

        private static string? ReadString(ReadOnlySpan<byte> span, ref int offset)
        {
            if (offset >= span.Length)
            {
                return null;
            }

            if (!VarInt.TryRead(span.Slice(offset), out ulong strLen, out int varIntBytes))
            {
                return null;
            }
            offset += varIntBytes;

            if (strLen == 0)
            {
                return null;
            }

            string result = Encoding.UTF8.GetString(span.Slice(offset, (int)strLen).ToArray());
            offset += (int)strLen;
            return result;
        }

        /// <summary>
        /// 清空解码器内部缓冲区
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _buffer.Clear();
            }
        }
    }
}
