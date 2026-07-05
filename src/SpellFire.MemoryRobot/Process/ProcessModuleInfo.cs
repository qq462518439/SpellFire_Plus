using System;

namespace SpellFire.MemoryRobot.Process
{
    public sealed class ProcessModuleInfo
    {
        public string Name { get; set; }

        public string FileName { get; set; }

        public IntPtr BaseAddress { get; set; }

        public int ModuleMemorySize { get; set; }
    }
}
