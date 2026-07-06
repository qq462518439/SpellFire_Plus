using SpellFire.Runtime.Models;

namespace SpellFire.Runtime.Contracts
{
    public interface IRuntimeFacade
    {
        RuntimeSessionSnapshot Attach(int processId);

        RuntimeMemoryProbeSnapshot ProbeMemory(int processId);

        RuntimeOperationSnapshot Preflight(int processId);

        RuntimeOperationSnapshot AttachHook(int processId);

        RuntimeOperationSnapshot GetHookStatus(int processId);

        RuntimeOperationSnapshot PingHook(int processId);

        RuntimeOperationSnapshot GetHookInfo(int processId);

        RuntimeOperationSnapshot ReadHookSelfModule(int processId);

        RuntimeOperationSnapshot LuaSmoke(int processId);

        RuntimeOperationSnapshot ExecuteLua(int processId, string script);

        RuntimeOperationSnapshot ShutdownHook(int processId);
    }
}
