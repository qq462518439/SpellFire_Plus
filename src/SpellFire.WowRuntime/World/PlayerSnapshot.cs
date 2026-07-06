namespace SpellFire.WowRuntime.World
{
    public sealed class PlayerSnapshot
    {
        public PlayerSnapshot(int mapId, Vector3 position, MovementFlags movement)
        {
            MapId = mapId;
            Position = position;
            Movement = movement;
        }

        public int MapId { get; }

        public Vector3 Position { get; }

        public MovementFlags Movement { get; }
    }
}
