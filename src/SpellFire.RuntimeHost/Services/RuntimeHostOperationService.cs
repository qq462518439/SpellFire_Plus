using System;
using System.Linq;
using SpellFire.RuntimeHost.Abstractions;
using SpellFire.RuntimeHost.Components;

namespace SpellFire.RuntimeHost.Services
{
    public sealed class RuntimeHostOperationService : IDisposable
    {
        private readonly IRuntimeHost host;

        public RuntimeHostOperationService()
            : this(new RuntimeHostFactory())
        {
        }

        public RuntimeHostOperationService(IRuntimeHostFactory hostFactory)
        {
            if (hostFactory == null)
            {
                throw new ArgumentNullException(nameof(hostFactory));
            }

            host = hostFactory.CreateHost();
        }

        public RuntimeHostOperationResult Preflight(int processId)
        {
            return Run(processId, "preflight", hook => hook.EvaluateSafetyBoundary(processId));
        }

        public RuntimeHostOperationResult ProbeMemoryRobot(int processId)
        {
            IRuntimeHostSession session = host.Attach(processId);
            RuntimeComponentStatus component = session.Components.FirstOrDefault(item => string.Equals(item.Name, "MemoryRobot", StringComparison.Ordinal));
            return CreateResult(processId, "memory-probe", component, session);
        }

        public RuntimeHostOperationResult AttachHook(int processId)
        {
            return Run(processId, "attach", hook => hook.AttemptAttach(processId));
        }

        public RuntimeHostOperationResult GetHookStatus(int processId)
        {
            return RunCommand(processId, "status", hook => hook.GetStatus(processId));
        }

        public RuntimeHostOperationResult PingHook(int processId)
        {
            return RunCommand(processId, "command-ping", hook => hook.CommandPing(processId));
        }

        public RuntimeHostOperationResult GetHookInfo(int processId)
        {
            return RunCommand(processId, "hook-info", hook => hook.GetHookInfo(processId));
        }

        public RuntimeHostOperationResult ReadHookSelfModule(int processId)
        {
            return RunCommand(processId, "read-self-module", hook => hook.ReadSelfModule(processId));
        }

        public RuntimeHostOperationResult LuaSmoke(int processId)
        {
            return RunCommand(processId, "lua-smoke", hook => hook.LuaSmoke(processId));
        }

        public RuntimeHostOperationResult ExecuteLua(int processId, string script)
        {
            return RunCommand(processId, "lua-exec", hook => hook.ExecuteLua(processId, script ?? string.Empty));
        }

        public RuntimeHostOperationResult ShutdownHook(int processId)
        {
            return RunCommand(processId, "shutdown", hook =>
            {
                RuntimeComponentStatus status = hook.GetStatus(processId);
                return status != null && status.Ready ? hook.RequestShutdown(processId) : status;
            });
        }

        public void Dispose()
        {
            foreach (IRuntimeHostSession session in host.GetSessions())
            {
                session.Dispose();
            }
        }

        private RuntimeHostOperationResult Run(int processId, string operation, Func<SpellFireHookRuntimeComponent, RuntimeComponentStatus> action)
        {
            IRuntimeHostSession session = host.Attach(processId);
            SpellFireHookRuntimeComponent hook = GetHookComponent();
            if (hook == null)
            {
                return CreateResult(processId, operation, new RuntimeComponentStatus
                {
                    Name = "SpellFireHook",
                    Ready = false,
                    Reason = "SpellFireHookUnavailable",
                    Detail = "Hook component is not registered."
                }, session);
            }

            RuntimeComponentStatus status = action(hook);
            return CreateResult(processId, operation, status, session);
        }

        private RuntimeHostOperationResult RunCommand(int processId, string operation, Func<SpellFireHookRuntimeComponent, RuntimeComponentStatus> action)
        {
            IRuntimeHostSession session = host.Attach(processId);
            SpellFireHookRuntimeComponent hook = GetHookComponent();
            if (hook == null)
            {
                return CreateResult(processId, operation, new RuntimeComponentStatus
                {
                    Name = "SpellFireHook",
                    Ready = false,
                    Reason = "SpellFireHookUnavailable",
                    Detail = "Hook component is not registered."
                }, session);
            }

            RuntimeComponentStatus baseline = hook.Probe(processId);
            if (baseline == null || !baseline.Ready)
            {
                return CreateResult(processId, operation, baseline, session);
            }

            RuntimeComponentStatus boundary = hook.EvaluateSafetyBoundary(processId);
            if (boundary == null || !CanRunCommandAfterBoundary(boundary.Reason))
            {
                return CreateResult(processId, operation, boundary, session);
            }

            RuntimeComponentStatus status = action(hook);
            return CreateResult(processId, operation, status, session);
        }

        private static bool CanRunCommandAfterBoundary(string reason)
        {
            return string.Equals(reason, "SafeBoundary_HookAlive", StringComparison.Ordinal) ||
                   string.Equals(reason, "SafeBoundary_CleanProcessNoHook", StringComparison.Ordinal) ||
                   string.Equals(reason, "SafeBoundary_DirtyRecoverable_ReadyMissing", StringComparison.Ordinal);
        }

        private SpellFireHookRuntimeComponent GetHookComponent()
        {
            RuntimeHost concreteHost = host as RuntimeHost;
            if (concreteHost == null)
            {
                return null;
            }

            foreach (IRuntimeComponent component in concreteHost.Components)
            {
                SpellFireHookRuntimeComponent hook = component as SpellFireHookRuntimeComponent;
                if (hook != null)
                {
                    return hook;
                }
            }

            return null;
        }

        private static RuntimeHostOperationResult CreateResult(int processId, string operation, RuntimeComponentStatus status, IRuntimeHostSession session)
        {
            return new RuntimeHostOperationResult
            {
                ProcessId = processId,
                Operation = operation,
                Ready = status != null && status.Ready,
                Reason = status == null ? "ComponentUnavailable" : status.Reason,
                Detail = status == null ? string.Empty : status.Detail,
                HostState = session == null ? RuntimeHostState.Unknown : session.State,
                Components = session == null ? Array.Empty<RuntimeComponentStatus>() : session.Components
            };
        }
    }
}
