using System;

namespace SpellFire.MemoryRobot.Reading
{
    public sealed class MemoryReadResult
    {
        public bool Success { get; set; }

        public IntPtr Address { get; set; }

        public int RequestedBytes { get; set; }

        public int BytesRead { get; set; }

        public byte[] Buffer { get; set; }

        public int Win32Error { get; set; }

        public string ErrorMessage { get; set; }
    }

    public sealed class MemoryReadResult<T> where T : struct
    {
        public bool Success { get; set; }

        public IntPtr Address { get; set; }

        public int RequestedBytes { get; set; }

        public int BytesRead { get; set; }

        public T Value { get; set; }

        public int Win32Error { get; set; }

        public string ErrorMessage { get; set; }
    }
}
