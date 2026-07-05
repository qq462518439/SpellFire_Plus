namespace SpellFire.Runtime.Models
{
    public sealed class RuntimeEvaluationSnapshot
    {
        public int ProcessId { get; set; }

        public bool ReadyToConnect { get; set; }

        public string Decision { get; set; }

        public RuntimeMemoryProbeSnapshot Memory { get; set; }

        public RuntimeConnectionSnapshot Connection { get; set; }
    }
}
