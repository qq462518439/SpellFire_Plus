namespace SpellFire.MemoryRobot.Abstractions
{
    public interface IRemoteLibraryLoader
    {
        int LoadLibrary(string libraryPath, int timeoutMilliseconds = 10000);

        bool FreeLibrary(System.IntPtr remoteModuleHandle, int timeoutMilliseconds = 10000);
    }
}
