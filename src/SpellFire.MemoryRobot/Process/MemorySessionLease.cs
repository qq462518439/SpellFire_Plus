using System;

namespace SpellFire.MemoryRobot.Process
{
    public sealed class MemorySessionLease : MemorySession
    {
        private readonly MemoryRobotSessionManager owner;
        private readonly MemorySession innerSession;
        private bool disposed;

        internal MemorySessionLease(MemoryRobotSessionManager owner, MemorySession innerSession)
            : base(innerSession.Process, innerSession.Handle)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.innerSession = innerSession ?? throw new ArgumentNullException(nameof(innerSession));
        }

        public override void Dispose()
        {
            if (disposed)
            {
                return;
            }

            owner.Release(innerSession.ProcessId);
            disposed = true;
        }
    }
}
