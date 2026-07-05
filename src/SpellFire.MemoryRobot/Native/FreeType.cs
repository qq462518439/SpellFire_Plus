using System;

namespace SpellFire.MemoryRobot.Native
{
    [Flags]
    public enum FreeType : uint
    {
        Decommit = 0x4000,
        Release = 0x8000
    }
}
