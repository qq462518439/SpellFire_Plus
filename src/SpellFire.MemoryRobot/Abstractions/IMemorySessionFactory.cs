using System.Collections.Generic;
using SpellFire.MemoryRobot.Process;

namespace SpellFire.MemoryRobot.Abstractions
{
    public interface IMemorySessionFactory
    {
        IMemoryRobot Open(int processId);

        IReadOnlyList<MemorySessionSnapshot> GetSessions();

        bool TryGetSession(int processId, out MemorySessionSnapshot snapshot);

        bool CloseSession(int processId);

        void ReleaseAll();
    }
}
