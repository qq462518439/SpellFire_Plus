using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SpellFire.MemoryRobot.Diagnostics
{
    public sealed class MemoryRobotException : InvalidOperationException
    {
        public MemoryRobotException(string operation, int win32Error)
            : base(BuildMessage(operation, win32Error))
        {
            Operation = operation;
            Win32Error = win32Error;
        }

        public string Operation { get; }

        public int Win32Error { get; }

        private static string BuildMessage(string operation, int win32Error)
        {
            string nativeMessage = new Win32Exception(win32Error).Message;
            return string.Format("{0} failed. Win32Error={1} Message=\"{2}\"", operation, win32Error, nativeMessage);
        }

        public static void ThrowLast(string operation)
        {
            throw new MemoryRobotException(operation, Marshal.GetLastWin32Error());
        }
    }
}
