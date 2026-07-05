namespace SpellFire.Runtime.Models
{
    public sealed class RuntimeComponentSnapshot
    {
        public string Name { get; set; }

        public bool Ready { get; set; }

        public string Reason { get; set; }

        public string Detail { get; set; }
    }
}
