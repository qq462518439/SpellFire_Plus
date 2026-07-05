using System;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Diagnostics;
using SpellFire.MemoryRobot.Native;

namespace SpellFire.MemoryRobot.Process
{
    public sealed class SystemLibraryResolver : ISystemLibraryResolver
    {
        public IntPtr GetModuleHandle(string moduleName)
        {
            IntPtr handle = Kernel32Native.GetModuleHandle(moduleName);
            if (handle == IntPtr.Zero)
            {
                MemoryRobotException.ThrowLast("GetModuleHandle(" + moduleName + ")");
            }

            return handle;
        }

        public IntPtr GetProcAddress(IntPtr moduleHandle, string procName)
        {
            if (moduleHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Module handle must not be zero.", nameof(moduleHandle));
            }

            IntPtr address = Kernel32Native.GetProcAddress(moduleHandle, procName);
            if (address == IntPtr.Zero)
            {
                MemoryRobotException.ThrowLast("GetProcAddress(" + procName + ")");
            }

            return address;
        }
    }
}
