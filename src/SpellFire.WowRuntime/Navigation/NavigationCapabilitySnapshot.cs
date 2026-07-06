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
            string stopResponsibility,
            string detail)
        {
            CanFindPath = canFindPath;
            CanExecutePath = canExecutePath;
            CanFindZ = canFindZ;
            SupportsPathQueue = supportsPathQueue;
            SupportsArrivalCheck = supportsArrivalCheck;
            SupportsStuckDetection = supportsStuckDetection;
            StopResponsibility = stopResponsibility ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public bool CanFindPath { get; }

        public bool CanExecutePath { get; }

        public bool CanFindZ { get; }

        public bool SupportsPathQueue { get; }

        public bool SupportsArrivalCheck { get; }

        public bool SupportsStuckDetection { get; }

        public string StopResponsibility { get; }

        public string Detail { get; }
    }
}
