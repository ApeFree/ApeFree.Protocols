using System;

namespace ApeFree.Protocol.ApeFtp.Core
{
    /// <summary>
    /// 标准 IEEE 802.3 CRC32 校验算法实现
    /// </summary>
    public static class Crc32
    {
        private const uint Polynomial = 0xEDB88320;
        private static readonly uint[] Table = new uint[256];

        static Crc32()
        {
            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 8; j > 0; j--)
                {
                    if ((crc & 1) == 1)
                    {
                        crc = (crc >> 1) ^ Polynomial;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
                Table[i] = crc;
            }
        }

        /// <summary>
        /// 计算字节缓冲区的 CRC32 校验和
        /// </summary>
        public static uint Compute(ReadOnlySpan<byte> bytes)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = 0; i < bytes.Length; i++)
            {
                byte index = (byte)((crc & 0xFF) ^ bytes[i]);
                crc = (crc >> 8) ^ Table[index];
            }
            return ~crc;
        }

        /// <summary>
        /// 计算字节数组的 CRC32 校验和
        /// </summary>
        public static uint Compute(byte[] buffer, int offset, int count)
        {
            return Compute(buffer.AsSpan(offset, count));
        }
    }
}
