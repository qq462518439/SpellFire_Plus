using System;
using SpellFire.MemoryRobot.Reading;

namespace SpellFire.MemoryRobot.Abstractions
{
    public interface IMemoryReader
    {
        byte[] ReadBytes(IntPtr address, int size);

        MemoryReadResult TryReadBytes(IntPtr address, int size);

        T Read<T>(IntPtr address) where T : struct;

        MemoryReadResult<T> TryRead<T>(IntPtr address) where T : struct;

        string ReadString(IntPtr address, int maxBytes = 256);
    }
}
