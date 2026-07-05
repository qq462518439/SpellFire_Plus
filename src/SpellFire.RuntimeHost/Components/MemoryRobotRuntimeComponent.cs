using System;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Diagnostics;
using SpellFire.MemoryRobot.Process;
using SpellFire.RuntimeHost.Abstractions;

namespace SpellFire.RuntimeHost.Components
{
    public sealed class MemoryRobotRuntimeComponent : IRuntimeComponent
    {
        private readonly IMemorySessionFactory sessionFactory;
        private readonly MemorySessionDiagnostics diagnostics;

        public MemoryRobotRuntimeComponent(IMemorySessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            diagnostics = new MemorySessionDiagnostics();
        }

        public string Name => "MemoryRobot";

        public RuntimeComponentStatus Probe(int processId)
        {
            MemorySessionProbeResult probe = diagnostics.Probe(processId);
            if (!string.Equals(probe.Reason, "SessionOpened", StringComparison.Ordinal))
            {
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = probe.Reason,
                    Detail = FormatProbe(probe)
                };
            }

            try
            {
                using (IMemoryRobot robot = sessionFactory.Open(processId))
                {
                    return new RuntimeComponentStatus
                    {
                        Name = Name,
                        Ready = robot.Session.IsOpen,
                        Reason = robot.Session.IsOpen ? "SessionOpened" : "SessionClosed",
                        Detail = FormatProbe(probe)
                    };
                }
            }
            catch (Exception ex)
            {
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = ex.GetType().Name + ":" + ex.Message
                };
            }
        }

        public void Cleanup(int processId)
        {
            sessionFactory.CloseSession(processId);
        }

        private static string FormatProbe(MemorySessionProbeResult probe)
        {
            return " ProcessFound=" + probe.ProcessFound +
                   " ProcessName=\"" + (probe.ProcessName ?? string.Empty) + "\"" +
                   " Responding=" + probe.Responding +
                   " HostX64OS=" + probe.HostIs64BitOperatingSystem +
                   " HostX64Process=" + probe.HostIs64BitProcess +
                   " TargetWow64Known=" + probe.TargetWow64Known +
                   " TargetWow64=" + probe.TargetWow64 +
                   " RequestedAccess=" + probe.RequestedAccess +
                   " Win32Error=" + probe.Win32Error +
                   " Win32Message=\"" + probe.Win32Message + "\"";
        }
    }
}
