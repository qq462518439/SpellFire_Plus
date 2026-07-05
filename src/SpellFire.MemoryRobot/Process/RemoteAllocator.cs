using System;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Diagnostics;
using SpellFire.MemoryRobot.Native;

namespace SpellFire.MemoryRobot.Process
{
    public sealed class RemoteAllocator : IRemoteAllocator
    {
        private readonly MemorySession session;

        public RemoteAllocator(MemorySession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public IntPtr Allocate(int size, AllocationType allocationType, MemoryProtection protection)
        {
            IntPtr address = Kernel32Native.VirtualAllocEx(
                session.Handle,
                IntPtr.Zero,
                new UIntPtr((uint)size),
                allocationType,
                protection);
            if (address == IntPtr.Zero)
            {
                MemoryRobotException.ThrowLast("VirtualAllocEx");
            }

            return address;
        }

        public bool Free(IntPtr address, int size = 0, FreeType freeType = FreeType.Release)
        {
            bool success = Kernel32Native.VirtualFreeEx(
                session.Handle,
                address,
                new UIntPtr((uint)Math.Max(0, size)),
                freeType);
            if (!success)
            {
                MemoryRobotException.ThrowLast("VirtualFreeEx");
            }

            return true;
        }

        public bool Protect(IntPtr address, int size, MemoryProtection protection, out MemoryProtection previousProtection)
        {
            bool success = Kernel32Native.VirtualProtectEx(
                session.Handle,
                address,
                new UIntPtr((uint)size),
                protection,
                out previousProtection);
            if (!success)
            {
                MemoryRobotException.ThrowLast("VirtualProtectEx");
            }

            return true;
        }
    }
}
