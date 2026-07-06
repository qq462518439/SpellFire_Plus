using SpellFire.Runtime.Contracts;
using SpellFire.Runtime.Models;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Scripting;

namespace SpellFire.WowRuntime.Infrastructure
{
    public sealed class RuntimeFacadeScriptService : IScriptService
    {
        private readonly int processId;
        private readonly IRuntimeFacade runtime;

        public RuntimeFacadeScriptService(int processId, IRuntimeFacade runtime)
        {
            this.processId = processId;
            this.runtime = runtime;
        }

        public WowRuntimeResult<ScriptExecutionSnapshot> LuaSmoke()
        {
            return Map(runtime.LuaSmoke(processId));
        }

        public WowRuntimeResult<ScriptExecutionSnapshot> Execute(string script)
        {
            if (string.IsNullOrWhiteSpace(script))
            {
                return WowRuntimeResult<ScriptExecutionSnapshot>.Fail(WowRuntimeStatus.InvalidArgument, "Script must not be empty.");
            }

            return Map(runtime.ExecuteLua(processId, script));
        }

        private static WowRuntimeResult<ScriptExecutionSnapshot> Map(RuntimeOperationSnapshot operation)
        {
            if (operation == null)
            {
                return WowRuntimeResult<ScriptExecutionSnapshot>.Fail(WowRuntimeStatus.FeatureUnavailable, "Runtime operation did not return a result.");
            }

            ScriptExecutionSnapshot snapshot = new ScriptExecutionSnapshot(
                operation.ProcessId,
                operation.Operation,
                operation.Reason,
                operation.Detail);

            return operation.Ready
                ? WowRuntimeResult<ScriptExecutionSnapshot>.Ok(snapshot)
                : WowRuntimeResult<ScriptExecutionSnapshot>.Fail(MapStatus(operation.Reason), operation.Detail);
        }

        private static WowRuntimeStatus MapStatus(string reason)
        {
            if (string.Equals(reason, "ProcessUnavailable", System.StringComparison.OrdinalIgnoreCase))
            {
                return WowRuntimeStatus.ProcessUnavailable;
            }

            if (string.Equals(reason, "InvalidArgument", System.StringComparison.OrdinalIgnoreCase))
            {
                return WowRuntimeStatus.InvalidArgument;
            }

            return WowRuntimeStatus.FeatureUnavailable;
        }
    }
}
