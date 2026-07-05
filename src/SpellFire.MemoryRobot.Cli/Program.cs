using System;
using System.Globalization;
using System.Linq;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Diagnostics;
using SpellFire.MemoryRobot.Native;
using SpellFire.MemoryRobot.Process;
using SpellFire.MemoryRobot.Reading;
using SpellFire.MemoryRobot.Writing;

namespace SpellFire.MemoryRobot.Cli
{
    internal static class Program
    {
        private static readonly MemorySessionFactory SessionFactory = new MemorySessionFactory(new MemoryRobotSessionManager());

        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                WriteUsage();
                return 2;
            }

            string command = args[0].Trim().ToLowerInvariant();
            int processId = ParseProcessId(args);

            try
            {
                switch (command)
                {
                    case "probe":
                        return RunProbe(processId);
                    case "probe-expect":
                        return RunProbeExpect(processId, args);
                    case "session-open-close":
                        return RunSessionOpenClose(processId);
                    case "close-then-reopen":
                        return RunCloseThenReopen(processId);
                    case "module-snapshot":
                        return RunModuleSnapshot(processId);
                    case "memory-region":
                        return RunMemoryRegion(processId);
                    case "remote-alloc-free":
                        return RunRemoteAllocFree(processId);
                    case "write-remote-allocation":
                        return RunWriteRemoteAllocation(processId);
                    case "try-read-invalid":
                        return RunTryReadInvalid(processId);
                    default:
                        WriteLine("FAIL unknown-command Command=\"" + command + "\"");
                        WriteUsage();
                        return 2;
                }
            }
            catch (Exception ex)
            {
                WriteLine("FAIL " + command + " Exception=\"" + ex.GetType().Name + "\" Message=\"" + Escape(ex.Message) + "\"");
                return 1;
            }
        }

        private static int RunProbe(int processId)
        {
            MemorySessionProbeResult probe = new MemorySessionDiagnostics().Probe(processId);
            bool ready = string.Equals(probe.Reason, "SessionOpened", StringComparison.Ordinal);
            WriteLine((ready ? "OK" : "FAIL") + " probe ProcessId=" + processId + " Reason=\"" + probe.Reason + "\" " + FormatProbe(probe));
            return ready ? 0 : 1;
        }

        private static int RunProbeExpect(int processId, string[] args)
        {
            string expected = args.Length >= 3 ? args[2] : string.Empty;
            if (string.IsNullOrWhiteSpace(expected))
            {
                WriteLine("FAIL probe-expect ProcessId=" + processId + " Reason=\"MissingExpectedReason\"");
                return 2;
            }

            MemorySessionProbeResult probe = new MemorySessionDiagnostics().Probe(processId);
            bool ok = string.Equals(probe.Reason, expected, StringComparison.Ordinal);
            WriteLine((ok ? "OK" : "FAIL") +
                      " probe-expect ProcessId=" + processId +
                      " ExpectedReason=\"" + Escape(expected) + "\"" +
                      " ActualReason=\"" + Escape(probe.Reason) + "\" " +
                      FormatProbe(probe));
            return ok ? 0 : 1;
        }

        private static int RunSessionOpenClose(int processId)
        {
            using (IMemoryRobot robot = SessionFactory.Open(processId))
            {
                MemorySessionSnapshot openSnapshot;
                bool hasOpenSnapshot = SessionFactory.TryGetSession(processId, out openSnapshot);
                bool closeResult = SessionFactory.CloseSession(processId);
                MemorySessionSnapshot closedSnapshot;
                bool hasClosedSnapshot = SessionFactory.TryGetSession(processId, out closedSnapshot);
                bool ok = hasOpenSnapshot && closeResult && !hasClosedSnapshot;
                WriteLine((ok ? "OK" : "FAIL") +
                          " session-open-close ProcessId=" + processId +
                          " HasOpenSnapshot=" + hasOpenSnapshot +
                          " CloseResult=" + closeResult +
                          " HasClosedSnapshot=" + hasClosedSnapshot +
                          " Snapshot=" + FormatSession(openSnapshot));
                return ok ? 0 : 1;
            }
        }

        private static int RunCloseThenReopen(int processId)
        {
            using (IMemoryRobot first = SessionFactory.Open(processId))
            {
                bool firstOpen = first.Session.IsOpen;
                bool closeResult = SessionFactory.CloseSession(processId);
                using (IMemoryRobot second = SessionFactory.Open(processId))
                {
                    MemorySessionSnapshot snapshot;
                    bool hasSnapshot = SessionFactory.TryGetSession(processId, out snapshot);
                    bool ok = firstOpen && closeResult && second.Session.IsOpen && hasSnapshot;
                    WriteLine((ok ? "OK" : "FAIL") +
                              " close-then-reopen ProcessId=" + processId +
                              " FirstOpen=" + firstOpen +
                              " CloseResult=" + closeResult +
                              " SecondOpen=" + second.Session.IsOpen +
                              " Snapshot=" + FormatSession(snapshot));
                    return ok ? 0 : 1;
                }
            }
        }

        private static int RunModuleSnapshot(int processId)
        {
            using (IMemoryRobot robot = SessionFactory.Open(processId))
            {
                var modules = robot.Modules.GetModules();
                bool ok = modules.Count > 0;
                ProcessModuleInfo first = modules.FirstOrDefault();
                WriteLine((ok ? "OK" : "FAIL") +
                          " module-snapshot ProcessId=" + processId +
                          " Count=" + modules.Count +
                          " First=" + FormatModule(first));
                return ok ? 0 : 1;
            }
        }

        private static int RunMemoryRegion(int processId)
        {
            using (IMemoryRobot robot = SessionFactory.Open(processId))
            {
                bool ok = robot.Regions.TryQuery(IntPtr.Zero, out var region);
                WriteLine((ok ? "OK" : "FAIL") +
                          " memory-region ProcessId=" + processId +
                          " Region=" + FormatRegion(region));
                return ok ? 0 : 1;
            }
        }

        private static int RunRemoteAllocFree(int processId)
        {
            using (IMemoryRobot robot = SessionFactory.Open(processId))
            {
                IntPtr address = IntPtr.Zero;
                bool freed = false;
                try
                {
                    address = robot.Allocator.Allocate(64, AllocationType.Commit | AllocationType.Reserve, MemoryProtection.ReadWrite);
                    freed = address != IntPtr.Zero && robot.Allocator.Free(address);
                    bool ok = address != IntPtr.Zero && freed;
                    WriteLine((ok ? "OK" : "FAIL") +
                              " remote-alloc-free ProcessId=" + processId +
                              " Address=0x" + address.ToInt64().ToString("X", CultureInfo.InvariantCulture) +
                              " Freed=" + freed);
                    return ok ? 0 : 1;
                }
                finally
                {
                    if (address != IntPtr.Zero && !freed)
                    {
                        robot.Allocator.Free(address);
                    }
                }
            }
        }

        private static int RunWriteRemoteAllocation(int processId)
        {
            using (IMemoryRobot robot = SessionFactory.Open(processId))
            {
                IntPtr address = IntPtr.Zero;
                bool freed = false;
                byte[] payload = new byte[] { 0x53, 0x46, 0x4D, 0x52 };
                try
                {
                    address = robot.Allocator.Allocate(payload.Length, AllocationType.Commit | AllocationType.Reserve, MemoryProtection.ReadWrite);
                    MemoryWriteResult write = robot.Writer.TryWriteBytes(address, payload);
                    MemoryReadResult read = robot.Reader.TryReadBytes(address, payload.Length);
                    bool payloadMatches = read.Success && read.Buffer.Length == payload.Length && payload.SequenceEqual(read.Buffer);
                    freed = address != IntPtr.Zero && robot.Allocator.Free(address);
                    bool ok = address != IntPtr.Zero && write.Success && payloadMatches && freed;
                    WriteLine((ok ? "OK" : "FAIL") +
                              " write-remote-allocation ProcessId=" + processId +
                              " Address=0x" + address.ToInt64().ToString("X", CultureInfo.InvariantCulture) +
                              " WriteSuccess=" + write.Success +
                              " BytesWritten=" + write.BytesWritten +
                              " ReadSuccess=" + read.Success +
                              " BytesRead=" + read.BytesRead +
                              " PayloadMatches=" + payloadMatches +
                              " Freed=" + freed);
                    return ok ? 0 : 1;
                }
                finally
                {
                    if (address != IntPtr.Zero && !freed)
                    {
                        robot.Allocator.Free(address);
                    }
                }
            }
        }

        private static int RunTryReadInvalid(int processId)
        {
            using (IMemoryRobot robot = SessionFactory.Open(processId))
            {
                MemoryReadResult result = robot.Reader.TryReadBytes(IntPtr.Zero, 4);
                bool ok = !result.Success && result.RequestedBytes == 4;
                WriteLine((ok ? "OK" : "FAIL") +
                          " try-read-invalid ProcessId=" + processId +
                          " Success=" + result.Success +
                          " RequestedBytes=" + result.RequestedBytes +
                          " BytesRead=" + result.BytesRead +
                          " Win32Error=" + result.Win32Error +
                          " ErrorMessage=\"" + Escape(result.ErrorMessage) + "\"");
                return ok ? 0 : 1;
            }
        }

        private static int ParseProcessId(string[] args)
        {
            if (args.Length >= 2 && int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int processId) && processId > 0)
            {
                return processId;
            }

            var process = System.Diagnostics.Process.GetProcessesByName("Wow")
                .OrderByDescending(item => SafeStartTime(item))
                .FirstOrDefault();
            if (process == null)
            {
                throw new InvalidOperationException("No process id was supplied and no Wow process was found.");
            }

            return process.Id;
        }

        private static DateTime SafeStartTime(System.Diagnostics.Process process)
        {
            try
            {
                return process.StartTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static string FormatProbe(MemorySessionProbeResult probe)
        {
            return "ProcessFound=" + probe.ProcessFound +
                   " ProcessName=\"" + Escape(probe.ProcessName) + "\"" +
                   " Responding=" + probe.Responding +
                   " HostX64OS=" + probe.HostIs64BitOperatingSystem +
                   " HostX64Process=" + probe.HostIs64BitProcess +
                   " TargetWow64Known=" + probe.TargetWow64Known +
                   " TargetWow64=" + probe.TargetWow64 +
                   " RequestedAccess=" + probe.RequestedAccess +
                   " Win32Error=" + probe.Win32Error +
                   " Win32Message=\"" + Escape(probe.Win32Message) + "\"";
        }

        private static string FormatSession(MemorySessionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "none";
            }

            return "ProcessId=" + snapshot.ProcessId +
                   " ProcessName=\"" + Escape(snapshot.ProcessName) + "\"" +
                   " Handle=0x" + snapshot.Handle.ToInt64().ToString("X", CultureInfo.InvariantCulture) +
                   " IsOpen=" + snapshot.IsOpen +
                   " HasExited=" + snapshot.HasExited +
                   " ReferenceCount=" + snapshot.ReferenceCount;
        }

        private static string FormatModule(ProcessModuleInfo module)
        {
            if (module == null)
            {
                return "none";
            }

            return "Name=\"" + Escape(module.Name) + "\"" +
                   " Base=0x" + module.BaseAddress.ToInt64().ToString("X", CultureInfo.InvariantCulture) +
                   " Size=" + module.ModuleMemorySize +
                   " Path=\"" + Escape(module.FileName) + "\"";
        }

        private static string FormatRegion(MemoryMap.MemoryRegion region)
        {
            if (region == null)
            {
                return "none";
            }

            return "Base=0x" + region.BaseAddress.ToInt64().ToString("X", CultureInfo.InvariantCulture) +
                   " Size=" + region.RegionSize.ToInt64().ToString(CultureInfo.InvariantCulture) +
                   " State=" + region.State +
                   " Protect=" + region.Protect;
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void WriteLine(string line)
        {
            Console.WriteLine(line);
        }

        private static void WriteUsage()
        {
            WriteLine("Usage: SpellFire.MemoryRobot.Cli <probe|probe-expect|session-open-close|close-then-reopen|module-snapshot|memory-region|remote-alloc-free|write-remote-allocation|try-read-invalid> [pid] [expectedReason]");
        }
    }
}
