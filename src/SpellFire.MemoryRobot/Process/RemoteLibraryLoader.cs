using System;
using System.Text;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Diagnostics;
using SpellFire.MemoryRobot.Native;

namespace SpellFire.MemoryRobot.Process
{
    public sealed class RemoteLibraryLoader : IRemoteLibraryLoader
    {
        private readonly MemorySession session;
        private readonly IMemoryWriter writer;
        private readonly IRemoteAllocator allocator;
        private readonly IRemoteThreadRunner threadRunner;

        public RemoteLibraryLoader(
            MemorySession session,
            IMemoryWriter writer,
            IRemoteAllocator allocator,
            IRemoteThreadRunner threadRunner)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
            this.allocator = allocator ?? throw new ArgumentNullException(nameof(allocator));
            this.threadRunner = threadRunner ?? throw new ArgumentNullException(nameof(threadRunner));
        }

        public int LoadLibrary(string libraryPath, int timeoutMilliseconds = 10000)
        {
            if (string.IsNullOrWhiteSpace(libraryPath))
            {
                throw new ArgumentException("Library path must not be empty.", nameof(libraryPath));
            }

            string normalizedPath = System.IO.Path.GetFullPath(libraryPath);
            if (!System.IO.File.Exists(normalizedPath))
            {
                throw new System.IO.FileNotFoundException("Library to inject was not found.", normalizedPath);
            }

            IntPtr kernel32Handle = Kernel32Native.GetModuleHandle("kernel32.dll");
            if (kernel32Handle == IntPtr.Zero)
            {
                MemoryRobotException.ThrowLast("GetModuleHandle(kernel32.dll)");
            }

            IntPtr loadLibraryAddress = Kernel32Native.GetProcAddress(kernel32Handle, "LoadLibraryW");
            if (loadLibraryAddress == IntPtr.Zero)
            {
                MemoryRobotException.ThrowLast("GetProcAddress(LoadLibraryW)");
            }

            byte[] pathBytes = Encoding.Unicode.GetBytes(normalizedPath + "\0");
            IntPtr remotePath = IntPtr.Zero;
            try
            {
                remotePath = allocator.Allocate(pathBytes.Length, AllocationType.Commit | AllocationType.Reserve, MemoryProtection.ReadWrite);
                writer.WriteBytes(remotePath, pathBytes);
                uint exitCode = threadRunner.Run(loadLibraryAddress, remotePath, timeoutMilliseconds);
                return unchecked((int)exitCode);
            }
            finally
            {
                if (remotePath != IntPtr.Zero)
                {
                    try
                    {
                        allocator.Free(remotePath);
                    }
                    catch
                    {
                    }
                }
            }
        }

        public bool FreeLibrary(IntPtr remoteModuleHandle, int timeoutMilliseconds = 10000)
        {
            if (remoteModuleHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Remote module handle must not be zero.", nameof(remoteModuleHandle));
            }

            IntPtr kernel32Handle = Kernel32Native.GetModuleHandle("kernel32.dll");
            if (kernel32Handle == IntPtr.Zero)
            {
                MemoryRobotException.ThrowLast("GetModuleHandle(kernel32.dll)");
            }

            IntPtr freeLibraryAddress = Kernel32Native.GetProcAddress(kernel32Handle, "FreeLibrary");
            if (freeLibraryAddress == IntPtr.Zero)
            {
                MemoryRobotException.ThrowLast("GetProcAddress(FreeLibrary)");
            }

            uint exitCode = threadRunner.Run(freeLibraryAddress, remoteModuleHandle, timeoutMilliseconds);
            return exitCode != 0;
        }
    }
}
