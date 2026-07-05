using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Native;
using SpellFire.MemoryRobot.Process;
using SpellFire.RuntimeHost.Abstractions;

namespace SpellFire.RuntimeHost.Components
{
    public sealed class SpellFireHookRuntimeComponent : IRuntimeComponent
    {
        private readonly IMemorySessionFactory sessionFactory;

        public SpellFireHookRuntimeComponent(IMemorySessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        }

        public string Name => "SpellFireHook";

        public RuntimeComponentStatus Probe(int processId)
        {
            Process process = null;
            try
            {
                process = Process.GetProcessById(processId);
            }
            catch (Exception ex)
            {
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = "ProcessUnavailable",
                    Detail = ex.GetType().Name + ":" + ex.Message
                };
            }

            StringBuilder detail = new StringBuilder();
            detail.Append(" Process=").Append(process.ProcessName);
            detail.Append(" Pid=").Append(process.Id);
            detail.Append(" Responding=").Append(process.Responding);
            bool isTargetWow64 = false;
            bool wow64Known = TryIsWow64(process, out isTargetWow64);
            detail.Append(" TargetWow64Known=").Append(wow64Known);
            detail.Append(" TargetWow64=").Append(wow64Known ? isTargetWow64.ToString() : "Unknown");

            if (!wow64Known)
            {
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = "TargetBitnessUnknown",
                    Detail = detail.ToString()
                };
            }

            if (!isTargetWow64)
            {
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = "TargetNot32Bit",
                    Detail = detail.ToString()
                };
            }

            try
            {
                using (IMemoryRobot robot = sessionFactory.Open(processId))
                {
                    detail.Append(" SessionOpen=").Append(robot.Session.IsOpen);
                    detail.Append(" Handle=").Append(robot.Session.Handle == IntPtr.Zero ? "null" : "0x" + robot.Session.Handle.ToString("X"));

                    IntPtr allocated = IntPtr.Zero;
                    try
                    {
                        allocated = robot.Allocator.Allocate(64, AllocationType.Commit | AllocationType.Reserve, MemoryProtection.ExecuteReadWrite);
                        detail.Append(" Alloc=").Append(allocated == IntPtr.Zero ? "null" : "0x" + allocated.ToString("X"));
                        robot.Allocator.Free(allocated);
                        detail.Append(" Free=True");

                        return new RuntimeComponentStatus
                        {
                            Name = Name,
                            Ready = true,
                            Reason = "MemoryReadyForHook",
                            Detail = detail.ToString()
                        };
                    }
                    catch (Exception allocEx)
                    {
                        if (allocated != IntPtr.Zero)
                        {
                            try
                            {
                                robot.Allocator.Free(allocated);
                            }
                            catch
                            {
                            }
                        }

                        detail.Append(" AllocError=").Append(allocEx.GetType().Name).Append(":").Append(allocEx.Message);
                        return new RuntimeComponentStatus
                        {
                            Name = Name,
                            Ready = false,
                            Reason = "RemoteAllocationFailed",
                            Detail = detail.ToString()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                detail.Append(" OpenError=").Append(ex.GetType().Name).Append(":").Append(ex.Message);
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = "MemorySessionOpenFailed",
                    Detail = detail.ToString()
                };
            }
        }

        public RuntimeComponentStatus AttemptAttach(int processId)
        {
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
                using (IMemoryRobot robot = sessionFactory.Open(processId))
                {
                    var existingModule = robot.Modules.GetModules()
                        .FirstOrDefault(module => string.Equals(module.Name, "SpellFire.Hook.dll", StringComparison.OrdinalIgnoreCase));
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
                        bool unloaded = robot.Libraries.FreeLibrary(existingModule.BaseAddress, 5000);
                        detail.Append(" StaleUnloadResult=").Append(unloaded);
                        Thread.Sleep(250);
                        bool stillLoaded = robot.Modules.GetModules()
                            .Any(module => string.Equals(module.Name, "SpellFire.Hook.dll", StringComparison.OrdinalIgnoreCase));
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
                    int moduleHandle = robot.Libraries.LoadLibrary(payloadPath, 10000);
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
                using (IMemoryRobot robot = sessionFactory.Open(processId))
                using (EventWaitHandle heartbeatEvent = HookReadySignal.CreateHeartbeat(processId))
                {
                    var existingModule = robot.Modules.GetModules()
                        .FirstOrDefault(module => string.Equals(module.Name, "SpellFire.Hook.dll", StringComparison.OrdinalIgnoreCase));
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
                RuntimeComponentStatus status = GetStatus(processId);
                detail.Append(" StatusReason=").Append(status.Reason);
                detail.Append(" StatusReady=").Append(status.Ready);
                detail.Append(" Script=JumpOrAscendStart+SPELLFIRE_LUA_OK");
                if (!status.Ready)
                {
                    return new RuntimeComponentStatus
                    {
                        Name = Name,
                        Ready = false,
                        Reason = "LuaSmokeHookUnavailable",
                        Detail = detail.ToString()
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

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.Winapi)]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            public static extern bool IsWow64Process(IntPtr processHandle, out bool wow64Process);
        }
    }
}
