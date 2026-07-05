using System.Collections.Generic;

namespace SpellFire.Runtime.Models
{
    public sealed class RuntimeSessionSnapshot
    {
        public int ProcessId { get; set; }

        public string HostState { get; set; }

        public IReadOnlyList<RuntimeComponentSnapshot> Components { get; set; }
    }
}
