namespace SpellFire.WowRuntime.World
{
    using SpellFire.WowRuntime.Movement;

    public sealed class PlayerSnapshot
    {
        public PlayerSnapshot(int mapId, Vector3 position, MovementFlags movement)
            : this(mapId, mapId != 0, mapId, ContinentNames.GetName(mapId), position, movement)
        {
        }

        public PlayerSnapshot(int mapId, bool mapIdKnown, int continentId, string continentName, Vector3 position, MovementFlags movement)
            : this(mapId, mapIdKnown, continentId, continentName, position, movement, 0, ClickToMoveState.Unknown)
        {
        }

        public PlayerSnapshot(
            int mapId,
            bool mapIdKnown,
            int continentId,
            string continentName,
            Vector3 position,
            MovementFlags movement,
            int clickToMoveTypeRaw,
            ClickToMoveState clickToMoveState)
        {
            MapId = mapId;
            MapIdKnown = mapIdKnown;
            ContinentId = continentId;
            ContinentName = continentName ?? string.Empty;
            Position = position;
            Movement = movement;
            ClickToMoveTypeRaw = clickToMoveTypeRaw;
            ClickToMoveState = clickToMoveState;
        }

        public int MapId { get; }

        public bool MapIdKnown { get; }

        public int ContinentId { get; }

        public string ContinentName { get; }

        public Vector3 Position { get; }

        public MovementFlags Movement { get; }

        public int ClickToMoveTypeRaw { get; }

        public ClickToMoveState ClickToMoveState { get; }
    }
}
