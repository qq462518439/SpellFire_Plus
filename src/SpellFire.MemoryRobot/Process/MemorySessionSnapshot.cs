using System;

namespace SpellFire.MemoryRobot.Process
{
    public sealed class MemorySessionSnapshot
    {
        public int ProcessId { get; set; }

        public string ProcessName { get; set; }

        public IntPtr Handle { get; set; }

        public bool IsOpen { get; set; }

        public bool HasExited { get; set; }

        public int ReferenceCount { get; set; }
    }
}
