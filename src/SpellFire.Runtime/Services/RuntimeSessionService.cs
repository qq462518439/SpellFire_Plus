using System.Linq;
using SpellFire.Runtime.Contracts;
using SpellFire.Runtime.Models;
using SpellFire.RuntimeHost;
using SpellFire.RuntimeHost.Abstractions;

namespace SpellFire.Runtime.Services
{
    public sealed class RuntimeSessionService : IRuntimeSessionService
    {
        private readonly IRuntimeHostFactory hostFactory;

        public RuntimeSessionService()
            : this(new RuntimeHostFactory())
        {
        }

        public RuntimeSessionService(IRuntimeHostFactory hostFactory)
        {
            this.hostFactory = hostFactory;
        }

        public RuntimeSessionSnapshot Attach(int processId)
        {
            IRuntimeHost host = hostFactory.CreateHost();
            using (IRuntimeHostSession session = host.Attach(processId))
            {
                return new RuntimeSessionSnapshot
                {
                    ProcessId = session.ProcessId,
                    HostState = session.State.ToString(),
                    Components = session.Components.Select(component => new RuntimeComponentSnapshot
                    {
                        Name = component.Name,
                        Ready = component.Ready,
                        Reason = component.Reason,
                        Detail = component.Detail
                    }).ToArray()
                };
            }
        }
    }
}
