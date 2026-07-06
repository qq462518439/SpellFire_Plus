namespace SpellFire.WowRuntime.World
{
    public sealed class WorldPhaseSnapshot
    {
        public WorldPhaseSnapshot(WorldPhaseKind phase, bool inGame, bool loadingOrConnecting, string source, string detail)
        {
            Phase = phase;
            InGame = inGame;
            LoadingOrConnecting = loadingOrConnecting;
            Source = source ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public WorldPhaseKind Phase { get; }

        public bool InGame { get; }

        public bool LoadingOrConnecting { get; }

        public string Source { get; }

        public string Detail { get; }
    }
}
