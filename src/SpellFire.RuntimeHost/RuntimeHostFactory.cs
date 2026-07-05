using SpellFire.MemoryRobot.Process;
using SpellFire.RuntimeHost.Abstractions;
using SpellFire.RuntimeHost.Components;

namespace SpellFire.RuntimeHost
{
    public sealed class RuntimeHostFactory : IRuntimeHostFactory
    {
        public IRuntimeHost CreateHost()
        {
            var sessionFactory = new MemorySessionFactory(new MemoryRobotSessionManager());
            return new RuntimeHost(new IRuntimeComponent[]
            {
                new MemoryRobotRuntimeComponent(sessionFactory),
                new SpellFireHookRuntimeComponent(sessionFactory)
            });
        }
    }
}
