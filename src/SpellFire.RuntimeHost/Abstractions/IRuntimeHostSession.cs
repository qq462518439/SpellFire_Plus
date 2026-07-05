using System;
using System.Collections.Generic;

namespace SpellFire.RuntimeHost.Abstractions
{
    public interface IRuntimeHostSession : IDisposable
    {
        int ProcessId { get; }

        RuntimeHostState State { get; }

        IReadOnlyList<RuntimeComponentStatus> Components { get; }

        bool IsDisposed { get; }
    }
}
