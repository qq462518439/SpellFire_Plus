using SpellFire.Runtime.Contracts;
using SpellFire.Runtime.Models;
using SpellFire.Runtime.Services;

namespace SpellFire.Runtime
{
    public sealed class RuntimeFacade : IRuntimeFacade
    {
        private readonly IRuntimeSessionService sessionService;
        private readonly IRuntimeMemoryProbeService memoryProbeService;

        public RuntimeFacade()
            : this(new RuntimeSessionService(), new RuntimeMemoryProbeService())
        {
        }

        public RuntimeFacade(IRuntimeSessionService sessionService)
            : this(sessionService, new RuntimeMemoryProbeService())
        {
        }

        public RuntimeFacade(IRuntimeSessionService sessionService, IRuntimeMemoryProbeService memoryProbeService)
        {
            this.sessionService = sessionService;
            this.memoryProbeService = memoryProbeService;
        }

        public RuntimeSessionSnapshot Attach(int processId)
        {
            return sessionService.Attach(processId);
        }

        public RuntimeMemoryProbeSnapshot ProbeMemory(int processId)
        {
            return memoryProbeService.Probe(processId);
        }
    }
}
