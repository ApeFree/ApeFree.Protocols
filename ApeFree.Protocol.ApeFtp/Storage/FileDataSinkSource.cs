using System;
using System.IO;
using System.Security.Cryptography;

namespace ApeFree.Protocol.ApeFtp.Storage
{
    /// <summary>
    /// 基于本地文件的流式数据源（零全量内存加载，高效支持超大文件）
    /// </summary>
    public class FileDataSource : ITransferDataSource
    {
        private readonly FileStream _fileStream;
        private bool _isDisposed = false;

        public ulong TotalLength { get; }
        public byte[] Hash { get; }
        public string? FileName { get; }
        public string FilePath { get; }

        public FileDataSource(string filePath, byte[]? customHash = null)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("指定的文件不存在", filePath);

            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            var fileInfo = new FileInfo(filePath);
            TotalLength = (ulong)fileInfo.Length;

            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64 * 1024, FileOptions.SequentialScan);

            if (customHash != null)
            {
                Hash = customHash;
            }
            else
            {
                // 流式计算文件 MD5，避免全量读入内存导致的 OOM
                using var md5 = MD5.Create();
                _fileStream.Position = 0;
                Hash = md5.ComputeHash(_fileStream);
                _fileStream.Position = 0;
            }
        }

        public int ReadChunk(ulong offset, Span<byte> destination)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(FileDataSource));
            if (offset >= TotalLength) return 0;

            lock (_fileStream)
            {
                _fileStream.Seek((long)offset, SeekOrigin.Begin);
                byte[] temp = new byte[destination.Length];
                int read = _fileStream.Read(temp, 0, destination.Length);
                temp.AsSpan(0, read).CopyTo(destination);
                return read;
            }
        }

        public byte[] ReadChunk(ulong offset, int count)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(FileDataSource));
            if (offset >= TotalLength) return Array.Empty<byte>();

            lock (_fileStream)
            {
                _fileStream.Seek((long)offset, SeekOrigin.Begin);
                int toRead = (int)Math.Min((ulong)count, TotalLength - offset);
                byte[] buffer = new byte[toRead];
                int read = _fileStream.Read(buffer, 0, toRead);
                if (read < toRead)
                {
                    Array.Resize(ref buffer, read);
                }
                return buffer;
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _fileStream.Dispose();
                _isDisposed = true;
            }
        }
    }

    /// <summary>
    /// 基于本地文件的分片随机写入与校验目标实现（支持断点续传与原子落盘）
    /// </summary>
    public class FileDataSink : ITransferDataSink
    {
        private readonly string _finalPath;
        private readonly string _partPath;
        private readonly FileStream _fileStream;
        private bool _isDisposed = false;

        public ulong TotalLength { get; }
        public string? TargetPath => _finalPath;

        public FileDataSink(string finalPath, ulong totalLength, bool appendOrResume = true)
        {
            if (string.IsNullOrEmpty(finalPath)) throw new ArgumentNullException(nameof(finalPath));

            _finalPath = finalPath;
            _partPath = finalPath + ".apeftp.part";
            TotalLength = totalLength;

            var dir = Path.GetDirectoryName(finalPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            FileMode mode = appendOrResume ? FileMode.OpenOrCreate : FileMode.Create;
            _fileStream = new FileStream(_partPath, mode, FileAccess.ReadWrite, FileShare.ReadWrite, 64 * 1024, FileOptions.RandomAccess);

            if (!appendOrResume)
            {
                _fileStream.SetLength((long)totalLength);
            }
        }

        public void WriteChunk(ulong offset, ReadOnlySpan<byte> data)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(FileDataSink));
            if (offset >= TotalLength) return;

            lock (_fileStream)
            {
                _fileStream.Seek((long)offset, SeekOrigin.Begin);
                byte[] temp = data.ToArray();
                _fileStream.Write(temp, 0, temp.Length);
                _fileStream.Flush();
            }
        }

        public ulong GetCurrentLength()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(FileDataSink));
            lock (_fileStream)
            {
                return (ulong)_fileStream.Length;
            }
        }

        public bool VerifyAndFinalize(byte[] expectedHash)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(FileDataSink));

            lock (_fileStream)
            {
                _fileStream.Flush();

                if (expectedHash != null && expectedHash.Length > 0)
                {
                    using var md5 = MD5.Create();
                    _fileStream.Position = 0;
                    byte[] actualHash = md5.ComputeHash(_fileStream);

                    if (actualHash.Length != expectedHash.Length) return false;
                    for (int i = 0; i < actualHash.Length; i++)
                    {
                        if (actualHash[i] != expectedHash[i]) return false;
                    }
                }
            }

            _fileStream.Dispose();
            _isDisposed = true;

            // 原子重命名为目标文件
            if (File.Exists(_finalPath))
            {
                File.Delete(_finalPath);
            }
            File.Move(_partPath, _finalPath);

            return true;
        }

        public void Abort()
        {
            if (!_isDisposed)
            {
                _fileStream.Dispose();
                _isDisposed = true;
            }

            if (File.Exists(_partPath))
            {
                try { File.Delete(_partPath); } catch { }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _fileStream.Dispose();
                _isDisposed = true;
            }
        }
    }
}
