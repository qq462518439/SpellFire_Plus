using System.Collections.Generic;

namespace SpellFire.RuntimeHost.Abstractions
{
    public interface IRuntimeHost
    {
        IRuntimeHostSession Attach(int processId);

        bool Detach(int processId, out IRuntimeHostSession detachedSession);

        IReadOnlyList<IRuntimeHostSession> GetSessions();
    }
}
