using System;
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
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            bool success = Kernel32Native.WriteProcessMemory(session.Handle, address, buffer, buffer.Length, out int numberOfBytesWritten);
            if (!success)
            {
                MemoryRobotException.ThrowLast("WriteProcessMemory");
            }

            return numberOfBytesWritten == buffer.Length;
        }

        public bool Write<T>(IntPtr address, T value) where T : struct
        {
            return WriteBytes(address, StructMarshaller.ToBytes(value));
        }
    }
}
