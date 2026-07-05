using System;
using System.Collections.Generic;
using System.Linq;
using SpellFire.MemoryRobot.Abstractions;

namespace SpellFire.MemoryRobot.Process
{
    public sealed class ProcessModuleSnapshotProvider : IModuleSnapshotProvider
    {
        private readonly System.Diagnostics.Process process;

        public ProcessModuleSnapshotProvider(System.Diagnostics.Process process)
        {
            this.process = process ?? throw new ArgumentNullException(nameof(process));
        }

        public IReadOnlyList<ProcessModuleInfo> GetModules()
        {
            return process.Modules.Cast<System.Diagnostics.ProcessModule>()
                .Select(module => new ProcessModuleInfo
                {
                    Name = module.ModuleName,
                    FileName = module.FileName,
                    BaseAddress = module.BaseAddress,
                    ModuleMemorySize = module.ModuleMemorySize
                })
                .ToArray();
        }
    }
}
