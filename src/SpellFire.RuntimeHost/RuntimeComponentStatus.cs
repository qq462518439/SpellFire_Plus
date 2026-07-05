namespace SpellFire.RuntimeHost
{
    public sealed class RuntimeComponentStatus
    {
        public string Name { get; set; }

        public bool Ready { get; set; }

        public string Reason { get; set; }

        public string Detail { get; set; }
    }
}
