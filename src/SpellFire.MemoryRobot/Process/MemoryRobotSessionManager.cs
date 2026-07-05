using System;
using System.Collections.Generic;

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
