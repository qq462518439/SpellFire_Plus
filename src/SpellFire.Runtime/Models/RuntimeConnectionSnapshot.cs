using System.Collections.Generic;

namespace SpellFire.Runtime.Models
{
    public sealed class RuntimeConnectionSnapshot
    {
        public int ProcessId { get; set; }

        public bool Connected { get; set; }

        public bool Disconnected { get; set; }

        public string HostState { get; set; }

        public bool SessionExists { get; set; }

        public bool SessionDisposed { get; set; }

        public string Reason { get; set; }

        public IReadOnlyList<RuntimeComponentSnapshot> Components { get; set; }
    }
}
