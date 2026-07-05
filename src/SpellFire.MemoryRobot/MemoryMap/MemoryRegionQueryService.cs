using System;
using SpellFire.MemoryRobot.Diagnostics;
using SpellFire.MemoryRobot.Process;
using SpellFire.MemoryRobot.Native;

namespace SpellFire.MemoryRobot.MemoryMap
{
    public sealed class MemoryRegionQueryService
    {
        private readonly MemorySession session;

        public MemoryRegionQueryService(MemorySession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public bool TryQuery(IntPtr address, out MemoryRegion region)
        {
            region = null;
            int result = Kernel32Native.VirtualQueryEx(
                session.Handle,
                address,
                out MemoryBasicInformation info,
                System.Runtime.InteropServices.Marshal.SizeOf(typeof(MemoryBasicInformation)));
            if (result <= 0)
            {
                int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                if (error != 0)
                {
                    throw new MemoryRobotException("VirtualQueryEx", error);
                }

                return false;
            }

            region = new MemoryRegion
            {
                BaseAddress = info.BaseAddress,
                AllocationBase = info.AllocationBase,
                RegionSize = info.RegionSize,
                Protect = info.Protect,
                State = info.State
            };
            return true;
        }
    }
}
