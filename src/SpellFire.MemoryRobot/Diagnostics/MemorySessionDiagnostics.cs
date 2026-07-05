using System;
using System.Runtime.InteropServices;
using SpellFire.MemoryRobot.Native;

namespace SpellFire.MemoryRobot.Diagnostics
{
    public sealed class MemorySessionDiagnostics
    {
        public MemorySessionProbeResult Probe(
            int processId,
            ProcessAccessFlags access = ProcessAccessFlags.DefaultMemoryAccess)
        {
            MemorySessionProbeResult result = new MemorySessionProbeResult
            {
                ProcessId = processId,
                RequestedAccess = access,
                HostIs64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                HostIs64BitProcess = Environment.Is64BitProcess
            };

            System.Diagnostics.Process process;
            try
            {
                process = System.Diagnostics.Process.GetProcessById(processId);
                result.ProcessFound = true;
                result.ProcessName = process.ProcessName;
                result.Responding = SafeResponding(process);
            }
            catch
            {
                result.ProcessFound = false;
                return result;
            }

            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = Kernel32Native.OpenProcess(access, false, processId);
                result.OpenSucceeded = handle != IntPtr.Zero;
                result.Handle = handle;
                if (handle == IntPtr.Zero)
                {
                    result.Win32Error = Marshal.GetLastWin32Error();
                    return result;
                }

                if (!Environment.Is64BitOperatingSystem)
                {
                    result.TargetWow64Known = true;
                    result.TargetWow64 = false;
                    return result;
                }

                bool wow64;
                if (Kernel32Native.IsWow64Process(handle, out wow64))
                {
                    result.TargetWow64Known = true;
                    result.TargetWow64 = wow64;
                }
                else
                {
                    result.TargetWow64Known = false;
                    result.Win32Error = Marshal.GetLastWin32Error();
                }

                return result;
            }
            finally
            {
                if (handle != IntPtr.Zero)
                {
                    Kernel32Native.CloseHandle(handle);
                    result.Handle = IntPtr.Zero;
                }
            }
        }

        private static bool SafeResponding(System.Diagnostics.Process process)
        {
            try
            {
                return process.Responding;
            }
            catch
            {
                return false;
            }
        }
    }
}
