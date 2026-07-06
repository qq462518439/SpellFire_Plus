using SpellFire.MemoryRobot.Process;
using SpellFire.MemoryRobot.Services;
using SpellFire.RuntimeHost.Abstractions;
using SpellFire.RuntimeHost.Components;

namespace SpellFire.RuntimeHost
{
    public sealed class RuntimeHostFactory : IRuntimeHostFactory
    {
        public IRuntimeHost CreateHost()
        {
            var sessionFactory = new MemorySessionFactory(new MemoryRobotSessionManager());
            var attachService = new ProcessAttachService(sessionFactory);
            var snapshotService = new ProcessSnapshotService(sessionFactory);
            var remoteExecutionService = new RemoteExecutionService(sessionFactory);
            return new RuntimeHost(new IRuntimeComponent[]
            {
                new MemoryRobotRuntimeComponent(sessionFactory, attachService),
                new SpellFireHookRuntimeComponent(attachService, snapshotService, remoteExecutionService)
            });
        }
    }
}
