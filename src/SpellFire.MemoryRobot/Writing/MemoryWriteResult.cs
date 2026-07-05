using System;

namespace SpellFire.MemoryRobot.Writing
{
    public sealed class MemoryWriteResult
    {
        public bool Success { get; set; }

        public IntPtr Address { get; set; }

        public int RequestedBytes { get; set; }

        public int BytesWritten { get; set; }

        public int Win32Error { get; set; }

        public string ErrorMessage { get; set; }
    }
}
