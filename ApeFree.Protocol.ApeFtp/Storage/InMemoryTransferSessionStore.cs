using System;
using System.Collections.Concurrent;
using System.Text;
using ApeFree.Protocol.ApeFtp.Core;

namespace ApeFree.Protocol.ApeFtp.Storage
{
    /// <summary>
    /// 基于内存并发字典的会话状态仓储默认实现
    /// </summary>
    public class InMemoryTransferSessionStore : ITransferSessionStore
    {
        private readonly ConcurrentDictionary<string, TransferSessionRecord> _sessions = new ConcurrentDictionary<string, TransferSessionRecord>();

        private static string GetKeyString(byte[] key)
        {
            if (key == null || key.Length == 0) return string.Empty;
            var sb = new StringBuilder(key.Length * 2);
            foreach (var b in key)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        public TransferSessionRecord? GetSession(byte[] fileKey)
        {
            var keyStr = GetKeyString(fileKey);
            _sessions.TryGetValue(keyStr, out var session);
            return session;
        }

        public void SaveOrUpdateSession(TransferSessionRecord session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var keyStr = GetKeyString(session.FileKey);
            session.LastUpdatedTime = DateTime.UtcNow;
            _sessions.AddOrUpdate(keyStr, session, (k, old) => session);
        }

        public void UpdateProgress(byte[] fileKey, ulong receivedBytes, uint ackedChunkIndex)
        {
            var keyStr = GetKeyString(fileKey);
            if (_sessions.TryGetValue(keyStr, out var session))
            {
                session.ReceivedBytes = receivedBytes;
                session.LastAckedChunkIndex = ackedChunkIndex;
                session.LastUpdatedTime = DateTime.UtcNow;
            }
        }

        public void UpdateState(byte[] fileKey, SessionState state)
        {
            var keyStr = GetKeyString(fileKey);
            if (_sessions.TryGetValue(keyStr, out var session))
            {
                session.State = state;
                session.LastUpdatedTime = DateTime.UtcNow;
            }
        }

        public bool RemoveSession(byte[] fileKey)
        {
            var keyStr = GetKeyString(fileKey);
            return _sessions.TryRemove(keyStr, out _);
        }

        public bool Exists(byte[] fileKey)
        {
            var keyStr = GetKeyString(fileKey);
            return _sessions.ContainsKey(keyStr);
        }

        /// <summary>
        /// 清空所有会话
        /// </summary>
        public void Clear()
        {
            _sessions.Clear();
        }
    }
}
