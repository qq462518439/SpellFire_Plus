using System;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Diagnostics;
using SpellFire.MemoryRobot.Native;

namespace SpellFire.MemoryRobot.Process
{
    public class MemorySession : IMemorySession
    {
        private readonly System.Diagnostics.Process process;
        protected IntPtr handle;

        public MemorySession(System.Diagnostics.Process process, ProcessAccessFlags access = ProcessAccessFlags.DefaultMemoryAccess)
        {
            this.process = process ?? throw new ArgumentNullException(nameof(process));
            handle = Kernel32Native.OpenProcess(access, false, process.Id);
            if (handle == IntPtr.Zero)
            {
                MemoryRobotException.ThrowLast("OpenProcess");
            }
        }

        protected MemorySession(System.Diagnostics.Process process, IntPtr existingHandle)
        {
            this.process = process ?? throw new ArgumentNullException(nameof(process));
            if (existingHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Existing handle must not be zero.", nameof(existingHandle));
            }

            handle = existingHandle;
        }

        public int ProcessId => process.Id;

        public System.Diagnostics.Process Process => process;

        public IntPtr Handle => handle;

        public bool IsOpen => handle != IntPtr.Zero;

        public virtual void Dispose()
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            Kernel32Native.CloseHandle(handle);
            handle = IntPtr.Zero;
        }
    }
}
