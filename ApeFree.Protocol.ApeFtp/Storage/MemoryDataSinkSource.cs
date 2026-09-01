using System;
using System.Security.Cryptography;

namespace ApeFree.Protocol.ApeFtp.Storage
{
    /// <summary>
    /// 基于内存字节数组的数据源实现
    /// </summary>
    public class MemoryDataSource : ITransferDataSource
    {
        private readonly byte[] _data;

        public ulong TotalLength => (ulong)_data.Length;
        public byte[] Hash { get; }
        public string? FileName { get; }

        public MemoryDataSource(byte[] data, string? fileName = null, byte[]? customHash = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            FileName = fileName;

            if (customHash != null)
            {
                Hash = customHash;
            }
            else
            {
                using var md5 = MD5.Create();
                Hash = md5.ComputeHash(_data);
            }
        }

        public int ReadChunk(ulong offset, Span<byte> destination)
        {
            if (offset >= (ulong)_data.Length)
            {
                return 0;
            }

            int count = (int)Math.Min((ulong)destination.Length, (ulong)_data.Length - offset);
            _data.AsSpan((int)offset, count).CopyTo(destination);
            return count;
        }

        public byte[] ReadChunk(ulong offset, int count)
        {
            if (offset >= (ulong)_data.Length)
            {
                return Array.Empty<byte>();
            }

            int actualCount = (int)Math.Min((ulong)count, (ulong)_data.Length - offset);
            byte[] chunk = new byte[actualCount];
            Buffer.BlockCopy(_data, (int)offset, chunk, 0, actualCount);
            return chunk;
        }

        public byte[] GetRawData() => _data;

        public void Dispose()
        {
            // 内存实现无需特别释放
        }
    }

    /// <summary>
    /// 基于内存缓冲区的数据写入目标实现
    /// </summary>
    public class MemoryDataSink : ITransferDataSink
    {
        private readonly byte[] _buffer;
        private ulong _maxWrittenOffset = 0;
        private bool _isDisposed = false;

        public ulong TotalLength { get; }
        public string? TargetPath { get; }

        public MemoryDataSink(ulong totalLength, string? targetPath = null)
        {
            TotalLength = totalLength;
            TargetPath = targetPath;
            _buffer = new byte[totalLength];
        }

        public void WriteChunk(ulong offset, ReadOnlySpan<byte> data)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(MemoryDataSink));
            if (offset >= TotalLength) return;

            int writeLen = (int)Math.Min((ulong)data.Length, TotalLength - offset);
            data.Slice(0, writeLen).CopyTo(_buffer.AsSpan((int)offset, writeLen));

            ulong currentEnd = offset + (ulong)writeLen;
            if (currentEnd > _maxWrittenOffset)
            {
                _maxWrittenOffset = currentEnd;
            }
        }

        public ulong GetCurrentLength() => _maxWrittenOffset;

        public bool VerifyAndFinalize(byte[] expectedHash)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(MemoryDataSink));
            if (expectedHash == null || expectedHash.Length == 0) return true;

            using var md5 = MD5.Create();
            byte[] actualHash = md5.ComputeHash(_buffer);

            if (actualHash.Length != expectedHash.Length) return false;
            for (int i = 0; i < actualHash.Length; i++)
            {
                if (actualHash[i] != expectedHash[i]) return false;
            }

            return true;
        }

        public byte[] GetBuffer() => _buffer;

        public void Abort()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _maxWrittenOffset = 0;
        }

        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}
