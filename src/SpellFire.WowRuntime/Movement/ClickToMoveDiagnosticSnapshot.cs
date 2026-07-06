using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public sealed class ClickToMoveDiagnosticSnapshot
    {
        public ClickToMoveDiagnosticSnapshot(
            WorldPhaseSnapshot phase,
            PlayerSnapshot player,
            MovementStateSnapshot movement,
            string detail)
        {
            Phase = phase;
            Player = player;
            Movement = movement;
            Detail = detail ?? string.Empty;
        }

        public WorldPhaseSnapshot Phase { get; }

        public PlayerSnapshot Player { get; }

        public MovementStateSnapshot Movement { get; }

        public string Detail { get; }
    }
}
