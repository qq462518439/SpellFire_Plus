using System;

namespace SpellFire.MemoryRobot.Abstractions
{
    public interface IMemorySession : IDisposable
    {
        int ProcessId { get; }

        IntPtr Handle { get; }

        bool IsOpen { get; }
    }
}
