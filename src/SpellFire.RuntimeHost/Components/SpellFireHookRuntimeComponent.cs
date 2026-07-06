using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using SpellFire.MemoryRobot.Models;
using SpellFire.MemoryRobot.Native;
using SpellFire.MemoryRobot.Process;
using SpellFire.MemoryRobot.Services;
using SpellFire.RuntimeHost.Abstractions;

namespace SpellFire.RuntimeHost.Components
{
    public sealed class SpellFireHookRuntimeComponent : IRuntimeComponent
    {
        private readonly ProcessAttachService attachService;
        private readonly ProcessSnapshotService snapshotService;
        private readonly RemoteExecutionService remoteExecutionService;

        public SpellFireHookRuntimeComponent(
            ProcessAttachService attachService,
            ProcessSnapshotService snapshotService,
            RemoteExecutionService remoteExecutionService)
        {
            this.attachService = attachService ?? throw new ArgumentNullException(nameof(attachService));
            this.snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
            this.remoteExecutionService = remoteExecutionService ?? throw new ArgumentNullException(nameof(remoteExecutionService));
        }

        public string Name => "SpellFireHook";

        public RuntimeComponentStatus EvaluateSafetyBoundary(int processId)
        {
            RuntimeComponentStatus baseline = Probe(processId);
            if (!baseline.Ready || !string.Equals(baseline.Reason, "MemoryReadyForHook", StringComparison.Ordinal))
            {
                return baseline;
            }

            StringBuilder detail = new StringBuilder(baseline.Detail ?? string.Empty);
            detail.Append(" ReadyEvent=").Append(HookReadySignal.GetEventName(processId));
            detail.Append(" HeartbeatEvent=").Append(HookReadySignal.GetHeartbeatEventName(processId));

            try
            {
                ModuleSnapshotResult moduleSnapshot = snapshotService.GetModules(processId, "SpellFire.Hook.dll");
                using (EventWaitHandle heartbeatEvent = HookReadySignal.CreateHeartbeat(processId))
                {
                    ProcessModuleInfo existingModule = moduleSnapshot.MatchedModule;
                    bool readySignal = HookReadySignal.IsSet(processId);
                    bool heartbeatSignal = heartbeatEvent.WaitOne(0);
                    detail.Append(" ExistingModule=").Append(existingModule == null ? "none" : "0x" + existingModule.BaseAddress.ToString("X"));
                    detail.Append(" ReadySignal=").Append(readySignal);
                    detail.Append(" HeartbeatSignal=").Append(heartbeatSignal);

                    if (existingModule == null)
                    {
                        detail.Append(" DirtyProcess=False");
                        detail.Append(" AttachAllowed=True");
                        detail.Append(" CommandAllowed=False");
                        return new RuntimeComponentStatus { Name = Name, Ready = true, Reason = "SafeBoundary_CleanProcessNoHook", Detail = detail.ToString() };
                    }

                    if (readySignal && heartbeatSignal)
                    {
                        detail.Append(" DirtyProcess=False");
                        detail.Append(" AttachAllowed=True");
                        detail.Append(" CommandAllowed=True");
                        return new RuntimeComponentStatus { Name = Name, Ready = true, Reason = "SafeBoundary_HookAlive", Detail = detail.ToString() };
                    }

                    bool recoverableStale = !readySignal;
                    detail.Append(" DirtyProcess=True");
                    detail.Append(" AttachAllowed=").Append(recoverableStale);
                    detail.Append(" CommandAllowed=False");
                    return new RuntimeComponentStatus { Name = Name, Ready = false, Reason = recoverableStale ? "SafeBoundary_DirtyRecoverable_ReadyMissing" : "SafeBoundary_DirtyRefused_HeartbeatMissing", Detail = detail.ToString() };
                }
            }
            catch (Exception ex)
            {
                detail.Append(" BoundaryError=").Append(ex.GetType().Name).Append(":").Append(ex.Message);
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = "SafeBoundaryFailed",
                    Detail = detail.ToString()
                };
            }
        }

        public RuntimeComponentStatus Probe(int processId)
        {
            ProcessAttachResult attach = attachService.Attach(processId);
            if (!attach.Ready)
            {
                return new RuntimeComponentStatus { Name = Name, Ready = false, Reason = attach.Reason, Detail = attach.Detail };
            }

            RemoteExecutionResult allocationProbe = remoteExecutionService.ProbeAllocation(processId, 64, MemoryProtection.ExecuteReadWrite);
            return new RuntimeComponentStatus
            {
                Name = Name,
                Ready = allocationProbe.Ready,
                Reason = allocationProbe.Ready ? "MemoryReadyForHook" : allocationProbe.Reason,
                Detail = (attach.Detail ?? string.Empty) + " " + (allocationProbe.Detail ?? string.Empty)
            };
        }

        public RuntimeComponentStatus AttemptAttach(int processId)
        {
            RuntimeComponentStatus boundary = EvaluateSafetyBoundary(processId);
            if (string.Equals(boundary.Reason, "SafeBoundary_DirtyRefused_HeartbeatMissing", StringComparison.Ordinal))
            {
                return boundary;
            }

            RuntimeComponentStatus baseline = Probe(processId);
            if (!baseline.Ready || !string.Equals(baseline.Reason, "MemoryReadyForHook", StringComparison.Ordinal))
            {
                return baseline;
            }

            string sourcePayloadPath = HookPayloadPathResolver.GetDefaultPayloadPath();
            StringBuilder detail = new StringBuilder(baseline.Detail ?? string.Empty);
            detail.Append(" PayloadSource=").Append(sourcePayloadPath);
            detail.Append(" ReadyEvent=").Append(HookReadySignal.GetEventName(processId));

            if (!File.Exists(sourcePayloadPath))
            {
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = "HookPayloadMissing",
                    Detail = detail.ToString()
                };
            }

            try
            {
                using (EventWaitHandle readyEvent = HookReadySignal.CreateForAttach(processId))
                {
                    var existingModule = snapshotService.GetModules(processId, "SpellFire.Hook.dll").MatchedModule;
                    if (existingModule != null)
                    {
                        detail.Append(" ExistingModule=0x").Append(existingModule.BaseAddress.ToString("X"));
                        bool alreadyReady = HookReadySignal.IsSet(processId);
                        detail.Append(" ReadySignal=").Append(alreadyReady);
                        if (alreadyReady)
                        {
                            return new RuntimeComponentStatus
                            {
                                Name = Name,
                                Ready = true,
                                Reason = "HookAlreadyReady",
                                Detail = detail.ToString()
                            };
                        }

                        detail.Append(" StaleUnloadAttempted=True");
                        RemoteExecutionResult freeResult = remoteExecutionService.FreeLibrary(processId, existingModule.BaseAddress, 5000);
                        bool unloaded = freeResult.Ready;
                        detail.Append(" StaleUnloadResult=").Append(unloaded);
                        bool stillLoaded = WaitForModuleState(processId, "SpellFire.Hook.dll", present: false, timeoutMilliseconds: 8000);
                        detail.Append(" StaleModuleStillLoaded=").Append(stillLoaded);
                        if (!unloaded || stillLoaded)
                        {
                            return new RuntimeComponentStatus
                            {
                                Name = Name,
                                Ready = false,
                                Reason = "HookLoadedButReadySignalMissing_UnloadFailed",
                                Detail = detail.ToString()
                            };
                        }
                    }

                    readyEvent.Reset();
                    string payloadPath = HookPayloadPathResolver.CreateInjectableCopy(processId);
                    detail.Append(" Payload=").Append(payloadPath);
                    RemoteExecutionResult loadResult = remoteExecutionService.LoadLibrary(processId, payloadPath, 10000);
                    int moduleHandle = loadResult.ModuleHandle.ToInt32();
                    detail.Append(" LoadLibraryExit=0x").Append(moduleHandle.ToString("X"));
                    bool ready = moduleHandle != 0 && HookReadySignal.Wait(readyEvent, 3000);
                    detail.Append(" ReadySignal=").Append(ready);
                    return new RuntimeComponentStatus
                    {
                        Name = Name,
                        Ready = ready,
                        Reason = ready
                            ? "HookReady"
                            : (moduleHandle != 0 ? "AttachAttempted_ReadySignalTimeout" : "AttachAttempted_LoadLibraryReturnedZero"),
                        Detail = detail.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                detail.Append(" AttachError=").Append(ex.GetType().Name).Append(":").Append(ex.Message);
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = "AttachAttemptFailed",
                    Detail = detail.ToString()
                };
            }
        }

        public RuntimeComponentStatus GetStatus(int processId)
        {
            StringBuilder detail = new StringBuilder();
            detail.Append(" ReadyEvent=").Append(HookReadySignal.GetEventName(processId));
            detail.Append(" HeartbeatEvent=").Append(HookReadySignal.GetHeartbeatEventName(processId));

            try
            {
                using (EventWaitHandle heartbeatEvent = HookReadySignal.CreateHeartbeat(processId))
                {
                    var existingModule = snapshotService.GetModules(processId, "SpellFire.Hook.dll").MatchedModule;
                    bool ready = HookReadySignal.IsSet(processId);
                    bool heartbeat = heartbeatEvent.WaitOne(2500);
                    detail.Append(" ExistingModule=").Append(existingModule == null ? "none" : "0x" + existingModule.BaseAddress.ToString("X"));
                    detail.Append(" ReadySignal=").Append(ready);
                    detail.Append(" HeartbeatSignal=").Append(heartbeat);

                    return new RuntimeComponentStatus
                    {
                        Name = Name,
                        Ready = existingModule != null && ready && heartbeat,
                        Reason = existingModule != null && ready && heartbeat ? "HookServiceAlive" : "HookServiceUnavailable",
                        Detail = detail.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                detail.Append(" StatusError=").Append(ex.GetType().Name).Append(":").Append(ex.Message);
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = "HookStatusFailed",
                    Detail = detail.ToString()
                };
            }
        }

        public RuntimeComponentStatus RequestShutdown(int processId)
        {
            StringBuilder detail = new StringBuilder();
            detail.Append(" ShutdownEvent=").Append(HookReadySignal.GetShutdownEventName(processId));

            try
            {
                using (EventWaitHandle shutdownEvent = HookReadySignal.CreateShutdown(processId))
                {
                    shutdownEvent.Set();
                }

                RuntimeComponentStatus status = GetStatus(processId);
                detail.Append(" PostStatusReason=").Append(status.Reason);
                detail.Append(" PostStatusReady=").Append(status.Ready);
                detail.Append(" PostStatusDetail=[").Append(status.Detail).Append("]");

                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = !status.Ready,
                    Reason = status.Ready ? "HookShutdownRequestedStillAlive" : "HookShutdownRequested",
                    Detail = detail.ToString()
                };
            }
            catch (Exception ex)
            {
                detail.Append(" ShutdownError=").Append(ex.GetType().Name).Append(":").Append(ex.Message);
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = "HookShutdownFailed",
                    Detail = detail.ToString()
                };
            }
        }

        public RuntimeComponentStatus CommandPing(int processId)
        {
            StringBuilder detail = new StringBuilder();
            try
            {
                RuntimeComponentStatus status = GetStatus(processId);
                detail.Append(" StatusReason=").Append(status.Reason);
                detail.Append(" StatusReady=").Append(status.Ready);
                if (!status.Ready)
                {
                    return new RuntimeComponentStatus
                    {
                        Name = Name,
                        Ready = false,
                        Reason = "HookCommandUnavailable",
                        Detail = detail.ToString()
                    };
                }

                using (HookCommandChannel channel = new HookCommandChannel(processId))
                {
                    HookCommandResult result = channel.Ping(2500);
                    AppendCommandResult(detail, result);
                    return new RuntimeComponentStatus
                    {
                        Name = Name,
                        Ready = result.Ready,
                        Reason = result.Ready ? "HookCommandPingOk" : "HookCommandPingFailed",
                        Detail = detail.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                detail.Append(" CommandError=").Append(ex.GetType().Name).Append(":").Append(ex.Message);
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = "HookCommandFailed",
                    Detail = detail.ToString()
                };
            }
        }

        public RuntimeComponentStatus GetHookInfo(int processId)
        {
            StringBuilder detail = new StringBuilder();
            try
            {
                RuntimeComponentStatus status = GetStatus(processId);
                detail.Append(" StatusReason=").Append(status.Reason);
                detail.Append(" StatusReady=").Append(status.Ready);
                if (!status.Ready)
                {
                    return new RuntimeComponentStatus
                    {
                        Name = Name,
                        Ready = false,
                        Reason = "HookInfoUnavailable",
                        Detail = detail.ToString()
                    };
                }

                using (HookCommandChannel channel = new HookCommandChannel(processId))
                {
                    HookCommandResult result = channel.GetHookInfo(2500);
                    AppendCommandResult(detail, result);
                    detail.Append(" HookProcessId=").Append(result.HookProcessId);
                    detail.Append(" HookProtocolVersion=").Append(result.HookProtocolVersion);
                    detail.Append(" HookStartTick=").Append(result.HookStartTick);
                    detail.Append(" HeartbeatCount=").Append(result.HeartbeatCount);
                    detail.Append(" MainThreadBridgeReady=").Append(result.MainThreadBridgeReady);
                    detail.Append(" LuaBridgeReady=").Append(result.LuaBridgeReady);
                    detail.Append(" LuaSmokeExecuted=").Append(result.LuaSmokeExecuted);
                    detail.Append(" LuaSmokeLastStatus=0x").Append(result.LuaSmokeLastStatus.ToString("X"));
                    return new RuntimeComponentStatus
                    {
                        Name = Name,
                        Ready = result.Ready,
                        Reason = result.Ready ? "HookInfoOk" : "HookInfoFailed",
                        Detail = detail.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                detail.Append(" InfoError=").Append(ex.GetType().Name).Append(":").Append(ex.Message);
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = "HookInfoFailed",
                    Detail = detail.ToString()
                };
            }
        }

        public RuntimeComponentStatus ReadSelfModule(int processId)
        {
            StringBuilder detail = new StringBuilder();
            try
            {
                RuntimeComponentStatus status = GetStatus(processId);
                detail.Append(" StatusReason=").Append(status.Reason);
                detail.Append(" StatusReady=").Append(status.Ready);
                if (!status.Ready)
                {
                    return new RuntimeComponentStatus
                    {
                        Name = Name,
                        Ready = false,
                        Reason = "HookSelfModuleUnavailable",
                        Detail = detail.ToString()
                    };
                }

                using (HookCommandChannel channel = new HookCommandChannel(processId))
                {
                    HookCommandResult result = channel.ReadSelfModule(2500);
                    AppendCommandResult(detail, result);
                    detail.Append(" ModuleBaseLow=0x").Append(result.ModuleBaseLow.ToString("X"));
                    detail.Append(" DosSignature=0x").Append(result.DosSignature.ToString("X"));
                    detail.Append(" PeSignature=0x").Append(result.PeSignature.ToString("X"));
                    detail.Append(" Machine=0x").Append(result.Machine.ToString("X"));
                    detail.Append(" SectionCount=").Append(result.SectionCount);
                    return new RuntimeComponentStatus
                    {
                        Name = Name,
                        Ready = result.Ready,
                        Reason = result.Ready ? "HookSelfModuleReadOk" : "HookSelfModuleReadFailed",
                        Detail = detail.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                detail.Append(" SelfModuleError=").Append(ex.GetType().Name).Append(":").Append(ex.Message);
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = "HookSelfModuleReadFailed",
                    Detail = detail.ToString()
                };
            }
        }

        public RuntimeComponentStatus LuaSmoke(int processId)
        {
            StringBuilder detail = new StringBuilder();
            try
            {
                RuntimeComponentStatus status = EnsureHookReadyForCommands(processId, detail);
                detail.Append(" StatusReason=").Append(status.Reason);
                detail.Append(" StatusReady=").Append(status.Ready);
                detail.Append(" Script=JumpOrAscendStart+SPELLFIRE_LUA_OK");
                if (!status.Ready)
                {
                    return new RuntimeComponentStatus
                    {
                        Name = Name,
                        Ready = false,
                        Reason = MapLuaUnavailableReason(status.Reason, "LuaSmokeHookUnavailable"),
                        Detail = detail.Append(" StatusDetail=[").Append(status.Detail ?? string.Empty).Append("]").ToString()
                    };
                }

                using (HookCommandChannel channel = new HookCommandChannel(processId))
                {
                    HookCommandResult result = channel.LuaSmoke(7000);
                    AppendCommandResult(detail, result);
                    return new RuntimeComponentStatus
                    {
                        Name = Name,
                        Ready = result.Ready,
                        Reason = result.Ready ? "LuaSmokeExecuted" : "LuaSmokeFailed",
                        Detail = detail.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                detail.Append(" LuaSmokeError=").Append(ex.GetType().Name).Append(":").Append(ex.Message);
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = "LuaSmokeFailed",
                    Detail = detail.ToString()
                };
            }
        }

        public RuntimeComponentStatus ExecuteLua(int processId, string script)
        {
            StringBuilder detail = new StringBuilder();
            try
            {
                RuntimeComponentStatus status = EnsureHookReadyForCommands(processId, detail);
                detail.Append(" StatusReason=").Append(status.Reason);
                detail.Append(" StatusReady=").Append(status.Ready);
                detail.Append(" Script=").Append(script ?? string.Empty);
                if (!status.Ready)
                {
                    return new RuntimeComponentStatus
                    {
                        Name = Name,
                        Ready = false,
                        Reason = MapLuaUnavailableReason(status.Reason, "LuaExecuteHookUnavailable"),
                        Detail = detail.Append(" StatusDetail=[").Append(status.Detail ?? string.Empty).Append("]").ToString()
                    };
                }

                using (HookCommandChannel channel = new HookCommandChannel(processId))
                {
                    HookCommandResult result = channel.ExecuteLua(script, 7000);
                    AppendCommandResult(detail, result);
                    return new RuntimeComponentStatus
                    {
                        Name = Name,
                        Ready = result.Ready,
                        Reason = result.Ready ? "LuaExecuteSucceeded" : "LuaExecuteFailed",
                        Detail = detail.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                detail.Append(" LuaExecuteError=").Append(ex.GetType().Name).Append(":").Append(ex.Message);
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = "LuaExecuteFailed",
                    Detail = detail.ToString()
                };
            }
        }

        public void Cleanup(int processId)
        {
        }

        private static void AppendCommandResult(StringBuilder detail, HookCommandResult result)
        {
            detail.Append(" Ack=").Append(result.Ack);
            detail.Append(" Magic=0x").Append(result.Magic.ToString("X"));
            detail.Append(" Version=").Append(result.Version);
            detail.Append(" HeaderSize=").Append(result.HeaderSize);
            detail.Append(" Status=0x").Append(result.Status.ToString("X"));
            detail.Append(" Result=0x").Append(result.Result.ToString("X"));
            detail.Append(" PayloadLength=").Append(result.PayloadLength);
            detail.Append(" PingCount=").Append(result.PingCount);
            detail.Append(" MainThreadBridgeReady=").Append(result.MainThreadBridgeReady);
            detail.Append(" LuaBridgeReady=").Append(result.LuaBridgeReady);
            detail.Append(" LuaSmokeExecuted=").Append(result.LuaSmokeExecuted);
            detail.Append(" LuaSmokeLastStatus=0x").Append(result.LuaSmokeLastStatus.ToString("X"));
            if (!string.IsNullOrWhiteSpace(result.TextPayload))
            {
                detail.Append(" TextPayload=").Append(result.TextPayload);
            }
        }

        private RuntimeComponentStatus EnsureHookReadyForCommands(int processId, StringBuilder detail)
        {
            RuntimeComponentStatus boundary = EvaluateSafetyBoundary(processId);
            detail.Append(" BoundaryReason=").Append(boundary.Reason);
            detail.Append(" BoundaryReady=").Append(boundary.Ready);

            if (string.Equals(boundary.Reason, "SafeBoundary_HookAlive", StringComparison.Ordinal))
            {
                return GetStatus(processId);
            }

            if (string.Equals(boundary.Reason, "SafeBoundary_CleanProcessNoHook", StringComparison.Ordinal) ||
                string.Equals(boundary.Reason, "SafeBoundary_DirtyRecoverable_ReadyMissing", StringComparison.Ordinal))
            {
                RuntimeComponentStatus attach = AttemptAttach(processId);
                detail.Append(" AttachReason=").Append(attach.Reason);
                detail.Append(" AttachReady=").Append(attach.Ready);
                if (!attach.Ready)
                {
                    return attach;
                }

                return GetStatus(processId);
            }

            return new RuntimeComponentStatus
            {
                Name = Name,
                Ready = false,
                Reason = boundary.Reason,
                Detail = boundary.Detail
            };
        }

        private static string MapLuaUnavailableReason(string statusReason, string fallbackReason)
        {
            if (string.IsNullOrWhiteSpace(statusReason))
            {
                return fallbackReason;
            }

            if (statusReason.StartsWith("SafeBoundary_", StringComparison.Ordinal) ||
                statusReason.StartsWith("AttachAttempted_", StringComparison.Ordinal) ||
                string.Equals(statusReason, "HookLoadedButReadySignalMissing_UnloadFailed", StringComparison.Ordinal))
            {
                return statusReason;
            }

            return fallbackReason;
        }

        private static bool TryIsWow64(Process process, out bool isWow64)
        {
            isWow64 = false;
            try
            {
                if (!Environment.Is64BitOperatingSystem)
                {
                    return false;
                }

                return NativeMethods.IsWow64Process(process.Handle, out isWow64);
            }
            catch
            {
                return false;
            }
        }

        private bool WaitForModuleState(int processId, string moduleName, bool present, int timeoutMilliseconds)
        {
            int remaining = Math.Max(0, timeoutMilliseconds);
            while (true)
            {
                bool found = snapshotService.GetModules(processId, moduleName).MatchedModule != null;
                if (found == present)
                {
                    return found;
                }

                if (remaining <= 0)
                {
                    return found;
                }

                int delay = Math.Min(100, remaining);
                Thread.Sleep(delay);
                remaining -= delay;
            }
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.Winapi)]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            public static extern bool IsWow64Process(IntPtr processHandle, out bool wow64Process);
        }
    }
}
