using System;

namespace SpellFire.MemoryRobot.Models
{
    public sealed class RemoteExecutionResult
    {
        public int ProcessId { get; set; }

        public bool Ready { get; set; }

        public string Reason { get; set; }

        public string Detail { get; set; }

        public IntPtr Address { get; set; }

        public uint ExitCode { get; set; }

        public IntPtr ModuleHandle { get; set; }
    }
}
