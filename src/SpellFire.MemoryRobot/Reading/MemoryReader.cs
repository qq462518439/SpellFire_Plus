using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
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
            MemoryReadResult result = TryReadBytes(address, size);
            if (!result.Success)
            {
                throw new MemoryRobotException("ReadProcessMemory", result.Win32Error);
            }

            return result.Buffer;
        }

        public MemoryReadResult TryReadBytes(IntPtr address, int size)
        {
            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Read size must not be negative.");
            }

            byte[] buffer = new byte[size];
            bool success = Kernel32Native.ReadProcessMemory(session.Handle, address, buffer, buffer.Length, out int numberOfBytesRead);
            int bytesRead = Math.Max(0, numberOfBytesRead);
            byte[] resultBuffer = buffer;
            if (bytesRead != buffer.Length)
            {
                resultBuffer = new byte[bytesRead];
                Array.Copy(buffer, resultBuffer, resultBuffer.Length);
            }

            int win32Error = success ? 0 : Marshal.GetLastWin32Error();
            return new MemoryReadResult
            {
                Success = success && bytesRead == size,
                Address = address,
                RequestedBytes = size,
                BytesRead = bytesRead,
                Buffer = resultBuffer,
                Win32Error = win32Error,
                ErrorMessage = win32Error == 0 ? string.Empty : new Win32Exception(win32Error).Message
            };
        }

        public T Read<T>(IntPtr address) where T : struct
        {
            return StructMarshaller.FromBytes<T>(ReadBytes(address, System.Runtime.InteropServices.Marshal.SizeOf(typeof(T))));
        }

        public MemoryReadResult<T> TryRead<T>(IntPtr address) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            MemoryReadResult bytes = TryReadBytes(address, size);
            var result = new MemoryReadResult<T>
            {
                Success = bytes.Success,
                Address = address,
                RequestedBytes = bytes.RequestedBytes,
                BytesRead = bytes.BytesRead,
                Win32Error = bytes.Win32Error,
                ErrorMessage = bytes.ErrorMessage
            };

            if (bytes.Success)
            {
                result.Value = StructMarshaller.FromBytes<T>(bytes.Buffer);
            }

            return result;
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
