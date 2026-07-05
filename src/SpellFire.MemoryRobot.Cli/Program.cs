using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Diagnostics;
using SpellFire.MemoryRobot.Native;
using SpellFire.MemoryRobot.Process;
using SpellFire.MemoryRobot.Reading;
using SpellFire.MemoryRobot.Writing;
using SpellFire.Runtime;
using SpellFire.Runtime.Bootstrap;
using SpellFire.Runtime.Models;

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
                    case "runtime-probe":
                        return RunRuntimeProbe(processId);
                    case "runtime-evaluate":
                        return RunRuntimeEvaluate(processId);
                    case "runtime-evaluate-expect":
                        return RunRuntimeEvaluateExpect(processId, args);
                    case "runtime-host-adapter":
                        return RunRuntimeHostAdapter(processId);
                    case "runtime-host-adapter-expect":
                        return RunRuntimeHostAdapterExpect(processId, args);
                    case "runtime-connect-disconnect":
                        return RunRuntimeConnectDisconnect(processId);
                    case "runtime-lifecycle-audit":
                        return RunRuntimeLifecycleAudit(processId);
                    case "probe-expect":
                        return RunProbeExpect(processId, args);
                    case "session-open-close":
                        return RunSessionOpenClose(processId);
                    case "close-then-reopen":
                        return RunCloseThenReopen(processId);
                    case "snapshot-after-close":
                        return RunSnapshotAfterClose(processId);
                    case "session-close-all":
                        return RunSessionCloseAll(processId);
                    case "process-exit-after-open":
                        return RunProcessExitAfterOpen();
                    case "module-snapshot":
                        return RunModuleSnapshot(processId);
                    case "memory-region":
                        return RunMemoryRegion(processId);
                    case "remote-alloc-free":
                        return RunRemoteAllocFree(processId);
                    case "write-remote-allocation":
                        return RunWriteRemoteAllocation(processId);
                    case "remote-thread-invalid-start":
                        return RunRemoteThreadInvalidStart(processId);
                    case "load-library-missing-file":
                        return RunLoadLibraryMissingFile(processId);
                    case "self-remote-thread-get-current-process-id":
                        return RunSelfRemoteThreadGetCurrentProcessId();
                    case "self-load-library-known-system-dll":
                        return RunSelfLoadLibraryKnownSystemDll();
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
            WriteLine((ready ? "OK" : "FAIL") + " probe TargetProcessId=" + processId + " Reason=\"" + probe.Reason + "\" " + FormatProbe(probe));
            return ready ? 0 : 1;
        }

        private static int RunRuntimeProbe(int processId)
        {
            var facade = new RuntimeFacade();
            RuntimeMemoryProbeSnapshot probe = facade.ProbeMemory(processId);
            bool ok = probe.Ready && string.Equals(probe.Reason, "SessionOpened", StringComparison.Ordinal);
            WriteLine((ok ? "OK" : "FAIL") +
                      " runtime-probe TargetProcessId=" + processId +
                      " Ready=" + probe.Ready +
                      " Reason=\"" + Escape(probe.Reason) + "\"" +
                      " ProcessFound=" + probe.ProcessFound +
                      " ProcessName=\"" + Escape(probe.ProcessName) + "\"" +
                      " TargetWow64Known=" + probe.TargetWow64Known +
                      " TargetWow64=" + probe.TargetWow64 +
                      " Win32Error=" + probe.Win32Error +
                      " Win32Message=\"" + Escape(probe.Win32Message) + "\"");
            return ok ? 0 : 1;
        }

        private static int RunRuntimeEvaluate(int processId)
        {
            var facade = new RuntimeFacade();
            RuntimeEvaluationSnapshot beforeConnect = facade.Evaluate(processId);
            RuntimeConnectionSnapshot connected = facade.Connect(processId);
            RuntimeEvaluationSnapshot afterConnect = facade.Evaluate(processId);
            RuntimeConnectionSnapshot disconnected = facade.Disconnect(processId);
            RuntimeEvaluationSnapshot afterDisconnect = facade.Evaluate(processId);

            bool ok = beforeConnect.ReadyToConnect &&
                      string.Equals(beforeConnect.Decision, "ReadyToConnect", StringComparison.Ordinal) &&
                      connected.Connected &&
                      !afterConnect.ReadyToConnect &&
                      string.Equals(afterConnect.Decision, "AlreadyConnected", StringComparison.Ordinal) &&
                      disconnected.Disconnected &&
                      afterDisconnect.ReadyToConnect &&
                      string.Equals(afterDisconnect.Decision, "ReadyToConnect", StringComparison.Ordinal);

            WriteLine((ok ? "OK" : "FAIL") +
                      " runtime-evaluate TargetProcessId=" + processId +
                      " BeforeConnect=" + FormatRuntimeEvaluation(beforeConnect) +
                      " Connected=" + FormatRuntimeConnection(connected) +
                      " AfterConnect=" + FormatRuntimeEvaluation(afterConnect) +
                      " Disconnected=" + FormatRuntimeConnection(disconnected) +
                      " AfterDisconnect=" + FormatRuntimeEvaluation(afterDisconnect));
            return ok ? 0 : 1;
        }

        private static int RunRuntimeEvaluateExpect(int processId, string[] args)
        {
            string expectedDecision = args.Length >= 3 ? args[2] : string.Empty;
            if (string.IsNullOrWhiteSpace(expectedDecision))
            {
                WriteLine("FAIL runtime-evaluate-expect TargetProcessId=" + processId + " Reason=\"MissingExpectedDecision\"");
                return 2;
            }

            var facade = new RuntimeFacade();
            RuntimeEvaluationSnapshot evaluation = facade.Evaluate(processId);
            bool ok = string.Equals(evaluation.Decision, expectedDecision, StringComparison.Ordinal);
            WriteLine((ok ? "OK" : "FAIL") +
                      " runtime-evaluate-expect TargetProcessId=" + processId +
                      " ExpectedDecision=\"" + Escape(expectedDecision) + "\"" +
                      " ActualDecision=\"" + Escape(evaluation.Decision) + "\"" +
                      " Evaluation=" + FormatRuntimeEvaluation(evaluation));
            return ok ? 0 : 1;
        }

        private static int RunRuntimeHostAdapter(int processId)
        {
            var adapter = RuntimeCompositionRoot.CreateDefaultHostAdapter();
            RuntimeHostAttachSnapshot attached = adapter.Attach(processId);
            RuntimeConnectionSnapshot status = adapter.GetConnection(processId);
            RuntimeConnectionSnapshot detached = adapter.Detach(processId);
            RuntimeConnectionSnapshot afterDetach = adapter.GetConnection(processId);

            bool ok = attached.Accepted &&
                      string.Equals(attached.Decision, "Connected", StringComparison.Ordinal) &&
                      attached.Evaluation != null &&
                      attached.Evaluation.ReadyToConnect &&
                      attached.Connection != null &&
                      attached.Connection.Connected &&
                      status.Connected &&
                      detached.Disconnected &&
                      !afterDetach.SessionExists &&
                      string.Equals(afterDetach.Reason, "SessionNotFound", StringComparison.Ordinal);

            WriteLine((ok ? "OK" : "FAIL") +
                      " runtime-host-adapter TargetProcessId=" + processId +
                      " Attached=" + FormatRuntimeHostAttach(attached) +
                      " Status=" + FormatRuntimeConnection(status) +
                      " Detached=" + FormatRuntimeConnection(detached) +
                      " AfterDetach=" + FormatRuntimeConnection(afterDetach));
            return ok ? 0 : 1;
        }

        private static int RunRuntimeHostAdapterExpect(int processId, string[] args)
        {
            string expectedDecision = args.Length >= 3 ? args[2] : string.Empty;
            if (string.IsNullOrWhiteSpace(expectedDecision))
            {
                WriteLine("FAIL runtime-host-adapter-expect TargetProcessId=" + processId + " Reason=\"MissingExpectedDecision\"");
                return 2;
            }

            var adapter = RuntimeCompositionRoot.CreateDefaultHostAdapter();
            RuntimeHostAttachSnapshot attached = adapter.Attach(processId);
            RuntimeConnectionSnapshot status = adapter.GetConnection(processId);
            bool ok = !attached.Accepted &&
                      string.Equals(attached.Decision, expectedDecision, StringComparison.Ordinal) &&
                      !status.SessionExists &&
                      string.Equals(status.Reason, "SessionNotFound", StringComparison.Ordinal);

            WriteLine((ok ? "OK" : "FAIL") +
                      " runtime-host-adapter-expect TargetProcessId=" + processId +
                      " ExpectedDecision=\"" + Escape(expectedDecision) + "\"" +
                      " Attached=" + FormatRuntimeHostAttach(attached) +
                      " Status=" + FormatRuntimeConnection(status));
            return ok ? 0 : 1;
        }

        private static int RunRuntimeConnectDisconnect(int processId)
        {
            var facade = new RuntimeFacade();
            RuntimeConnectionSnapshot connected = facade.Connect(processId);
            RuntimeConnectionSnapshot statusAfterConnect = facade.GetConnection(processId);
            RuntimeConnectionSnapshot disconnected = facade.Disconnect(processId);
            RuntimeConnectionSnapshot statusAfterDisconnect = facade.GetConnection(processId);

            bool ok = connected.Connected &&
                      connected.SessionExists &&
                      !connected.SessionDisposed &&
                      statusAfterConnect.Connected &&
                      disconnected.Disconnected &&
                      disconnected.SessionDisposed &&
                      !statusAfterDisconnect.SessionExists &&
                      !statusAfterDisconnect.Connected &&
                      string.Equals(statusAfterDisconnect.Reason, "SessionNotFound", StringComparison.Ordinal);

            WriteLine((ok ? "OK" : "FAIL") +
                      " runtime-connect-disconnect TargetProcessId=" + processId +
                      " Connected=" + FormatRuntimeConnection(connected) +
                      " StatusAfterConnect=" + FormatRuntimeConnection(statusAfterConnect) +
                      " Disconnected=" + FormatRuntimeConnection(disconnected) +
                      " StatusAfterDisconnect=" + FormatRuntimeConnection(statusAfterDisconnect));
            return ok ? 0 : 1;
        }

        private static int RunRuntimeLifecycleAudit(int processId)
        {
            var facade = new RuntimeFacade();
            RuntimeConnectionSnapshot firstConnect = facade.Connect(processId);
            RuntimeConnectionSnapshot secondConnect = facade.Connect(processId);
            RuntimeConnectionSnapshot firstDisconnect = facade.Disconnect(processId);
            RuntimeConnectionSnapshot secondDisconnect = facade.Disconnect(processId);

            bool repeatedConnectOk = firstConnect.Connected &&
                                     secondConnect.Connected &&
                                     firstConnect.SessionExists &&
                                     secondConnect.SessionExists;
            bool repeatedDisconnectOk = firstDisconnect.Disconnected &&
                                        firstDisconnect.SessionDisposed &&
                                        !secondDisconnect.SessionExists &&
                                        string.Equals(secondDisconnect.Reason, "SessionNotFound", StringComparison.Ordinal);
            bool targetExitOk = RunRuntimeTargetExitAudit(out string targetExitDetail);
            bool ok = repeatedConnectOk && repeatedDisconnectOk && targetExitOk;

            WriteLine((ok ? "OK" : "FAIL") +
                      " runtime-lifecycle-audit TargetProcessId=" + processId +
                      " RepeatedConnectOk=" + repeatedConnectOk +
                      " FirstConnect=" + FormatRuntimeConnection(firstConnect) +
                      " SecondConnect=" + FormatRuntimeConnection(secondConnect) +
                      " RepeatedDisconnectOk=" + repeatedDisconnectOk +
                      " FirstDisconnect=" + FormatRuntimeConnection(firstDisconnect) +
                      " SecondDisconnect=" + FormatRuntimeConnection(secondDisconnect) +
                      " TargetExitOk=" + targetExitOk +
                      " TargetExit={" + targetExitDetail + "}");
            return ok ? 0 : 1;
        }

        private static bool RunRuntimeTargetExitAudit(out string detail)
        {
            System.Diagnostics.Process child = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
                Arguments = "/c ping 127.0.0.1 -n 2 > nul",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            if (child == null)
            {
                detail = "ChildProcessStartFailed";
                return false;
            }

            int childPid = child.Id;
            var childFacade = new RuntimeFacade();
            RuntimeConnectionSnapshot connected = childFacade.Connect(childPid);
            bool exited = child.WaitForExit(5000);
            RuntimeConnectionSnapshot disconnected = childFacade.Disconnect(childPid);
            RuntimeConnectionSnapshot afterDisconnect = childFacade.GetConnection(childPid);

            bool ok = connected.Connected &&
                      exited &&
                      disconnected.Disconnected &&
                      disconnected.SessionDisposed &&
                      !afterDisconnect.SessionExists &&
                      string.Equals(afterDisconnect.Reason, "SessionNotFound", StringComparison.Ordinal);

            detail = "ChildProcessId=" + childPid +
                     " Connected=" + FormatRuntimeConnection(connected) +
                     " ChildExited=" + exited +
                     " Disconnected=" + FormatRuntimeConnection(disconnected) +
                     " AfterDisconnect=" + FormatRuntimeConnection(afterDisconnect);
            return ok;
        }

        private static int RunProbeExpect(int processId, string[] args)
        {
            string expected = args.Length >= 3 ? args[2] : string.Empty;
            if (string.IsNullOrWhiteSpace(expected))
            {
                WriteLine("FAIL probe-expect TargetProcessId=" + processId + " Reason=\"MissingExpectedReason\"");
                return 2;
            }

            MemorySessionProbeResult probe = new MemorySessionDiagnostics().Probe(processId);
            bool ok = string.Equals(probe.Reason, expected, StringComparison.Ordinal);
            WriteLine((ok ? "OK" : "FAIL") +
                      " probe-expect TargetProcessId=" + processId +
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
                          " session-open-close TargetProcessId=" + processId +
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
                              " close-then-reopen TargetProcessId=" + processId +
                              " FirstOpen=" + firstOpen +
                              " CloseResult=" + closeResult +
                              " SecondOpen=" + second.Session.IsOpen +
                              " Snapshot=" + FormatSession(snapshot));
                    return ok ? 0 : 1;
                }
            }
        }

        private static int RunSnapshotAfterClose(int processId)
        {
            using (IMemoryRobot robot = SessionFactory.Open(processId))
            {
                bool closeResult = SessionFactory.CloseSession(processId);
                MemorySessionSnapshot snapshot;
                bool hasSnapshot = SessionFactory.TryGetSession(processId, out snapshot);
                bool ok = closeResult && !hasSnapshot;
                WriteLine((ok ? "OK" : "FAIL") +
                          " snapshot-after-close TargetProcessId=" + processId +
                          " CloseResult=" + closeResult +
                          " HasSnapshot=" + hasSnapshot +
                          " Snapshot=" + FormatSession(snapshot));
                return ok ? 0 : 1;
            }
        }

        private static int RunSessionCloseAll(int processId)
        {
            using (IMemoryRobot robot = SessionFactory.Open(processId))
            {
                MemorySessionSnapshot before;
                bool hasBefore = SessionFactory.TryGetSession(processId, out before);
                SessionFactory.ReleaseAll();
                MemorySessionSnapshot after;
                bool hasAfter = SessionFactory.TryGetSession(processId, out after);
                bool ok = hasBefore && !hasAfter;
                WriteLine((ok ? "OK" : "FAIL") +
                          " session-close-all TargetProcessId=" + processId +
                          " HasBefore=" + hasBefore +
                          " HasAfter=" + hasAfter +
                          " Before=" + FormatSession(before) +
                          " After=" + FormatSession(after));
                return ok ? 0 : 1;
            }
        }

        private static int RunProcessExitAfterOpen()
        {
            System.Diagnostics.Process child = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
                Arguments = "/c exit 0",
                CreateNoWindow = true,
                UseShellExecute = false
            });
            if (child == null)
            {
                WriteLine("FAIL process-exit-after-open Reason=\"ChildProcessStartFailed\"");
                return 1;
            }

            int childPid = child.Id;
            using (IMemoryRobot robot = SessionFactory.Open(childPid))
            {
                bool opened = robot.Session.IsOpen;
                child.WaitForExit(5000);
                Thread.Sleep(100);
                MemorySessionSnapshot beforeAcquire;
                bool hasBeforeAcquire = SessionFactory.TryGetSession(childPid, out beforeAcquire);

                bool acquireFailed = false;
                string acquireException = string.Empty;
                try
                {
                    using (IMemoryRobot ignored = SessionFactory.Open(childPid))
                    {
                    }
                }
                catch (Exception ex)
                {
                    acquireFailed = true;
                    acquireException = ex.GetType().Name;
                }

                MemorySessionSnapshot afterAcquire;
                bool hasAfterAcquire = SessionFactory.TryGetSession(childPid, out afterAcquire);
                bool closeResult = SessionFactory.CloseSession(childPid);
                MemorySessionSnapshot afterClose;
                bool hasAfterClose = SessionFactory.TryGetSession(childPid, out afterClose);
                bool ok = opened && child.HasExited && hasBeforeAcquire && beforeAcquire.HasExited && acquireFailed && !hasAfterAcquire && !hasAfterClose;
                WriteLine((ok ? "OK" : "FAIL") +
                          " process-exit-after-open ChildProcessId=" + childPid +
                          " Opened=" + opened +
                          " ChildExited=" + child.HasExited +
                          " HasBeforeAcquire=" + hasBeforeAcquire +
                          " BeforeAcquire=" + FormatSession(beforeAcquire) +
                          " AcquireFailed=" + acquireFailed +
                          " AcquireException=\"" + Escape(acquireException) + "\"" +
                          " HasAfterAcquire=" + hasAfterAcquire +
                          " CloseResult=" + closeResult +
                          " HasAfterClose=" + hasAfterClose);
                return ok ? 0 : 1;
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
                          " module-snapshot TargetProcessId=" + processId +
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
                          " memory-region TargetProcessId=" + processId +
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
                              " remote-alloc-free TargetProcessId=" + processId +
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
                              " write-remote-allocation TargetProcessId=" + processId +
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
                          " try-read-invalid TargetProcessId=" + processId +
                          " Success=" + result.Success +
                          " RequestedBytes=" + result.RequestedBytes +
                          " BytesRead=" + result.BytesRead +
                          " Win32Error=" + result.Win32Error +
                          " ErrorMessage=\"" + Escape(result.ErrorMessage) + "\"");
                return ok ? 0 : 1;
            }
        }

        private static int RunRemoteThreadInvalidStart(int processId)
        {
            using (IMemoryRobot robot = SessionFactory.Open(processId))
            {
                try
                {
                    robot.Threads.Run(IntPtr.Zero, IntPtr.Zero, 1000);
                    WriteLine("FAIL remote-thread-invalid-start TargetProcessId=" + processId + " Reason=\"UnexpectedSuccess\"");
                    return 1;
                }
                catch (ArgumentException ex)
                {
                    WriteLine("OK remote-thread-invalid-start TargetProcessId=" + processId +
                              " Exception=\"" + ex.GetType().Name + "\"" +
                              " Message=\"" + Escape(ex.Message) + "\"");
                    return 0;
                }
            }
        }

        private static int RunLoadLibraryMissingFile(int processId)
        {
            using (IMemoryRobot robot = SessionFactory.Open(processId))
            {
                string missingPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "SpellFire.MemoryRobot.Missing." + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".dll");
                try
                {
                    robot.Libraries.LoadLibrary(missingPath, 1000);
                    WriteLine("FAIL load-library-missing-file TargetProcessId=" + processId + " Reason=\"UnexpectedSuccess\" Path=\"" + Escape(missingPath) + "\"");
                    return 1;
                }
                catch (System.IO.FileNotFoundException ex)
                {
                    WriteLine("OK load-library-missing-file TargetProcessId=" + processId +
                              " Exception=\"" + ex.GetType().Name + "\"" +
                              " Path=\"" + Escape(missingPath) + "\"");
                    return 0;
                }
            }
        }

        private static int RunSelfRemoteThreadGetCurrentProcessId()
        {
            int selfProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            using (IMemoryRobot robot = SessionFactory.Open(selfProcessId))
            {
                IntPtr kernel32Handle = robot.SystemLibraries.GetModuleHandle("kernel32.dll");
                IntPtr procAddress = robot.SystemLibraries.GetProcAddress(kernel32Handle, "GetCurrentProcessId");

                uint exitCode = robot.Threads.Run(procAddress, IntPtr.Zero, 5000);
                bool ok = exitCode == unchecked((uint)selfProcessId);
                WriteLine((ok ? "OK" : "FAIL") +
                          " self-remote-thread-get-current-process-id SelfProcessId=" + selfProcessId +
                          " ExitCode=" + exitCode +
                          " Expected=" + selfProcessId);
                return ok ? 0 : 1;
            }
        }

        private static int RunSelfLoadLibraryKnownSystemDll()
        {
            int selfProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            string systemDirectory = Environment.SystemDirectory;
            string dllPath = System.IO.Path.Combine(systemDirectory, "version.dll");
            using (IMemoryRobot robot = SessionFactory.Open(selfProcessId))
            {
                int moduleHandle = robot.Libraries.LoadLibrary(dllPath, 5000);
                bool ok = moduleHandle != 0;
                WriteLine((ok ? "OK" : "FAIL") +
                          " self-load-library-known-system-dll SelfProcessId=" + selfProcessId +
                          " Path=\"" + Escape(dllPath) + "\"" +
                          " ModuleHandle=0x" + moduleHandle.ToString("X", CultureInfo.InvariantCulture));
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

            return "SnapshotProcessId=" + snapshot.ProcessId +
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

        private static string FormatRuntimeConnection(RuntimeConnectionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "none";
            }

            return "{Connected=" + snapshot.Connected +
                   " Disconnected=" + snapshot.Disconnected +
                   " HostState=\"" + Escape(snapshot.HostState) + "\"" +
                   " SessionExists=" + snapshot.SessionExists +
                   " SessionDisposed=" + snapshot.SessionDisposed +
                   " Reason=\"" + Escape(snapshot.Reason) + "\"" +
                   " Components=" + (snapshot.Components == null ? 0 : snapshot.Components.Count) +
                   "}";
        }

        private static string FormatRuntimeEvaluation(RuntimeEvaluationSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "none";
            }

            return "{ReadyToConnect=" + snapshot.ReadyToConnect +
                   " Decision=\"" + Escape(snapshot.Decision) + "\"" +
                   " MemoryReady=" + (snapshot.Memory != null && snapshot.Memory.Ready) +
                   " MemoryReason=\"" + Escape(snapshot.Memory == null ? string.Empty : snapshot.Memory.Reason) + "\"" +
                   " Connection=" + FormatRuntimeConnection(snapshot.Connection) +
                   "}";
        }

        private static string FormatRuntimeHostAttach(RuntimeHostAttachSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "none";
            }

            return "{Accepted=" + snapshot.Accepted +
                   " Decision=\"" + Escape(snapshot.Decision) + "\"" +
                   " Evaluation=" + FormatRuntimeEvaluation(snapshot.Evaluation) +
                   " Connection=" + FormatRuntimeConnection(snapshot.Connection) +
                   "}";
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
            WriteLine("Usage: SpellFire.MemoryRobot.Cli <probe|runtime-probe|runtime-evaluate|runtime-evaluate-expect|runtime-host-adapter|runtime-host-adapter-expect|runtime-connect-disconnect|runtime-lifecycle-audit|probe-expect|session-open-close|close-then-reopen|snapshot-after-close|session-close-all|process-exit-after-open|module-snapshot|memory-region|remote-alloc-free|write-remote-allocation|remote-thread-invalid-start|load-library-missing-file|self-remote-thread-get-current-process-id|self-load-library-known-system-dll|try-read-invalid> [pid] [expectedReason]");
        }
    }
}
