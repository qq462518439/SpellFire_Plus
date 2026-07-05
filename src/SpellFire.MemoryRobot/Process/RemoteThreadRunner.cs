using System;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Diagnostics;
using SpellFire.MemoryRobot.Native;

namespace SpellFire.MemoryRobot.Process
{
    public sealed class RemoteThreadRunner : IRemoteThreadRunner
    {
        private readonly MemorySession session;

        public RemoteThreadRunner(MemorySession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public uint Run(IntPtr startAddress, IntPtr parameterAddress, int timeoutMilliseconds = 10000)
        {
            if (startAddress == IntPtr.Zero)
            {
                throw new ArgumentException("Remote thread start address must not be zero.", nameof(startAddress));
            }

            IntPtr threadHandle = Kernel32Native.CreateRemoteThread(
                session.Handle,
                IntPtr.Zero,
                UIntPtr.Zero,
                startAddress,
                parameterAddress,
                0,
                out uint threadId);

            if (threadHandle == IntPtr.Zero)
            {
                MemoryRobotException.ThrowLast("CreateRemoteThread");
            }

            try
            {
                WaitResult waitResult = Kernel32Native.WaitForSingleObject(threadHandle, unchecked((uint)Math.Max(0, timeoutMilliseconds)));
                if (waitResult == WaitResult.Timeout)
                {
                    throw new TimeoutException("Remote thread did not finish before timeout.");
                }

                if (waitResult == WaitResult.Failed)
                {
                    MemoryRobotException.ThrowLast("WaitForSingleObject");
                }

                if (!Kernel32Native.GetExitCodeThread(threadHandle, out uint exitCode))
                {
                    MemoryRobotException.ThrowLast("GetExitCodeThread");
                }

                return exitCode;
            }
            finally
            {
                Kernel32Native.CloseHandle(threadHandle);
            }
        }
    }
}
