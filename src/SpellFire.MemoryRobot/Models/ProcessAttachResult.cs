using SpellFire.MemoryRobot.Diagnostics;
using SpellFire.MemoryRobot.Process;

namespace SpellFire.MemoryRobot.Models
{
    public sealed class ProcessAttachResult
    {
        public int ProcessId { get; set; }

        public bool Ready { get; set; }

        public string Reason { get; set; }

        public string Detail { get; set; }

        public MemorySessionProbeResult Probe { get; set; }

        public MemorySessionSnapshot SessionSnapshot { get; set; }
    }
}
