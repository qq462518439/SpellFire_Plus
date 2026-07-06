using System.Collections.Generic;

namespace SpellFire.RuntimeHost
{
    public sealed class RuntimeHostOperationResult
    {
        public int ProcessId { get; set; }

        public string Operation { get; set; }

        public bool Ready { get; set; }

        public string Reason { get; set; }

        public string Detail { get; set; }

        public RuntimeHostState HostState { get; set; }

        public IReadOnlyList<RuntimeComponentStatus> Components { get; set; }
    }
}
