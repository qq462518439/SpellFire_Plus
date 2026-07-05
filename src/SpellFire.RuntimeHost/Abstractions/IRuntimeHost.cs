using System.Collections.Generic;

namespace SpellFire.RuntimeHost.Abstractions
{
    public interface IRuntimeHost
    {
        IRuntimeHostSession Attach(int processId);

        IReadOnlyList<IRuntimeHostSession> GetSessions();
    }
}
