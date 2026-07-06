using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Navigation
{
    public sealed class PathQuery
    {
        public PathQuery(int mapId, Vector3 from, Vector3 to)
        {
            MapId = mapId;
            From = from;
            To = to;
        }

        public int MapId { get; }

        public Vector3 From { get; }

        public Vector3 To { get; }
    }
}
