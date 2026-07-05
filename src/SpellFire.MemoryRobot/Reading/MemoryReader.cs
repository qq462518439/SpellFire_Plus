using System;
using System.Collections.Generic;
using System.Text;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Diagnostics;
using SpellFire.MemoryRobot.Process;
using SpellFire.MemoryRobot.Native;

namespace SpellFire.MemoryRobot.Reading
{
    public sealed class MemoryReader : IMemoryReader
    {
        private const int StringChunkLength = 32;
        private readonly MemorySession session;

        public MemoryReader(MemorySession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public byte[] ReadBytes(IntPtr address, int size)
        {
            byte[] buffer = new byte[size];
            if (!Kernel32Native.ReadProcessMemory(session.Handle, address, buffer, buffer.Length, out int numberOfBytesRead))
            {
                MemoryRobotException.ThrowLast("ReadProcessMemory");
            }

            if (numberOfBytesRead == buffer.Length)
            {
                return buffer;
            }

            byte[] resized = new byte[Math.Max(0, numberOfBytesRead)];
            Array.Copy(buffer, resized, resized.Length);
            return resized;
        }

        public T Read<T>(IntPtr address) where T : struct
        {
            return StructMarshaller.FromBytes<T>(ReadBytes(address, System.Runtime.InteropServices.Marshal.SizeOf(typeof(T))));
        }

        public string ReadString(IntPtr address, int maxBytes = 256)
        {
            List<byte> chars = new List<byte>(Math.Max(maxBytes, StringChunkLength));
            IntPtr current = address;
            int remaining = maxBytes;

            while (remaining > 0)
            {
                int toRead = Math.Min(StringChunkLength, remaining);
                byte[] chunk = ReadBytes(current, toRead);
                if (chunk.Length == 0)
                {
                    break;
                }

                for (int i = 0; i < chunk.Length; i++)
                {
                    byte character = chunk[i];
                    chars.Add(character);
                    if (character == 0)
                    {
                        remaining = 0;
                        break;
                    }
                }

                remaining -= chunk.Length;
                current = IntPtr.Add(current, chunk.Length);
            }

            byte[] arr = chars.ToArray();
            int len = arr.Length;
            while (len > 0 && arr[len - 1] == 0)
            {
                len--;
            }

            return Encoding.UTF8.GetString(arr, 0, len);
        }
    }
}
