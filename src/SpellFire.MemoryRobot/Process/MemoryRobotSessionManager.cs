using System;
using System.Collections.Generic;
using System.Linq;

namespace SpellFire.MemoryRobot.Process
{
    public sealed class MemoryRobotSessionManager
    {
        private readonly object syncRoot = new object();
        private readonly Dictionary<int, SessionEntry> sessions = new Dictionary<int, SessionEntry>();

        public MemorySessionLease Acquire(int processId)
        {
            lock (syncRoot)
            {
                SessionEntry entry;
                if (sessions.TryGetValue(processId, out entry))
                {
                    if (!entry.Session.IsOpen || HasExited(entry.Session.Process))
                    {
                        entry.Session.Dispose();
                        sessions.Remove(processId);
                    }
                    else
                    {
                        entry.RefCount++;
                        return new MemorySessionLease(this, entry.Session);
                    }
                }

                var process = System.Diagnostics.Process.GetProcessById(processId);
                var session = new MemorySession(process);
                sessions[processId] = new SessionEntry(session);
                return new MemorySessionLease(this, session);
            }
        }

        public void Release(int processId)
        {
            lock (syncRoot)
            {
                SessionEntry entry;
                if (!sessions.TryGetValue(processId, out entry))
                {
                    return;
                }

                entry.RefCount--;
                if (entry.RefCount > 0)
                {
                    return;
                }

                entry.Session.Dispose();
                sessions.Remove(processId);
            }
        }

        public bool CloseSession(int processId)
        {
            lock (syncRoot)
            {
                SessionEntry entry;
                if (!sessions.TryGetValue(processId, out entry))
                {
                    return false;
                }

                entry.Session.Dispose();
                sessions.Remove(processId);
                return true;
            }
        }

        public IReadOnlyList<MemorySessionSnapshot> GetSessions()
        {
            lock (syncRoot)
            {
                return sessions
                    .OrderBy(pair => pair.Key)
                    .Select(pair => CreateSnapshot(pair.Value))
                    .ToArray();
            }
        }

        public bool TryGetSession(int processId, out MemorySessionSnapshot snapshot)
        {
            lock (syncRoot)
            {
                SessionEntry entry;
                if (!sessions.TryGetValue(processId, out entry))
                {
                    snapshot = null;
                    return false;
                }

                snapshot = CreateSnapshot(entry);
                return true;
            }
        }

        public void ReleaseAll()
        {
            lock (syncRoot)
            {
                foreach (SessionEntry entry in sessions.Values)
                {
                    entry.Session.Dispose();
                }

                sessions.Clear();
            }
        }

        private static bool HasExited(System.Diagnostics.Process process)
        {
            try
            {
                return process.HasExited;
            }
            catch
            {
                return true;
            }
        }

        private static MemorySessionSnapshot CreateSnapshot(SessionEntry entry)
        {
            MemorySession session = entry.Session;
            return new MemorySessionSnapshot
            {
                ProcessId = session.ProcessId,
                ProcessName = SafeProcessName(session.Process),
                Handle = session.Handle,
                IsOpen = session.IsOpen,
                HasExited = HasExited(session.Process),
                ReferenceCount = entry.RefCount
            };
        }

        private static string SafeProcessName(System.Diagnostics.Process process)
        {
            try
            {
                return process.ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }

        private sealed class SessionEntry
        {
            public SessionEntry(MemorySession session)
            {
                Session = session;
                RefCount = 1;
            }

            public MemorySession Session { get; }

            public int RefCount { get; set; }
        }
    }
}
