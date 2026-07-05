using System;

namespace SpellFire.MemoryRobot.Native
{
    [Flags]
    public enum ProcessAccessFlags : uint
    {
        Terminate = 0x0001,
        CreateThread = 0x0002,
        VmOperation = 0x0008,
        VmRead = 0x0010,
        VmWrite = 0x0020,
        DupHandle = 0x0040,
        QueryInformation = 0x0400,
        Synchronize = 0x00100000,
        DefaultMemoryAccess = VmRead | VmWrite | VmOperation | QueryInformation | Synchronize
    }
}
