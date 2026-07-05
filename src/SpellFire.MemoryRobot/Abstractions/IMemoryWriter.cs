using System;
using SpellFire.MemoryRobot.Writing;

namespace SpellFire.MemoryRobot.Abstractions
{
    public interface IMemoryWriter
    {
        bool WriteBytes(IntPtr address, byte[] buffer);

        MemoryWriteResult TryWriteBytes(IntPtr address, byte[] buffer);

        bool Write<T>(IntPtr address, T value) where T : struct;

        MemoryWriteResult TryWrite<T>(IntPtr address, T value) where T : struct;
    }
}
