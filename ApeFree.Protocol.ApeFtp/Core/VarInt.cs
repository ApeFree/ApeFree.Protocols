using System;
using System.IO;

namespace ApeFree.Protocol.ApeFtp.Core
{
    /// <summary>
    /// 可变长度无符号 64 位整型（VarInt - 基于 LEB128 规范）编解码工具类
    /// </summary>
    public static class VarInt
    {
        /// <summary>
        /// 计算指定 ulong 值编码后所占用的字节数（1~10 字节）
        /// </summary>
        public static int GetByteCount(ulong value)
        {
            int count = 0;
            do
            {
                count++;
                value >>= 7;
            } while (value != 0);
            return count;
        }

        /// <summary>
        /// 将 ulong 编码为 VarInt 写入 Span 中
        /// </summary>
        public static bool TryWrite(ulong value, Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;
            do
            {
                if (bytesWritten >= destination.Length)
                {
                    return false;
                }

                byte b = (byte)(value & 0x7F);
                value >>= 7;
                if (value != 0)
                {
                    b |= 0x80;
                }
                destination[bytesWritten++] = b;
            } while (value != 0);

            return true;
        }

        /// <summary>
        /// 将 ulong 编码为 VarInt 写入 Stream 中
        /// </summary>
        public static int Write(ulong value, Stream stream)
        {
            int bytesWritten = 0;
            do
            {
                byte b = (byte)(value & 0x7F);
                value >>= 7;
                if (value != 0)
                {
                    b |= 0x80;
                }
                stream.WriteByte(b);
                bytesWritten++;
            } while (value != 0);

            return bytesWritten;
        }

        /// <summary>
        /// 从 ReadOnlySpan 中解码 VarInt
        /// </summary>
        public static bool TryRead(ReadOnlySpan<byte> source, out ulong value, out int bytesRead)
        {
            value = 0;
            bytesRead = 0;
            int shift = 0;

            while (bytesRead < source.Length && bytesRead < 10)
            {
                byte b = source[bytesRead++];
                value |= ((ulong)(b & 0x7F)) << shift;

                if ((b & 0x80) == 0)
                {
                    return true;
                }

                shift += 7;
            }

            value = 0;
            bytesRead = 0;
            return false;
        }

        /// <summary>
        /// 从 Stream 中解码 VarInt
        /// </summary>
        public static bool TryRead(Stream stream, out ulong value, out int bytesRead)
        {
            value = 0;
            bytesRead = 0;
            int shift = 0;

            while (bytesRead < 10)
            {
                int b = stream.ReadByte();
                if (b == -1)
                {
                    value = 0;
                    return false;
                }

                bytesRead++;
                value |= ((ulong)(b & 0x7F)) << shift;

                if ((b & 0x80) == 0)
                {
                    return true;
                }

                shift += 7;
            }

            value = 0;
            return false;
        }
    }
}
