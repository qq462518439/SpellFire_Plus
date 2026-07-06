using SpellFire.MemoryRobot.MemoryMap;

namespace SpellFire.MemoryRobot.Models
{
    public sealed class MemoryRegionQueryResult
    {
        public int ProcessId { get; set; }

        public bool Ready { get; set; }

        public string Reason { get; set; }

        public string Detail { get; set; }

        public MemoryRegion Region { get; set; }
    }
}
