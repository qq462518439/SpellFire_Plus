using System;

namespace SpellFire.MemoryRobot.Abstractions
{
    public interface IMemoryWriter
    {
        bool WriteBytes(IntPtr address, byte[] buffer);

        bool Write<T>(IntPtr address, T value) where T : struct;
    }
}
