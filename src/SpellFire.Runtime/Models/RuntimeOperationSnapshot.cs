using System.Collections.Generic;

namespace SpellFire.Runtime.Models
{
    public sealed class RuntimeOperationSnapshot
    {
        public int ProcessId { get; set; }

        public string Operation { get; set; }

        public bool Ready { get; set; }

        public string Reason { get; set; }

        public string Detail { get; set; }

        public string HostState { get; set; }

        public IReadOnlyList<RuntimeComponentSnapshot> Components { get; set; }
    }
}
