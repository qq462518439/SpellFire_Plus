namespace SpellFire.WowRuntime.Navigation
{
    public sealed class NavigationCapabilitySnapshot
    {
        public NavigationCapabilitySnapshot(
            bool canFindPath,
            bool canExecutePath,
            bool canFindZ,
            bool supportsPathQueue,
            bool supportsArrivalCheck,
            bool supportsStuckDetection,
            bool rdManagedAssemblyPresent,
            bool rdManagedSessionReady,
            bool tileProviderReady,
            string stopResponsibility,
            string detail)
        {
            CanFindPath = canFindPath;
            CanExecutePath = canExecutePath;
            CanFindZ = canFindZ;
            SupportsPathQueue = supportsPathQueue;
            SupportsArrivalCheck = supportsArrivalCheck;
            SupportsStuckDetection = supportsStuckDetection;
            RdManagedAssemblyPresent = rdManagedAssemblyPresent;
            RdManagedSessionReady = rdManagedSessionReady;
            TileProviderReady = tileProviderReady;
            StopResponsibility = stopResponsibility ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public bool CanFindPath { get; }

        public bool CanExecutePath { get; }

        public bool CanFindZ { get; }

        public bool SupportsPathQueue { get; }

        public bool SupportsArrivalCheck { get; }

        public bool SupportsStuckDetection { get; }

        public bool RdManagedAssemblyPresent { get; }

        public bool RdManagedSessionReady { get; }

        public bool TileProviderReady { get; }

        public string StopResponsibility { get; }

        public string Detail { get; }
    }
}
