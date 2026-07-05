using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Diagnostics;
using SpellFire.MemoryRobot.Process;
using SpellFire.MemoryRobot.Native;
using SpellFire.MemoryRobot.Reading;

namespace SpellFire.MemoryRobot.Writing
{
    public sealed class MemoryWriter : IMemoryWriter
    {
        private readonly MemorySession session;

        public MemoryWriter(MemorySession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public bool WriteBytes(IntPtr address, byte[] buffer)
        {
            MemoryWriteResult result = TryWriteBytes(address, buffer);
            if (!result.Success && result.Win32Error != 0)
            {
                throw new MemoryRobotException("WriteProcessMemory", result.Win32Error);
            }

            return result.Success;
        }

        public MemoryWriteResult TryWriteBytes(IntPtr address, byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            bool success = Kernel32Native.WriteProcessMemory(session.Handle, address, buffer, buffer.Length, out int numberOfBytesWritten);
            int bytesWritten = Math.Max(0, numberOfBytesWritten);
            int win32Error = success ? 0 : Marshal.GetLastWin32Error();
            return new MemoryWriteResult
            {
                Success = success && bytesWritten == buffer.Length,
                Address = address,
                RequestedBytes = buffer.Length,
                BytesWritten = bytesWritten,
                Win32Error = win32Error,
                ErrorMessage = win32Error == 0 ? string.Empty : new Win32Exception(win32Error).Message
            };
        }

        public bool Write<T>(IntPtr address, T value) where T : struct
        {
            return WriteBytes(address, StructMarshaller.ToBytes(value));
        }

        public MemoryWriteResult TryWrite<T>(IntPtr address, T value) where T : struct
        {
            return TryWriteBytes(address, StructMarshaller.ToBytes(value));
        }
    }
}
