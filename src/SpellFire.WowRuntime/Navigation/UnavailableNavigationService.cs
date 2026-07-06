namespace SpellFire.WowRuntime.Navigation
{
    public sealed class UnavailableNavigationService : INavigationService
    {
        public PathResult FindPath(PathQuery query)
        {
            return new PathResult(PathStatus.NotImplemented, null, "Navigation is not implemented yet.");
        }

        public bool TryFindZ(int mapId, float x, float y, float hintZ, out float z)
        {
            z = 0;
            return false;
        }
    }
}
