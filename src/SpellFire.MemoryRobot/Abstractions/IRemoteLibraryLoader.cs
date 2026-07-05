namespace SpellFire.MemoryRobot.Abstractions
{
    public interface IRemoteLibraryLoader
    {
        int LoadLibrary(string libraryPath, int timeoutMilliseconds = 10000);
    }
}
