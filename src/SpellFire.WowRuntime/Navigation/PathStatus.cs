namespace SpellFire.WowRuntime.Navigation
{
    public enum PathStatus
    {
        Success = 0,
        NotImplemented = 1,
        MapUnavailable = 2,
        AssetMissing = 3,
        NoPath = 4,
        TileProviderMissing = 5,
        RdManagedUnavailable = 6,
        TileLoadFailed = 7
    }
}
