using System;

namespace SpellFire.MemoryRobot.Abstractions
{
    public interface ISystemLibraryResolver
    {
        IntPtr GetModuleHandle(string moduleName);

        IntPtr GetProcAddress(IntPtr moduleHandle, string procName);
    }
}
