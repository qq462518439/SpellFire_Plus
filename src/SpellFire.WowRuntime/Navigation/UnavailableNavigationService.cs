namespace SpellFire.WowRuntime.Navigation
{
    public sealed class UnavailableNavigationService : INavigationService
    {
        public NavigationCapabilitySnapshot GetCapability()
        {
            return new NavigationCapabilitySnapshot(
                false,
                false,
                false,
                false,
                false,
                false,
                "Movement.StopMove/StopMoveTo remain the caller cleanup path; native CTM stop is not proven.",
                "Navigation is not implemented. Movement.Go is only a single-point CTM primitive, not a path executor.");
        }

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
