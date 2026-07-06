using SpellFire.Runtime.Contracts;
using SpellFire.Runtime.Models;
using SpellFire.Runtime.Services;

namespace SpellFire.Runtime
{
    public sealed class RuntimeFacade : IRuntimeFacade
    {
        private readonly IRuntimeSessionService sessionService;
        private readonly IRuntimeMemoryProbeService memoryProbeService;
        private readonly RuntimeHookService hookService;

        public RuntimeFacade()
            : this(new RuntimeSessionService(), new RuntimeMemoryProbeService(), new RuntimeHookService())
        {
        }

        public RuntimeFacade(IRuntimeSessionService sessionService)
            : this(sessionService, new RuntimeMemoryProbeService(), new RuntimeHookService())
        {
        }

        public RuntimeFacade(IRuntimeSessionService sessionService, IRuntimeMemoryProbeService memoryProbeService)
            : this(sessionService, memoryProbeService, new RuntimeHookService())
        {
        }

        public RuntimeFacade(IRuntimeSessionService sessionService, IRuntimeMemoryProbeService memoryProbeService, RuntimeHookService hookService)
        {
            this.sessionService = sessionService;
            this.memoryProbeService = memoryProbeService;
            this.hookService = hookService;
        }

        public RuntimeSessionSnapshot Attach(int processId)
        {
            return sessionService.Attach(processId);
        }

        public RuntimeMemoryProbeSnapshot ProbeMemory(int processId)
        {
            return memoryProbeService.Probe(processId);
        }

        public RuntimeOperationSnapshot Preflight(int processId)
        {
            return hookService.Preflight(processId);
        }

        public RuntimeOperationSnapshot AttachHook(int processId)
        {
            return hookService.AttachHook(processId);
        }

        public RuntimeOperationSnapshot GetHookStatus(int processId)
        {
            return hookService.GetHookStatus(processId);
        }

        public RuntimeOperationSnapshot PingHook(int processId)
        {
            return hookService.PingHook(processId);
        }

        public RuntimeOperationSnapshot GetHookInfo(int processId)
        {
            return hookService.GetHookInfo(processId);
        }

        public RuntimeOperationSnapshot ReadHookSelfModule(int processId)
        {
            return hookService.ReadHookSelfModule(processId);
        }

        public RuntimeOperationSnapshot LuaSmoke(int processId)
        {
            return hookService.LuaSmoke(processId);
        }

        public RuntimeOperationSnapshot ExecuteLua(int processId, string script)
        {
            return hookService.ExecuteLua(processId, script);
        }

        public RuntimeOperationSnapshot ShutdownHook(int processId)
        {
            return hookService.ShutdownHook(processId);
        }
    }
}
