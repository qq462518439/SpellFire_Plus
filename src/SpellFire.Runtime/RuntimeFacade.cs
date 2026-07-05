using SpellFire.Runtime.Contracts;
using SpellFire.Runtime.Models;
using SpellFire.Runtime.Services;

namespace SpellFire.Runtime
{
    public sealed class RuntimeFacade : IRuntimeFacade
    {
        private readonly IRuntimeSessionService sessionService;

        public RuntimeFacade()
            : this(new RuntimeSessionService())
        {
        }

        public RuntimeFacade(IRuntimeSessionService sessionService)
        {
            this.sessionService = sessionService;
        }

        public RuntimeSessionSnapshot Attach(int processId)
        {
            return sessionService.Attach(processId);
        }
    }
}
