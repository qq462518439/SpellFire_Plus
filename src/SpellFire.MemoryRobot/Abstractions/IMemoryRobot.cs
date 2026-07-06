using System;
using SpellFire.MemoryRobot.MemoryMap;
using SpellFire.MemoryRobot.Process;

namespace SpellFire.MemoryRobot.Abstractions
{
    /// <summary>
    /// Stable low-level process memory surface for a single target process.
    /// This interface intentionally stops at memory and process primitives and does not include product logic.
    /// </summary>
    public interface IMemoryRobot : IDisposable
    {
        /// <summary>
        /// Open process session. Caller must dispose the robot to release the session lease.
        /// </summary>
        IMemorySession Session { get; }

        /// <summary>
        /// Raw memory read operations. Throws on non-Try methods when the OS read fails.
        /// </summary>
        IMemoryReader Reader { get; }

        /// <summary>
        /// Raw memory write operations. Throws on non-Try methods when the OS write fails.
        /// </summary>
        IMemoryWriter Writer { get; }

        /// <summary>
        /// Remote allocation and free primitives. Caller is responsible for freeing successful allocations.
        /// </summary>
        IRemoteAllocator Allocator { get; }

        /// <summary>
        /// Remote thread execution primitives. Caller is responsible for validating the start address and timeout.
        /// </summary>
        IRemoteThreadRunner Threads { get; }

        /// <summary>
        /// Remote library load/unload primitives built on top of remote thread execution.
        /// </summary>
        IRemoteLibraryLoader Libraries { get; }

        /// <summary>
        /// System-library lookup helpers used to resolve local module and export addresses.
        /// </summary>
        ISystemLibraryResolver SystemLibraries { get; }

        /// <summary>
        /// Fresh module snapshot provider for the target process.
        /// </summary>
        IModuleSnapshotProvider Modules { get; }

        /// <summary>
        /// Virtual memory region query service for the target process.
        /// </summary>
        MemoryRegionQueryService Regions { get; }
    }
}
