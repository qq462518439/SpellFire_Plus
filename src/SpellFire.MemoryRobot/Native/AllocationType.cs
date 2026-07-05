using System;

namespace SpellFire.MemoryRobot.Native
{
    [Flags]
    public enum AllocationType : uint
    {
        Commit = 0x1000,
        Reserve = 0x2000
    }
}
