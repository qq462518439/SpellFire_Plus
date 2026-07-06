using System;

namespace SpellFire.WowRuntime.World
{
    public sealed class StaticWorldAddressProvider : IWorldAddressProvider
    {
        private static readonly WorldAddressTable Wrath335a = new WorldAddressTable(
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            new IntPtr(0x00C79CE0),
            new IntPtr(0xAC),
            new IntPtr(0x3C),
            new IntPtr(0x00CA1238),
            new IntPtr(0x00BD07B0),
            new IntPtr(0xCC),
            new IntPtr(0x30),
            new IntPtr(0x14),
            new IntPtr(0x8),
            new IntPtr(0x798),
            new IntPtr(0xE8),
            new IntPtr(0x007D0792),
            new IntPtr(0x0076AA38),
            new IntPtr(0x008A11F4),
            512);

        public WorldAddressTable GetAddressTable(int processId)
        {
            return Wrath335a;
        }
    }
}
