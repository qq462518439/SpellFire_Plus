using System;

namespace SpellFire.MemoryRobot.Abstractions
{
    public interface IRemoteThreadRunner
    {
        uint Run(IntPtr startAddress, IntPtr parameterAddress, int timeoutMilliseconds = 10000);
    }
}
