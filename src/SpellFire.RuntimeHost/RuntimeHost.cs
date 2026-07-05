using System;
using System.Collections.Generic;
using System.Linq;
using SpellFire.RuntimeHost.Abstractions;

namespace SpellFire.RuntimeHost
{
    public sealed class RuntimeHost : IRuntimeHost
    {
        private readonly IReadOnlyList<IRuntimeComponent> components;
        private readonly Dictionary<int, IRuntimeHostSession> sessions = new Dictionary<int, IRuntimeHostSession>();
        private readonly object syncRoot = new object();

        public RuntimeHost(IReadOnlyList<IRuntimeComponent> components)
        {
            this.components = components ?? Array.Empty<IRuntimeComponent>();
        }

        public IReadOnlyList<IRuntimeComponent> Components => components;

        public IRuntimeHostSession Attach(int processId)
        {
            lock (syncRoot)
            {
                if (sessions.TryGetValue(processId, out IRuntimeHostSession existing) && existing != null && !existing.IsDisposed)
                {
                    return existing;
                }

                IRuntimeHostSession session = new RuntimeHostSession(processId, components);
                sessions[processId] = session;
                return session;
            }
        }

        public bool Detach(int processId, out IRuntimeHostSession detachedSession)
        {
            lock (syncRoot)
            {
                if (!sessions.TryGetValue(processId, out detachedSession) || detachedSession == null)
                {
                    detachedSession = null;
                    return false;
                }

                detachedSession.Dispose();
                sessions.Remove(processId);
                return true;
            }
        }

        public IReadOnlyList<IRuntimeHostSession> GetSessions()
        {
            lock (syncRoot)
            {
                return sessions.Values.OrderBy(session => session.ProcessId).ToArray();
            }
        }
    }
}
