using System;
using SpellFire.MemoryRobot.MemoryMap;
using SpellFire.MemoryRobot.Process;

namespace SpellFire.MemoryRobot.Abstractions
{
    public interface IMemoryRobot : IDisposable
    {
        IMemorySession Session { get; }

        IMemoryReader Reader { get; }

        IMemoryWriter Writer { get; }

        IRemoteAllocator Allocator { get; }

        IRemoteThreadRunner Threads { get; }

        IRemoteLibraryLoader Libraries { get; }

        IModuleSnapshotProvider Modules { get; }

        MemoryRegionQueryService Regions { get; }
    }
}
