using System;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Diagnostics;
using SpellFire.MemoryRobot.Models;

namespace SpellFire.MemoryRobot.Services
{
    public sealed class ProcessAttachService
    {
        private readonly IMemorySessionFactory sessionFactory;
        private readonly MemorySessionDiagnostics diagnostics;

        public ProcessAttachService(IMemorySessionFactory sessionFactory, MemorySessionDiagnostics diagnostics = null)
        {
            this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            this.diagnostics = diagnostics ?? new MemorySessionDiagnostics();
        }

        public ProcessAttachResult Probe(int processId)
        {
            MemorySessionProbeResult probe = diagnostics.Probe(processId);
            return new ProcessAttachResult
            {
                ProcessId = processId,
                Ready = string.Equals(probe.Reason, "SessionOpened", StringComparison.Ordinal),
                Reason = probe.Reason,
                Detail = FormatProbe(probe),
                Probe = probe
            };
        }

        public ProcessAttachResult Attach(int processId)
        {
            ProcessAttachResult result = Probe(processId);
            if (!result.Ready)
            {
                return result;
            }

            try
            {
                using (IMemoryRobot robot = sessionFactory.Open(processId))
                {
                    sessionFactory.TryGetSession(processId, out var snapshot);
                    result.Ready = robot.Session.IsOpen;
                    result.Reason = robot.Session.IsOpen ? "SessionOpened" : "SessionClosed";
                    result.Detail = (result.Detail ?? string.Empty) +
                                    " SessionOpen=" + robot.Session.IsOpen +
                                    " Handle=" + (robot.Session.Handle == IntPtr.Zero ? "null" : "0x" + robot.Session.Handle.ToString("X"));
                    result.SessionSnapshot = snapshot;
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new ProcessAttachResult
                {
                    ProcessId = processId,
                    Ready = false,
                    Reason = "SessionOpenFailed",
                    Detail = (result.Detail ?? string.Empty) + " Exception=" + ex.GetType().Name + ":" + ex.Message,
                    Probe = result.Probe,
                    SessionSnapshot = result.SessionSnapshot
                };
            }
        }

        public bool Close(int processId)
        {
            return sessionFactory.CloseSession(processId);
        }

        public void CloseAll()
        {
            sessionFactory.ReleaseAll();
        }

        private static string FormatProbe(MemorySessionProbeResult probe)
        {
            return "ProcessFound=" + probe.ProcessFound +
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
