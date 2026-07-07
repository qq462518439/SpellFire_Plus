namespace SpellFire.WowRuntime.Navigation
{
    internal sealed class RdManagedTileProbe
    {
        public RdManagedTileProbe(bool sourcePresent, bool decodeReady, string sourceRoot, string reason)
        {
            SourcePresent = sourcePresent;
            DecodeReady = decodeReady;
            SourceRoot = sourceRoot ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public bool SourcePresent { get; }

        public bool DecodeReady { get; }

        public string SourceRoot { get; }

        public string Reason { get; }
    }
}
