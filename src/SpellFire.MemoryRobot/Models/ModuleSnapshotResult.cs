using System.Collections.Generic;
using SpellFire.MemoryRobot.Process;

namespace SpellFire.MemoryRobot.Models
{
    public sealed class ModuleSnapshotResult
    {
        public int ProcessId { get; set; }

        public bool Ready { get; set; }

        public string Reason { get; set; }

        public string Detail { get; set; }

        public IReadOnlyList<ProcessModuleInfo> Modules { get; set; }

        public ProcessModuleInfo MatchedModule { get; set; }
    }
}
