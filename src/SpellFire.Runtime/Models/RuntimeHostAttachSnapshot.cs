namespace SpellFire.Runtime.Models
{
    public sealed class RuntimeHostAttachSnapshot
    {
        public int ProcessId { get; set; }

        public bool Accepted { get; set; }

        public string Decision { get; set; }

        public RuntimeEvaluationSnapshot Evaluation { get; set; }

        public RuntimeConnectionSnapshot Connection { get; set; }
    }
}
