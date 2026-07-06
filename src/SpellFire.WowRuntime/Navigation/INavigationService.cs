namespace SpellFire.WowRuntime.Navigation
{
    public interface INavigationService
    {
        PathResult FindPath(PathQuery query);

        bool TryFindZ(int mapId, float x, float y, float hintZ, out float z);
    }
}
