namespace SpellFire.WowRuntime.Navigation
{
    public interface INavigationService
    {
        NavigationCapabilitySnapshot GetCapability();

        PathResult FindPath(PathQuery query);

        bool TryFindZ(int mapId, float x, float y, float hintZ, out float z);
    }
}
