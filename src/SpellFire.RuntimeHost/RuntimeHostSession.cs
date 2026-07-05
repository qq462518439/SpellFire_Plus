using System;
using System.Collections.Generic;
using System.Linq;
using SpellFire.RuntimeHost.Abstractions;

namespace SpellFire.RuntimeHost
{
    public sealed class RuntimeHostSession : IRuntimeHostSession
    {
        private readonly IReadOnlyList<IRuntimeComponent> components;
        private bool disposed;

        public RuntimeHostSession(int processId, IReadOnlyList<IRuntimeComponent> components)
        {
            ProcessId = processId;
            this.components = components ?? Array.Empty<IRuntimeComponent>();
            Components = this.components.Select(component => component.Probe(processId)).ToArray();
            State = ComputeState(Components);
        }

        public int ProcessId { get; }

        public RuntimeHostState State { get; private set; }

        public IReadOnlyList<RuntimeComponentStatus> Components { get; }

        public bool IsDisposed => disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            foreach (IRuntimeComponent component in components)
            {
                component.Cleanup(ProcessId);
            }

            State = RuntimeHostState.Detached;
            disposed = true;
        }

        private static RuntimeHostState ComputeState(IReadOnlyList<RuntimeComponentStatus> statuses)
        {
            if (statuses == null || statuses.Count == 0)
            {
                return RuntimeHostState.Unknown;
            }

            int readyCount = statuses.Count(status => status != null && status.Ready);
            if (readyCount == 0)
            {
                return RuntimeHostState.Failed;
            }

            if (readyCount == statuses.Count)
            {
                return RuntimeHostState.Ready;
            }

            return RuntimeHostState.PartiallyReady;
        }
    }
}
