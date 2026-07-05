using System;
using SpellFire.MemoryRobot.Abstractions;

namespace SpellFire.MemoryRobot.Process
{
    public sealed class MemorySessionFactory : IMemorySessionFactory
    {
        private readonly MemoryRobotSessionManager sessionManager;

        public MemorySessionFactory()
            : this(new MemoryRobotSessionManager())
        {
        }

        public MemorySessionFactory(MemoryRobotSessionManager sessionManager)
        {
            this.sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        }

        public IMemoryRobot Open(int processId)
        {
            MemorySession session = sessionManager.Acquire(processId);
            return new MemoryRobotFacade(session);
        }
    }
}
