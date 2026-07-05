using System;
using SpellFire.MemoryRobot.Native;

namespace SpellFire.MemoryRobot.Abstractions
{
    public interface IRemoteAllocator
    {
        IntPtr Allocate(int size, AllocationType allocationType, MemoryProtection protection);

        bool Free(IntPtr address, int size = 0, FreeType freeType = FreeType.Release);

        bool Protect(IntPtr address, int size, MemoryProtection protection, out MemoryProtection previousProtection);
    }
}
