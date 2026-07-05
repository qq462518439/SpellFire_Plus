using System;
using SpellFire.MemoryRobot.Diagnostics;
using SpellFire.Runtime.Contracts;
using SpellFire.Runtime.Models;

namespace SpellFire.Runtime.Services
{
    public sealed class RuntimeMemoryProbeService : IRuntimeMemoryProbeService
    {
        private readonly MemorySessionDiagnostics diagnostics;

        public RuntimeMemoryProbeService()
            : this(new MemorySessionDiagnostics())
        {
        }

        public RuntimeMemoryProbeService(MemorySessionDiagnostics diagnostics)
        {
            this.diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public RuntimeMemoryProbeSnapshot Probe(int processId)
        {
            MemorySessionProbeResult probe = diagnostics.Probe(processId);
            return new RuntimeMemoryProbeSnapshot
            {
                ProcessId = probe.ProcessId,
                Ready = string.Equals(probe.Reason, "SessionOpened", StringComparison.Ordinal),
                Reason = probe.Reason,
                ProcessName = probe.ProcessName,
                ProcessFound = probe.ProcessFound,
                Responding = probe.Responding,
                HostIs64BitOperatingSystem = probe.HostIs64BitOperatingSystem,
                HostIs64BitProcess = probe.HostIs64BitProcess,
                TargetWow64Known = probe.TargetWow64Known,
                TargetWow64 = probe.TargetWow64,
                RequestedAccess = probe.RequestedAccess.ToString(),
                Win32Error = probe.Win32Error,
                Win32Message = probe.Win32Message
            };
        }
    }
}
