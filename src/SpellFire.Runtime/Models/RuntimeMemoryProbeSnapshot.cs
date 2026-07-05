namespace SpellFire.Runtime.Models
{
    public sealed class RuntimeMemoryProbeSnapshot
    {
        public int ProcessId { get; set; }

        public bool Ready { get; set; }

        public string Reason { get; set; }

        public string ProcessName { get; set; }

        public bool ProcessFound { get; set; }

        public bool Responding { get; set; }

        public bool HostIs64BitOperatingSystem { get; set; }

        public bool HostIs64BitProcess { get; set; }

        public bool TargetWow64Known { get; set; }

        public bool TargetWow64 { get; set; }

        public string RequestedAccess { get; set; }

        public int Win32Error { get; set; }

        public string Win32Message { get; set; }
    }
}
