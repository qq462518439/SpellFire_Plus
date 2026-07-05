namespace SpellFire.RuntimeHost.Components
{
    public sealed class HookPayloadCleanupResult
    {
        public int RemovedProcessDirectories { get; set; }

        public int RemovedPayloadDirectories { get; set; }

        public int SkippedActiveProcessDirectories { get; set; }

        public int SkippedLockedDirectories { get; set; }

        public override string ToString()
        {
            return "RemovedProcessDirs=" + RemovedProcessDirectories +
                   " RemovedPayloadDirs=" + RemovedPayloadDirectories +
                   " SkippedActiveProcessDirs=" + SkippedActiveProcessDirectories +
                   " SkippedLockedDirs=" + SkippedLockedDirectories;
        }
    }
}
