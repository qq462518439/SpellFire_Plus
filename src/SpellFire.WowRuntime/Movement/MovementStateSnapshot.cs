using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public sealed class MovementStateSnapshot
    {
        public MovementStateSnapshot(bool inMovement, MovementFlags flags, string source, string detail)
            : this(inMovement, flags, 0, ClickToMoveState.Unknown, 0, false, source, detail)
        {
        }

        public MovementStateSnapshot(
            bool inMovement,
            MovementFlags flags,
            int clickToMoveTypeRaw,
            ClickToMoveState clickToMoveState,
            float speed,
            bool speedKnown,
            string source,
            string detail)
        {
            InMovement = inMovement;
            Flags = flags;
            ClickToMoveTypeRaw = clickToMoveTypeRaw;
            ClickToMoveState = clickToMoveState;
            Speed = speed;
            SpeedKnown = speedKnown;
            Source = source ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public bool InMovement { get; }

        public MovementFlags Flags { get; }

        public int ClickToMoveTypeRaw { get; }

        public ClickToMoveState ClickToMoveState { get; }

        public float Speed { get; }

        public bool SpeedKnown { get; }

        public string Source { get; }

        public string Detail { get; }
    }
}
