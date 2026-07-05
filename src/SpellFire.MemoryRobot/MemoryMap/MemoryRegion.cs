using System;
using SpellFire.MemoryRobot.Native;

namespace SpellFire.MemoryRobot.MemoryMap
{
    public sealed class MemoryRegion
    {
        public IntPtr BaseAddress { get; set; }

        public IntPtr AllocationBase { get; set; }

        public IntPtr RegionSize { get; set; }

        public MemoryProtection Protect { get; set; }

        public MemoryState State { get; set; }
    }
}
