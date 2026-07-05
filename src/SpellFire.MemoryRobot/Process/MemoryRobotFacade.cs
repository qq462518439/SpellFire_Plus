using System;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.MemoryMap;
using SpellFire.MemoryRobot.Reading;
using SpellFire.MemoryRobot.Writing;

namespace SpellFire.MemoryRobot.Process
{
    public sealed class MemoryRobotFacade : IMemoryRobot, IDisposable
    {
        private readonly MemorySession concreteSession;

        public MemoryRobotFacade(MemorySession session)
        {
            concreteSession = session ?? throw new ArgumentNullException(nameof(session));
            Session = concreteSession;
            Reader = new MemoryReader(concreteSession);
            Writer = new MemoryWriter(concreteSession);
            Allocator = new RemoteAllocator(concreteSession);
            Threads = new RemoteThreadRunner(concreteSession);
            Libraries = new RemoteLibraryLoader(session, Writer, Allocator, Threads);
            Modules = new ProcessModuleSnapshotProvider(concreteSession.Process);
            Regions = new MemoryRegionQueryService(concreteSession);
        }

        public IMemorySession Session { get; }

        public IMemoryReader Reader { get; }

        public IMemoryWriter Writer { get; }

        public IRemoteAllocator Allocator { get; }

        public IRemoteThreadRunner Threads { get; }

        public IRemoteLibraryLoader Libraries { get; }

        public IModuleSnapshotProvider Modules { get; }

        public MemoryRegionQueryService Regions { get; }

        public void Dispose()
        {
            Session.Dispose();
        }
    }
}
