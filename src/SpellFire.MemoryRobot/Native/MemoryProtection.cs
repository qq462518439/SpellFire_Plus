using System;

namespace SpellFire.MemoryRobot.Native
{
    [Flags]
    public enum MemoryProtection : uint
    {
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40
    }
}
