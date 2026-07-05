using System.Collections.Generic;
using SpellFire.MemoryRobot.Process;

namespace SpellFire.MemoryRobot.Abstractions
{
    public interface IModuleSnapshotProvider
    {
        IReadOnlyList<ProcessModuleInfo> GetModules();
    }
}
