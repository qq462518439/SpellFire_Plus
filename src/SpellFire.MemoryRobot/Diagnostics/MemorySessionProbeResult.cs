using System;
using System.ComponentModel;
using SpellFire.MemoryRobot.Native;

namespace SpellFire.MemoryRobot.Diagnostics
{
    public sealed class MemorySessionProbeResult
    {
        public int ProcessId { get; set; }

        public string ProcessName { get; set; }

        public bool ProcessFound { get; set; }

        public bool Responding { get; set; }

        public bool HostIs64BitOperatingSystem { get; set; }

        public bool HostIs64BitProcess { get; set; }

        public bool TargetWow64Known { get; set; }

        public bool TargetWow64 { get; set; }

        public ProcessAccessFlags RequestedAccess { get; set; }

        public bool OpenSucceeded { get; set; }

        public IntPtr Handle { get; set; }

        public int Win32Error { get; set; }

        public string Win32Message
        {
            get
            {
                return Win32Error == 0 ? string.Empty : new Win32Exception(Win32Error).Message;
            }
        }

        public string Reason
        {
            get
            {
                if (!ProcessFound)
                {
                    return "ProcessUnavailable";
                }

                if (OpenSucceeded)
                {
                    if (!TargetWow64Known)
                    {
                        return "TargetBitnessUnknown";
                    }

                    return TargetWow64 ? "SessionOpened" : "TargetNot32Bit";
                }

                if (Win32Error == 5)
                {
                    return "AccessDenied";
                }

                if (Win32Error == 87)
                {
                    return "InvalidParameter";
                }

                return "OpenProcessFailed";
            }
        }
    }
}
